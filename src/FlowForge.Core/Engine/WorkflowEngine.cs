using System.Collections.Concurrent;
using System.Diagnostics;
using FlowForge.Core.Models;
using FlowForge.Core.Nodes;
using Microsoft.Extensions.Logging;

namespace FlowForge.Core.Engine;

/// <summary>
/// Main workflow execution engine. Walks the graph topologically starting from
/// nodes with no incoming edges, handles condition branching (true/false),
/// loop iteration, parallel branches, progress reporting, and per-node error
/// isolation. Thread-safe for concurrent workflow runs.
/// </summary>
public class WorkflowEngine : IWorkflowEngine
{
    private readonly NodeExecutor _nodeExecutor;
    private readonly ILogger<WorkflowEngine> _logger;

    public WorkflowEngine(NodeRegistry registry, ILogger<WorkflowEngine> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _nodeExecutor = new NodeExecutor(registry);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult> ExecuteAsync(
        Workflow workflow,
        ExecutionContext context,
        IProgress<ExecutionTrace>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(context);

        var result = new WorkflowExecutionResult
        {
            WorkflowId = workflow.Id,
            StartTime = DateTime.UtcNow,
            Success = true
        };

        try
        {
            // Build lookup structures once for efficient graph traversal
            var nodeLookup = workflow.Nodes.ToDictionary(n => n.Id, StringComparer.OrdinalIgnoreCase);
            var outgoingEdges = BuildOutgoingEdgeMap(workflow.Edges);
            var incomingEdges = BuildIncomingEdgeMap(workflow.Edges);

            // Start nodes are those with no incoming edges
            var startNodes = workflow.Nodes
                .Where(n => !incomingEdges.ContainsKey(n.Id) || incomingEdges[n.Id].Count == 0)
                .ToList();

            if (startNodes.Count == 0)
            {
                result.Success = false;
                result.Error = "No start nodes found. A workflow must have at least one node with no incoming edges.";
                result.EndTime = DateTime.UtcNow;
                return result;
            }

            _logger.LogInformation(
                "Starting workflow '{WorkflowName}' ({WorkflowId}) with {StartNodeCount} start node(s).",
                workflow.Metadata.Name, workflow.Id, startNodes.Count);

            // Track which nodes have been visited to avoid re-execution
            var visited = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            // Execute starting from each start node. If there are multiple start
            // nodes they represent independent parallel branches.
            if (startNodes.Count == 1)
            {
                await ExecuteNodeChainAsync(
                    startNodes[0], nodeLookup, outgoingEdges, context,
                    visited, result, progress, ct).ConfigureAwait(false);
            }
            else
            {
                var tasks = startNodes.Select(startNode =>
                    ExecuteNodeChainAsync(
                        startNode, nodeLookup, outgoingEdges, context,
                        visited, result, progress, ct));

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            // If any trace ended with an error, mark overall as failed
            if (result.Traces.Any(t => t.Status == ExecutionStatus.Error))
            {
                result.Success = false;
                result.Error = "One or more nodes failed during execution.";
            }
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.Error = "Workflow execution was cancelled.";
            _logger.LogWarning("Workflow '{WorkflowId}' was cancelled.", workflow.Id);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Unexpected error: {ex.Message}";
            _logger.LogError(ex, "Unexpected error executing workflow '{WorkflowId}'.", workflow.Id);
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
            result.Variables = new Dictionary<string, object?>(context.Variables, StringComparer.OrdinalIgnoreCase);
            result.NodesExecuted = result.Traces.Count(t =>
                t.Status is ExecutionStatus.Success or ExecutionStatus.Error);
        }

        return result;
    }

    // ───────────────────────── Graph traversal ─────────────────────────

    /// <summary>
    /// Recursively executes a node and follows its outgoing edges based on the
    /// output port of the execution result.
    /// </summary>
    private async Task ExecuteNodeChainAsync(
        FlowNode node,
        Dictionary<string, FlowNode> nodeLookup,
        Dictionary<string, List<FlowEdge>> outgoingEdges,
        ExecutionContext context,
        ConcurrentDictionary<string, bool> visited,
        WorkflowExecutionResult workflowResult,
        IProgress<ExecutionTrace>? progress,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Guard against re-visiting a node (can happen in diamond-shaped graphs)
        if (!visited.TryAdd(node.Id, true))
            return;

        // ── Report "Running" ────────────────────────────────────────────
        var runningTrace = new ExecutionTrace
        {
            NodeId = node.Id,
            NodeName = node.Name,
            NodeType = node.Type,
            Status = ExecutionStatus.Running,
            Message = $"Executing node '{node.Name}'..."
        };
        progress?.Report(runningTrace);

        // ── Execute the node ────────────────────────────────────────────
        var executionResult = await _nodeExecutor.ExecuteAsync(node, context, ct).ConfigureAwait(false);

        // ── Build and report completion trace ───────────────────────────
        var trace = new ExecutionTrace
        {
            NodeId = node.Id,
            NodeName = node.Name,
            NodeType = node.Type,
            Status = executionResult.Success ? ExecutionStatus.Success : ExecutionStatus.Error,
            Duration = executionResult.Duration,
            Output = executionResult.Data,
            Error = executionResult.Error,
            Message = executionResult.Success
                ? $"Node '{node.Name}' completed successfully."
                : $"Node '{node.Name}' failed: {executionResult.Error}"
        };

        lock (workflowResult.Traces)
        {
            workflowResult.Traces.Add(trace);
        }

        progress?.Report(trace);

        _logger.LogDebug(
            "Node '{NodeName}' ({NodeId}) completed with status {Status} in {Duration}ms.",
            node.Name, node.Id, trace.Status, executionResult.Duration.TotalMilliseconds);

        // ── Handle loop nodes ───────────────────────────────────────────
        if (IsLoopNode(node))
        {
            await HandleLoopNodeAsync(
                node, executionResult, nodeLookup, outgoingEdges,
                context, visited, workflowResult, progress, ct).ConfigureAwait(false);
            return;
        }

        // ── Follow outgoing edges based on the output port ──────────────
        await FollowOutgoingEdgesAsync(
            node, executionResult.OutputPort, nodeLookup, outgoingEdges,
            context, visited, workflowResult, progress, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Finds outgoing edges whose SourcePortName matches the given output port
    /// and continues execution along those edges. Multiple matching edges
    /// are executed in parallel (parallel branches).
    /// </summary>
    private async Task FollowOutgoingEdgesAsync(
        FlowNode currentNode,
        string outputPort,
        Dictionary<string, FlowNode> nodeLookup,
        Dictionary<string, List<FlowEdge>> outgoingEdges,
        ExecutionContext context,
        ConcurrentDictionary<string, bool> visited,
        WorkflowExecutionResult workflowResult,
        IProgress<ExecutionTrace>? progress,
        CancellationToken ct)
    {
        if (!outgoingEdges.TryGetValue(currentNode.Id, out var edges) || edges.Count == 0)
            return;

        // Filter edges matching the output port of the execution result
        var matchingEdges = edges
            .Where(e => string.Equals(e.SourcePortName, outputPort, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // If no edges match the specific port, fall back to "output" port for
        // non-condition nodes so simple action nodes still chain correctly.
        if (matchingEdges.Count == 0 && !IsConditionNode(currentNode))
        {
            matchingEdges = edges
                .Where(e => string.Equals(e.SourcePortName, "output", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (matchingEdges.Count == 0)
            return;

        // Resolve target nodes
        var nextNodes = matchingEdges
            .Select(e => nodeLookup.GetValueOrDefault(e.TargetNodeId))
            .Where(n => n is not null)
            .Cast<FlowNode>()
            .ToList();

        if (nextNodes.Count == 1)
        {
            await ExecuteNodeChainAsync(
                nextNodes[0], nodeLookup, outgoingEdges, context,
                visited, workflowResult, progress, ct).ConfigureAwait(false);
        }
        else if (nextNodes.Count > 1)
        {
            // Parallel branches
            var tasks = nextNodes.Select(nextNode =>
                ExecuteNodeChainAsync(
                    nextNode, nodeLookup, outgoingEdges, context,
                    visited, workflowResult, progress, ct));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    // ───────────────────────── Loop handling ────────────────────────────

    /// <summary>
    /// Handles loop node execution: iterates over a collection, executing the
    /// "iteration" edge for each item, then follows the "completed" edge.
    /// </summary>
    private async Task HandleLoopNodeAsync(
        FlowNode loopNode,
        ExecutionResult loopResult,
        Dictionary<string, FlowNode> nodeLookup,
        Dictionary<string, List<FlowEdge>> outgoingEdges,
        ExecutionContext context,
        ConcurrentDictionary<string, bool> visited,
        WorkflowExecutionResult workflowResult,
        IProgress<ExecutionTrace>? progress,
        CancellationToken ct)
    {
        if (!outgoingEdges.TryGetValue(loopNode.Id, out var edges))
            return;

        // Find edges for the iteration body and the completion path
        var iterationEdges = edges
            .Where(e => string.Equals(e.SourcePortName, "iteration", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var completedEdges = edges
            .Where(e => string.Equals(e.SourcePortName, "completed", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Resolve the items to iterate over from the loop node result
        var items = ResolveLoopItems(loopResult);

        if (items is not null && iterationEdges.Count > 0)
        {
            var iterationTargets = iterationEdges
                .Select(e => nodeLookup.GetValueOrDefault(e.TargetNodeId))
                .Where(n => n is not null)
                .Cast<FlowNode>()
                .ToList();

            int index = 0;
            foreach (var item in items)
            {
                ct.ThrowIfCancellationRequested();

                // Set loop variables so iteration body nodes can access them
                context.SetVariable("loopItem", item);
                context.SetVariable("loopIndex", index);

                // For each iteration, allow re-visiting the iteration body nodes
                var iterationVisited = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

                // Copy the already-visited nodes but exclude the iteration targets
                // and their downstream nodes so they can re-execute each iteration.
                foreach (var kvp in visited)
                {
                    iterationVisited.TryAdd(kvp.Key, kvp.Value);
                }

                // Remove iteration body nodes from visited so they run again
                foreach (var target in iterationTargets)
                {
                    RemoveDownstreamFromVisited(target.Id, outgoingEdges, iterationVisited, completedEdges);
                }

                foreach (var target in iterationTargets)
                {
                    await ExecuteNodeChainAsync(
                        target, nodeLookup, outgoingEdges, context,
                        iterationVisited, workflowResult, progress, ct).ConfigureAwait(false);
                }

                index++;
            }
        }

        // After all iterations, follow the "completed" edge
        if (completedEdges.Count > 0)
        {
            var completedTargets = completedEdges
                .Select(e => nodeLookup.GetValueOrDefault(e.TargetNodeId))
                .Where(n => n is not null)
                .Cast<FlowNode>()
                .ToList();

            foreach (var target in completedTargets)
            {
                // Ensure these targets can be visited even if visited during iteration
                visited.TryRemove(target.Id, out _);

                await ExecuteNodeChainAsync(
                    target, nodeLookup, outgoingEdges, context,
                    visited, workflowResult, progress, ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Removes a node and its downstream successors from the visited set
    /// so they can be re-executed in a loop iteration. Stops at completed edges
    /// so the completion path is not cleared.
    /// </summary>
    private static void RemoveDownstreamFromVisited(
        string nodeId,
        Dictionary<string, List<FlowEdge>> outgoingEdges,
        ConcurrentDictionary<string, bool> visited,
        List<FlowEdge> completedEdges)
    {
        var completedTargets = new HashSet<string>(
            completedEdges.Select(e => e.TargetNodeId),
            StringComparer.OrdinalIgnoreCase);

        var queue = new Queue<string>();
        queue.Enqueue(nodeId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            visited.TryRemove(current, out _);

            if (outgoingEdges.TryGetValue(current, out var edges))
            {
                foreach (var edge in edges)
                {
                    // Don't clear nodes on the completed path
                    if (!completedTargets.Contains(edge.TargetNodeId))
                    {
                        queue.Enqueue(edge.TargetNodeId);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extracts the iterable collection from a loop node's execution result.
    /// </summary>
    private static IEnumerable<object?>? ResolveLoopItems(ExecutionResult result)
    {
        if (result.Data is null)
            return null;

        if (result.Data is IEnumerable<object?> enumerable)
            return enumerable;

        if (result.Data is System.Collections.IEnumerable nonGeneric and not string)
            return nonGeneric.Cast<object?>();

        // Wrap a single value as a single-item collection
        return new[] { result.Data };
    }

    // ───────────────────────── Helper methods ───────────────────────────

    /// <summary>
    /// Builds a dictionary mapping SourceNodeId to all outgoing edges.
    /// </summary>
    private static Dictionary<string, List<FlowEdge>> BuildOutgoingEdgeMap(List<FlowEdge> edges)
    {
        var map = new Dictionary<string, List<FlowEdge>>(StringComparer.OrdinalIgnoreCase);
        foreach (var edge in edges)
        {
            if (!map.TryGetValue(edge.SourceNodeId, out var list))
            {
                list = new List<FlowEdge>();
                map[edge.SourceNodeId] = list;
            }
            list.Add(edge);
        }
        return map;
    }

    /// <summary>
    /// Builds a dictionary mapping TargetNodeId to all incoming edges.
    /// </summary>
    private static Dictionary<string, List<FlowEdge>> BuildIncomingEdgeMap(List<FlowEdge> edges)
    {
        var map = new Dictionary<string, List<FlowEdge>>(StringComparer.OrdinalIgnoreCase);
        foreach (var edge in edges)
        {
            if (!map.TryGetValue(edge.TargetNodeId, out var list))
            {
                list = new List<FlowEdge>();
                map[edge.TargetNodeId] = list;
            }
            list.Add(edge);
        }
        return map;
    }

    /// <summary>
    /// Determines whether a node is a condition/branch node by checking its type
    /// or whether it has "true"/"false" output ports.
    /// </summary>
    private static bool IsConditionNode(FlowNode node)
    {
        if (node.Type.Contains("condition", StringComparison.OrdinalIgnoreCase) ||
            node.Type.Contains("branch", StringComparison.OrdinalIgnoreCase) ||
            node.Type.Contains("if", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return node.OutputPorts.Any(p =>
            string.Equals(p.Name, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.Name, "false", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines whether a node is a loop node by checking its type
    /// or whether it has "iteration"/"completed" output ports.
    /// </summary>
    private static bool IsLoopNode(FlowNode node)
    {
        if (node.Type.Contains("loop", StringComparison.OrdinalIgnoreCase) ||
            node.Type.Contains("foreach", StringComparison.OrdinalIgnoreCase) ||
            node.Type.Contains("repeat", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return node.OutputPorts.Any(p =>
            string.Equals(p.Name, "iteration", StringComparison.OrdinalIgnoreCase)) &&
               node.OutputPorts.Any(p =>
            string.Equals(p.Name, "completed", StringComparison.OrdinalIgnoreCase));
    }
}
