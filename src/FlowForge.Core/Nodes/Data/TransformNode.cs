using System.Text.Json;
using FlowForge.Core.Engine;
using FlowForge.Core.Models;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Nodes.Data;

public class TransformNode : IFlowNode
{
    public string Type => "transform";

    public NodeDefinition Definition => new()
    {
        Type = Type,
        Name = "Transform",
        Description = "Transform data by mapping output keys to templates with variable interpolation.",
        Icon = "\ud83d\udd04",
        Category = NodeCategory.Data,
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
            new() { Name = "Mapping", Label = "Mapping (JSON: key -> template)", Type = "text", Required = true }
        }
    };

    public Task<ExecutionResult> ExecuteAsync(NodeConfig config, ExecutionContext context, CancellationToken ct = default)
    {
        try
        {
            var mappingRaw = config.GetString("Mapping", "{}");

            Dictionary<string, string>? mapping;

            // Parse the mapping JSON
            try
            {
                mapping = JsonSerializer.Deserialize<Dictionary<string, string>>(mappingRaw);
            }
            catch (JsonException ex)
            {
                return Task.FromResult(ExecutionResult.Failed($"Invalid mapping JSON: {ex.Message}"));
            }

            if (mapping is null || mapping.Count == 0)
            {
                return Task.FromResult(ExecutionResult.Failed("Mapping is empty or invalid."));
            }

            // Apply each mapping: interpolate the template and store the result
            var output = new Dictionary<string, object?>();
            foreach (var (key, template) in mapping)
            {
                var interpolated = context.InterpolateString(template);
                output[key] = interpolated;
                context.SetVariable(key, interpolated);
            }

            var result = ExecutionResult.Succeeded(output, "output");
            foreach (var (key, value) in output)
            {
                result.OutputVariables[key] = value;
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            return Task.FromResult(ExecutionResult.Failed($"Transform failed: {ex.Message}"));
        }
    }
}
