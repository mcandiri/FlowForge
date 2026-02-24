namespace FlowForge.Core.Engine;

public class ExecutionTrace
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public ExecutionStatus Status { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }
    public object? Output { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
}
