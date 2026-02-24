namespace FlowForge.Core.Engine;

public class ExecutionResult
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Error { get; set; }
    public string OutputPort { get; set; } = "output";
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object?> OutputVariables { get; set; } = new();

    public static ExecutionResult Succeeded(object? data = null, string outputPort = "output")
        => new() { Success = true, Data = data, OutputPort = outputPort };

    public static ExecutionResult Failed(string error, string outputPort = "error")
        => new() { Success = false, Error = error, OutputPort = outputPort };
}
