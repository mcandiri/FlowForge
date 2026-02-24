namespace FlowForge.Core.Engine;

public class WorkflowExecutionResult
{
    public bool Success { get; set; }
    public string WorkflowId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan TotalDuration => EndTime - StartTime;
    public List<ExecutionTrace> Traces { get; set; } = new();
    public Dictionary<string, object?> Variables { get; set; } = new();
    public string? Error { get; set; }
    public int NodesExecuted { get; set; }
}
