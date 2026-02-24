using System.Diagnostics;
using FlowForge.Core.Models;
using FlowForge.Core.Nodes;

namespace FlowForge.Core.Engine;

/// <summary>
/// Resolves and executes individual nodes by looking up their IFlowNode
/// implementation from the NodeRegistry, measuring execution duration,
/// and wrapping results in try/catch for error isolation.
/// </summary>
public class NodeExecutor
{
    private readonly NodeRegistry _registry;

    public NodeExecutor(NodeRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// Executes a single FlowNode by resolving its IFlowNode implementation,
    /// running it with the provided config and context, measuring the duration,
    /// and catching any exceptions.
    /// </summary>
    public async Task<ExecutionResult> ExecuteAsync(
        FlowNode node,
        ExecutionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(context);

        var flowNode = _registry.GetNode(node.Type);
        if (flowNode is null)
        {
            return ExecutionResult.Failed(
                $"No node implementation registered for type '{node.Type}'.",
                "error");
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            ct.ThrowIfCancellationRequested();

            var result = await flowNode.ExecuteAsync(node.Config, context, ct).ConfigureAwait(false);
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            // Merge any output variables the node produced into the shared context
            if (result.OutputVariables is { Count: > 0 })
            {
                foreach (var kvp in result.OutputVariables)
                {
                    context.SetVariable(kvp.Key, kvp.Value);
                }
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return new ExecutionResult
            {
                Success = false,
                Error = "Execution was cancelled.",
                OutputPort = "error",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ExecutionResult
            {
                Success = false,
                Error = $"{ex.GetType().Name}: {ex.Message}",
                OutputPort = "error",
                Duration = stopwatch.Elapsed
            };
        }
    }
}
