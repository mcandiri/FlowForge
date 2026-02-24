using FlowForge.Core.Engine;
using FlowForge.Core.Models;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Nodes.Triggers;

public class WebhookTriggerNode : IFlowNode
{
    public string Type => "webhook-trigger";

    public NodeDefinition Definition => new()
    {
        Type = Type,
        Name = "Webhook Trigger",
        Description = "Trigger a workflow via an incoming webhook request. This is the entry point for the workflow.",
        Icon = "\ud83d\udd14",
        Category = NodeCategory.Trigger,
        InputPorts = new List<PortDefinition>(),
        OutputPorts = new List<PortDefinition>
        {
            new() { Name = "output", Label = "Output" }
        },
        ConfigFields = new List<ConfigField>
        {
            new() { Name = "Path", Label = "Webhook Path", Type = "string", DefaultValue = "/webhook", Required = true },
            new() { Name = "Method", Label = "HTTP Method", Type = "select", DefaultValue = "POST", Options = new List<string> { "GET", "POST", "PUT", "DELETE" } }
        }
    };

    public Task<ExecutionResult> ExecuteAsync(NodeConfig config, ExecutionContext context, CancellationToken ct = default)
    {
        var path = config.GetString("Path", "/webhook");
        var method = config.GetString("Method", "POST");

        // Trigger nodes act as entry points -- pass through any data already in context
        var triggerData = new Dictionary<string, object?>
        {
            ["path"] = path,
            ["method"] = method,
            ["triggeredAt"] = DateTime.UtcNow.ToString("O"),
            ["payload"] = context.GetVariable("payload"),
            ["headers"] = context.GetVariable("headers"),
            ["queryParams"] = context.GetVariable("queryParams")
        };

        context.SetVariable("triggerData", triggerData);
        context.SetVariable("webhookPath", path);
        context.SetVariable("webhookMethod", method);

        var result = ExecutionResult.Succeeded(triggerData, "output");
        result.OutputVariables["triggerData"] = triggerData;
        result.OutputVariables["webhookPath"] = path;
        result.OutputVariables["webhookMethod"] = method;

        return Task.FromResult(result);
    }
}
