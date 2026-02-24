using System.Collections;
using FlowForge.Core.Engine;
using FlowForge.Core.Models;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Nodes.Control;

public class LoopNode : IFlowNode
{
    public string Type => "loop";

    public NodeDefinition Definition => new()
    {
        Type = Type,
        Name = "Loop",
        Description = "Iterate over a collection stored in a workflow variable. Sets the current item variable on each iteration.",
        Icon = "\ud83d\udd01",
        Category = NodeCategory.Control,
        InputPorts = new List<PortDefinition>
        {
            new() { Name = "input", Label = "Input" }
        },
        OutputPorts = new List<PortDefinition>
        {
            new() { Name = "iteration", Label = "Iteration" },
            new() { Name = "completed", Label = "Completed" }
        },
        ConfigFields = new List<ConfigField>
        {
            new() { Name = "Collection", Label = "Collection Variable", Type = "string", Required = true },
            new() { Name = "ItemVariable", Label = "Item Variable Name", Type = "string", DefaultValue = "item", Required = true }
        }
    };

    public Task<ExecutionResult> ExecuteAsync(NodeConfig config, ExecutionContext context, CancellationToken ct = default)
    {
        var collectionName = config.GetString("Collection");
        var itemVariable = config.GetString("ItemVariable", "item");

        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return Task.FromResult(ExecutionResult.Failed("Collection variable name is required."));
        }

        var collectionObj = context.GetVariable(collectionName);

        if (collectionObj is null)
        {
            return Task.FromResult(ExecutionResult.Failed($"Variable '{collectionName}' not found or is null."));
        }

        // Try to get items as a list
        var items = new List<object?>();
        if (collectionObj is IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
            {
                items.Add(item);
            }
        }
        else
        {
            return Task.FromResult(ExecutionResult.Failed($"Variable '{collectionName}' is not a collection."));
        }

        // Track iteration index
        var indexKey = $"_loop_{collectionName}_index";
        var currentIndex = context.GetVariable<int>(indexKey, 0);

        if (currentIndex < items.Count)
        {
            // Set the current item and index in context
            context.SetVariable(itemVariable, items[currentIndex]);
            context.SetVariable("loopIndex", currentIndex);
            context.SetVariable("loopCount", items.Count);
            context.SetVariable(indexKey, currentIndex + 1);

            var result = ExecutionResult.Succeeded(
                new Dictionary<string, object?>
                {
                    ["item"] = items[currentIndex],
                    ["index"] = currentIndex,
                    ["total"] = items.Count
                },
                "iteration"
            );
            result.OutputVariables[itemVariable] = items[currentIndex];
            result.OutputVariables["loopIndex"] = currentIndex;
            result.OutputVariables["loopCount"] = items.Count;

            return Task.FromResult(result);
        }
        else
        {
            // Reset the index for potential re-use
            context.SetVariable(indexKey, 0);

            var result = ExecutionResult.Succeeded(
                new Dictionary<string, object?>
                {
                    ["message"] = "Loop completed",
                    ["totalIterations"] = items.Count
                },
                "completed"
            );
            return Task.FromResult(result);
        }
    }
}
