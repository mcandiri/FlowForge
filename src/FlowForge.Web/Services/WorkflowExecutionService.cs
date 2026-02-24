using FlowForge.Core.Engine;
using FlowForge.Core.Models;

namespace FlowForge.Web.Services;

/// <summary>
/// Bridges the Blazor UI with the Core workflow engine.
/// Manages execution state, progress reporting, and cancellation.
/// </summary>
public sealed class WorkflowExecutionService : IDisposable
{
    private readonly IWorkflowEngine _engine;
    private readonly CanvasStateService _canvasState;
    private CancellationTokenSource? _cts;

    public WorkflowExecutionService(IWorkflowEngine engine, CanvasStateService canvasState)
    {
        _engine = engine;
        _canvasState = canvasState;
    }

    // ── State ────────────────────────────────────────────────
    public bool IsRunning { get; private set; }
    public List<ExecutionTrace> Traces { get; private set; } = new();
    public Dictionary<string, ExecutionStatus> NodeStatuses { get; private set; } = new();
    public WorkflowExecutionResult? LastResult { get; private set; }

    // ── Events ───────────────────────────────────────────────
    public event Action<ExecutionTrace>? OnTraceAdded;
    public event Action? OnExecutionStarted;
    public event Action<WorkflowExecutionResult>? OnExecutionCompleted;
    public event Action? OnStateChanged;

    // ── Execution ────────────────────────────────────────────

    public async Task ExecuteAsync()
    {
        if (IsRunning) return;

        var workflow = _canvasState.GetWorkflow();
        if (workflow.Nodes.Count == 0) return;

        IsRunning = true;
        Traces.Clear();
        NodeStatuses.Clear();
        LastResult = null;
        _cts = new CancellationTokenSource();

        OnExecutionStarted?.Invoke();
        NotifyStateChanged();

        var context = new FlowForge.Core.Engine.ExecutionContext();
        var progress = new Progress<ExecutionTrace>(trace =>
        {
            Traces.Add(trace);
            NodeStatuses[trace.NodeId] = trace.Status;
            OnTraceAdded?.Invoke(trace);
            NotifyStateChanged();
        });

        try
        {
            LastResult = await _engine.ExecuteAsync(workflow, context, progress, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            LastResult = new WorkflowExecutionResult
            {
                WorkflowId = workflow.Id,
                Success = false,
                Error = "Execution was cancelled by user.",
                EndTime = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            LastResult = new WorkflowExecutionResult
            {
                WorkflowId = workflow.Id,
                Success = false,
                Error = $"Execution failed: {ex.Message}",
                EndTime = DateTime.UtcNow
            };
        }
        finally
        {
            IsRunning = false;
            OnExecutionCompleted?.Invoke(LastResult!);
            NotifyStateChanged();
        }
    }

    public void Stop()
    {
        if (!IsRunning) return;
        _cts?.Cancel();
    }

    public void ClearTraces()
    {
        Traces.Clear();
        NodeStatuses.Clear();
        LastResult = null;
        NotifyStateChanged();
    }

    public ExecutionStatus GetNodeStatus(string nodeId)
    {
        return NodeStatuses.TryGetValue(nodeId, out var status)
            ? status
            : ExecutionStatus.Pending;
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
