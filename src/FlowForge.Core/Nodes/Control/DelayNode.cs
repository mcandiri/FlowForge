using FlowForge.Core.Engine;
using FlowForge.Core.Models;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Nodes.Control;

public class DelayNode : IFlowNode
{
    public string Type => "delay";

    public NodeDefinition Definition => new()
    {
        Type = Type,
        Name = "Delay",
        Description = "Pause workflow execution for a specified duration in milliseconds.",
        Icon = "\u23f0",
        Category = NodeCategory.Control,
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
            new() { Name = "Duration", Label = "Duration (ms)", Type = "number", DefaultValue = 1000, Required = true }
        }
    };

    public async Task<ExecutionResult> ExecuteAsync(NodeConfig config, ExecutionContext context, CancellationToken ct = default)
    {
        var duration = config.GetInt("Duration", 1000);

        if (duration < 0)
        {
            duration = 0;
        }

        // Cap at 5 minutes to prevent excessively long delays
        if (duration > 300000)
        {
            duration = 300000;
        }

        await Task.Delay(duration, ct);

        return ExecutionResult.Succeeded(
            new Dictionary<string, object?>
            {
                ["delayMs"] = duration,
                ["message"] = $"Waited {duration}ms"
            },
            "output"
        );
    }
}
