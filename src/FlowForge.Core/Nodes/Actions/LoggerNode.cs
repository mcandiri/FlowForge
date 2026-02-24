using FlowForge.Core.Engine;
using FlowForge.Core.Models;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Nodes.Actions;

public class LoggerNode : IFlowNode
{
    public string Type => "logger";

    public NodeDefinition Definition => new()
    {
        Type = Type,
        Name = "Logger",
        Description = "Log a message with variable interpolation. Supports Debug, Info, Warning, and Error levels.",
        Icon = "\ud83d\udcdd",
        Category = NodeCategory.Action,
        InputPorts = new List<PortDefinition>
        {
            new() { Name = "input", Label = "Input" }
        },
        OutputPorts = new List<PortDefinition>
        {
            new() { Name = "output", Label = "Output" }
        },
        ConfigFields = new List<ConfigField>
        {
            new() { Name = "Message", Label = "Message", Type = "text", Required = true },
            new() { Name = "Level", Label = "Log Level", Type = "select", DefaultValue = "Info", Options = new List<string> { "Debug", "Info", "Warning", "Error" } }
        }
    };

    public Task<ExecutionResult> ExecuteAsync(NodeConfig config, ExecutionContext context, CancellationToken ct = default)
    {
        var messageTemplate = config.GetString("Message");
        var level = config.GetString("Level", "Info");

        // Interpolate variables in the message template
        var message = context.InterpolateString(messageTemplate);

        var logEntry = new Dictionary<string, object?>
        {
            ["level"] = level,
            ["message"] = message,
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        };

        context.SetVariable("lastLog", logEntry);

        var result = ExecutionResult.Succeeded(logEntry, "output");
        result.OutputVariables["lastLog"] = logEntry;

        return Task.FromResult(result);
    }
}
