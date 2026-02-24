using FlowForge.Core.Engine;
using FlowForge.Core.Models;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Nodes.Actions;

public class SetVariableNode : IFlowNode
{
    public string Type => "set-variable";

    public NodeDefinition Definition => new()
    {
        Type = Type,
        Name = "Set Variable",
        Description = "Set a workflow variable to a specified value. The value supports variable interpolation.",
        Icon = "\ud83d\udce6",
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
            new() { Name = "VariableName", Label = "Variable Name", Type = "string", Required = true },
            new() { Name = "Value", Label = "Value", Type = "text", Required = true }
        }
    };

    public Task<ExecutionResult> ExecuteAsync(NodeConfig config, ExecutionContext context, CancellationToken ct = default)
    {
        var variableName = config.GetString("VariableName");
        var rawValue = config.GetString("Value");

        if (string.IsNullOrWhiteSpace(variableName))
        {
            return Task.FromResult(ExecutionResult.Failed("Variable name is required."));
        }

        // Interpolate the value to resolve any embedded variables
        var interpolatedValue = context.InterpolateString(rawValue);

        context.SetVariable(variableName, interpolatedValue);

        var result = ExecutionResult.Succeeded(
            new Dictionary<string, object?>
            {
                ["variableName"] = variableName,
                ["value"] = interpolatedValue
            },
            "output"
        );
        result.OutputVariables[variableName] = interpolatedValue;

        return Task.FromResult(result);
    }
}
