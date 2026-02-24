using FlowForge.Core.Engine;
using FlowForge.Core.Models;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Nodes.Control;

public class RetryNode : IFlowNode
{
    public string Type => "retry";

    public NodeDefinition Definition => new()
    {
        Type = Type,
        Name = "Retry",
        Description = "Retry a failed operation up to a maximum number of times with a delay between attempts.",
        Icon = "\ud83d\udd03",
        Category = NodeCategory.Control,
        InputPorts = new List<PortDefinition>
        {
            new() { Name = "input", Label = "Input" }
        },
        OutputPorts = new List<PortDefinition>
        {
            new() { Name = "success", Label = "Success" },
            new() { Name = "failed", Label = "Failed" }
        },
        ConfigFields = new List<ConfigField>
        {
            new() { Name = "MaxRetries", Label = "Max Retries", Type = "number", DefaultValue = 3, Required = true },
            new() { Name = "DelayMs", Label = "Delay Between Retries (ms)", Type = "number", DefaultValue = 1000 }
        }
    };

    public async Task<ExecutionResult> ExecuteAsync(NodeConfig config, ExecutionContext context, CancellationToken ct = default)
    {
        var maxRetries = config.GetInt("MaxRetries", 3);
        var delayMs = config.GetInt("DelayMs", 1000);

        if (maxRetries < 1) maxRetries = 1;
        if (delayMs < 0) delayMs = 0;

        // Track retry state using context variables
        var retryCountKey = "_retry_count";
        var retryStatusKey = "_retry_status";
        var currentAttempt = context.GetVariable<int>(retryCountKey, 0);
        var lastStatus = context.GetVariable<string>(retryStatusKey, "pending");

        // If the last attempt succeeded or this is a fresh start, route to success
        if (lastStatus == "success")
        {
            context.SetVariable(retryCountKey, 0);
            context.SetVariable(retryStatusKey, "pending");

            return ExecutionResult.Succeeded(
                new Dictionary<string, object?>
                {
                    ["message"] = "Operation succeeded",
                    ["attempts"] = currentAttempt
                },
                "success"
            );
        }

        // If we still have retries remaining
        if (currentAttempt < maxRetries)
        {
            if (currentAttempt > 0 && delayMs > 0)
            {
                await Task.Delay(delayMs, ct);
            }

            context.SetVariable(retryCountKey, currentAttempt + 1);
            context.SetVariable("retryAttempt", currentAttempt + 1);
            context.SetVariable("retryMax", maxRetries);

            return ExecutionResult.Succeeded(
                new Dictionary<string, object?>
                {
                    ["attempt"] = currentAttempt + 1,
                    ["maxRetries"] = maxRetries,
                    ["message"] = $"Retry attempt {currentAttempt + 1} of {maxRetries}"
                },
                "success"
            );
        }

        // All retries exhausted
        context.SetVariable(retryCountKey, 0);
        context.SetVariable(retryStatusKey, "pending");

        return ExecutionResult.Failed(
            $"All {maxRetries} retry attempts exhausted.",
            "failed"
        );
    }
}
