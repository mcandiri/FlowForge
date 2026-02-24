using FlowForge.Core.Engine;
using FlowForge.Core.Models;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Nodes.Actions;

public class EmailSenderNode : IFlowNode
{
    public string Type => "email-sender";

    public NodeDefinition Definition => new()
    {
        Type = Type,
        Name = "Send Email",
        Description = "Send an email notification. Logs the email details instead of actually sending in demo mode.",
        Icon = "\ud83d\udce7",
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
            new() { Name = "To", Label = "To", Type = "string", Required = true },
            new() { Name = "Subject", Label = "Subject", Type = "string", Required = true },
            new() { Name = "Body", Label = "Email Body", Type = "text", Required = true },
            new() { Name = "From", Label = "From", Type = "string", DefaultValue = "noreply@flowforge.dev" }
        }
    };

    public Task<ExecutionResult> ExecuteAsync(NodeConfig config, ExecutionContext context, CancellationToken ct = default)
    {
        try
        {
            var to = context.InterpolateString(config.GetString("To"));
            var subject = context.InterpolateString(config.GetString("Subject"));
            var body = context.InterpolateString(config.GetString("Body"));
            var from = context.InterpolateString(config.GetString("From", "noreply@flowforge.dev"));

            if (string.IsNullOrWhiteSpace(to))
            {
                return Task.FromResult(ExecutionResult.Failed("Recipient (To) is required.", "error"));
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                return Task.FromResult(ExecutionResult.Failed("Subject is required.", "error"));
            }

            // Demo mode: log the email instead of sending
            var emailLog = new Dictionary<string, object?>
            {
                ["from"] = from,
                ["to"] = to,
                ["subject"] = subject,
                ["body"] = body,
                ["sentAt"] = DateTime.UtcNow.ToString("O"),
                ["messageId"] = Guid.NewGuid().ToString("N")[..12]
            };

            context.SetVariable("emailResult", emailLog);
            context.SetVariable("emailMessageId", emailLog["messageId"]);

            var result = ExecutionResult.Succeeded(emailLog, "success");
            result.OutputVariables["emailResult"] = emailLog;
            result.OutputVariables["emailMessageId"] = emailLog["messageId"];

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            return Task.FromResult(ExecutionResult.Failed($"Email send failed: {ex.Message}", "error"));
        }
    }
}
