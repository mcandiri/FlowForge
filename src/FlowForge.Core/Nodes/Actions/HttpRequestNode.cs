using FlowForge.Core.Engine;
using FlowForge.Core.Models;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Nodes.Actions;

public class HttpRequestNode : IFlowNode
{
    public string Type => "http-request";

    public NodeDefinition Definition => new()
    {
        Type = Type,
        Name = "HTTP Request",
        Description = "Perform an HTTP request (GET, POST, PUT, DELETE). In demo mode returns mock response data.",
        Icon = "\ud83c\udf10",
        Category = NodeCategory.Action,
        InputPorts = new List<PortDefinition>
        {
            new() { Name = "input", Label = "Input" }
        },
        OutputPorts = new List<PortDefinition>
        {
            new() { Name = "success", Label = "Success" },
            new() { Name = "error", Label = "Error" }
        },
        ConfigFields = new List<ConfigField>
        {
            new() { Name = "Method", Label = "HTTP Method", Type = "select", DefaultValue = "GET", Options = new List<string> { "GET", "POST", "PUT", "DELETE" }, Required = true },
            new() { Name = "Url", Label = "URL", Type = "string", Required = true },
            new() { Name = "Headers", Label = "Headers (JSON)", Type = "text", DefaultValue = "{}" },
            new() { Name = "Body", Label = "Request Body", Type = "text", DefaultValue = "" }
        }
    };

    public Task<ExecutionResult> ExecuteAsync(NodeConfig config, ExecutionContext context, CancellationToken ct = default)
    {
        try
        {
            var method = config.GetString("Method", "GET").ToUpperInvariant();
            var url = context.InterpolateString(config.GetString("Url"));
            var body = context.InterpolateString(config.GetString("Body"));

            if (string.IsNullOrWhiteSpace(url))
            {
                return Task.FromResult(ExecutionResult.Failed("URL is required.", "error"));
            }

            // Demo mode: return mock response data instead of making actual HTTP calls
            var mockResponse = new Dictionary<string, object?>
            {
                ["statusCode"] = 200,
                ["method"] = method,
                ["url"] = url,
                ["headers"] = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                    ["X-Request-Id"] = Guid.NewGuid().ToString("N")[..8]
                },
                ["body"] = method switch
                {
                    "GET" => new Dictionary<string, object?> { ["data"] = new[] { "item1", "item2", "item3" }, ["total"] = 3 },
                    "POST" => new Dictionary<string, object?> { ["id"] = 1, ["created"] = true },
                    "PUT" => new Dictionary<string, object?> { ["id"] = 1, ["updated"] = true },
                    "DELETE" => new Dictionary<string, object?> { ["id"] = 1, ["deleted"] = true },
                    _ => (object)new Dictionary<string, object?> { ["message"] = "OK" }
                }
            };

            // Set response variables in context
            context.SetVariable("statusCode", 200);
            context.SetVariable("responseBody", mockResponse["body"]);
            context.SetVariable("httpResponse", mockResponse);

            var result = ExecutionResult.Succeeded(mockResponse, "success");
            result.OutputVariables["statusCode"] = 200;
            result.OutputVariables["responseBody"] = mockResponse["body"];
            result.OutputVariables["httpResponse"] = mockResponse;

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            return Task.FromResult(ExecutionResult.Failed($"HTTP request failed: {ex.Message}", "error"));
        }
    }
}
