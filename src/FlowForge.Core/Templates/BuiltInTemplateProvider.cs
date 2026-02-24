using FlowForge.Core.Models;

namespace FlowForge.Core.Templates;

/// <summary>
/// Provides a collection of built-in demo workflow templates that showcase
/// the various node types and workflow patterns available in FlowForge.
/// </summary>
public class BuiltInTemplateProvider : ITemplateProvider
{
    private readonly List<WorkflowTemplate> _templates;

    public BuiltInTemplateProvider()
    {
        _templates = new List<WorkflowTemplate>
        {
            CreateApiDataPipeline(),
            CreateSmartNotification(),
            CreateDataSyncLoop(),
            CreateErrorHandlingDemo(),
            CreateMultiStepApproval()
        };
    }

    public IReadOnlyList<WorkflowTemplate> GetTemplates() => _templates;

    public WorkflowTemplate? GetTemplate(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _templates.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    // ─────────────────────── Template 1: API Data Pipeline ───────────────────────

    private static WorkflowTemplate CreateApiDataPipeline()
    {
        var webhookId = "t1-webhook";
        var httpId = "t1-http";
        var transformId = "t1-transform";
        var loggerId = "t1-logger";

        var nodes = new List<FlowNode>
        {
            CreateNode(webhookId, "webhook-trigger", "Webhook Trigger", 100, 250,
                new NodeConfig { ["Path"] = "/api/pipeline", ["Method"] = "POST" },
                inputPorts: new List<NodePort>(),
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{webhookId}-out", Name = "output", Type = PortType.Output, NodeId = webhookId }
                }),

            CreateNode(httpId, "http-request", "Fetch Users", 300, 250,
                new NodeConfig { ["Method"] = "GET", ["Url"] = "https://jsonplaceholder.typicode.com/users" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{httpId}-in", Name = "input", Type = PortType.Input, NodeId = httpId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{httpId}-success", Name = "success", Type = PortType.Output, NodeId = httpId },
                    new() { Id = $"{httpId}-error", Name = "error", Type = PortType.Output, NodeId = httpId }
                }),

            CreateNode(transformId, "transform", "Extract Names", 500, 250,
                new NodeConfig { ["Mapping"] = "{\"names\": \"{{responseBody}}\"}" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{transformId}-in", Name = "input", Type = PortType.Input, NodeId = transformId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{transformId}-out", Name = "output", Type = PortType.Output, NodeId = transformId }
                }),

            CreateNode(loggerId, "logger", "Log Results", 700, 250,
                new NodeConfig { ["Message"] = "Extracted data: {{names}}", ["Level"] = "Info" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{loggerId}-in", Name = "input", Type = PortType.Input, NodeId = loggerId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{loggerId}-out", Name = "output", Type = PortType.Output, NodeId = loggerId }
                })
        };

        var edges = new List<FlowEdge>
        {
            CreateEdge(webhookId, "output", httpId, "input"),
            CreateEdge(httpId, "success", transformId, "input"),
            CreateEdge(transformId, "output", loggerId, "input")
        };

        return new WorkflowTemplate
        {
            Id = "api-data-pipeline",
            Name = "API Data Pipeline",
            Description = "Fetch data from an external API, transform it, and log the results. Demonstrates a linear data-processing pipeline.",
            Icon = "\ud83d\udce1",
            Workflow = new Workflow
            {
                Id = "template-api-data-pipeline",
                Metadata = new WorkflowMetadata
                {
                    Name = "API Data Pipeline",
                    Description = "Fetch users from JSONPlaceholder, extract names, and log results.",
                    Author = "FlowForge",
                    Version = "1.0.0"
                },
                Nodes = nodes,
                Edges = edges
            }
        };
    }

    // ─────────────────────── Template 2: Smart Notification ──────────────────────

    private static WorkflowTemplate CreateSmartNotification()
    {
        var webhookId = "t2-webhook";
        var httpId = "t2-http";
        var conditionId = "t2-condition";
        var loggerOkId = "t2-logger-ok";
        var emailId = "t2-email";
        var delayId = "t2-delay";
        var httpRetryId = "t2-http-retry";

        var nodes = new List<FlowNode>
        {
            CreateNode(webhookId, "webhook-trigger", "Webhook Trigger", 100, 300,
                new NodeConfig { ["Path"] = "/api/notify", ["Method"] = "POST" },
                inputPorts: new List<NodePort>(),
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{webhookId}-out", Name = "output", Type = PortType.Output, NodeId = webhookId }
                }),

            CreateNode(httpId, "http-request", "Check Status", 280, 300,
                new NodeConfig { ["Method"] = "GET", ["Url"] = "https://api.example.com/status" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{httpId}-in", Name = "input", Type = PortType.Input, NodeId = httpId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{httpId}-success", Name = "success", Type = PortType.Output, NodeId = httpId },
                    new() { Id = $"{httpId}-error", Name = "error", Type = PortType.Output, NodeId = httpId }
                }),

            CreateNode(conditionId, "condition", "Status OK?", 460, 300,
                new NodeConfig { ["Expression"] = "{{statusCode}} == 200" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{conditionId}-in", Name = "input", Type = PortType.Input, NodeId = conditionId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{conditionId}-true", Name = "true", Type = PortType.Output, NodeId = conditionId },
                    new() { Id = $"{conditionId}-false", Name = "false", Type = PortType.Output, NodeId = conditionId }
                }),

            CreateNode(loggerOkId, "logger", "Log Success", 660, 180,
                new NodeConfig { ["Message"] = "Service is healthy. Status: {{statusCode}}", ["Level"] = "Info" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{loggerOkId}-in", Name = "input", Type = PortType.Input, NodeId = loggerOkId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{loggerOkId}-out", Name = "output", Type = PortType.Output, NodeId = loggerOkId }
                }),

            CreateNode(emailId, "email-sender", "Alert Team", 620, 420,
                new NodeConfig { ["To"] = "ops@company.com", ["Subject"] = "Service Down Alert", ["Body"] = "Service returned status {{statusCode}}. Investigating..." },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{emailId}-in", Name = "input", Type = PortType.Input, NodeId = emailId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{emailId}-success", Name = "success", Type = PortType.Output, NodeId = emailId },
                    new() { Id = $"{emailId}-error", Name = "error", Type = PortType.Output, NodeId = emailId }
                }),

            CreateNode(delayId, "delay", "Wait 5s", 760, 420,
                new NodeConfig { ["Duration"] = 5000 },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{delayId}-in", Name = "input", Type = PortType.Input, NodeId = delayId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{delayId}-out", Name = "output", Type = PortType.Output, NodeId = delayId }
                }),

            CreateNode(httpRetryId, "http-request", "Retry Check", 900, 420,
                new NodeConfig { ["Method"] = "GET", ["Url"] = "https://api.example.com/status" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{httpRetryId}-in", Name = "input", Type = PortType.Input, NodeId = httpRetryId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{httpRetryId}-success", Name = "success", Type = PortType.Output, NodeId = httpRetryId },
                    new() { Id = $"{httpRetryId}-error", Name = "error", Type = PortType.Output, NodeId = httpRetryId }
                })
        };

        var edges = new List<FlowEdge>
        {
            CreateEdge(webhookId, "output", httpId, "input"),
            CreateEdge(httpId, "success", conditionId, "input"),
            CreateEdge(conditionId, "true", loggerOkId, "input"),
            CreateEdge(conditionId, "false", emailId, "input"),
            CreateEdge(emailId, "success", delayId, "input"),
            CreateEdge(delayId, "output", httpRetryId, "input")
        };

        return new WorkflowTemplate
        {
            Id = "smart-notification",
            Name = "Smart Notification",
            Description = "Check service health, branch on status, send alerts on failure, then retry after a delay.",
            Icon = "\ud83d\udce2",
            Workflow = new Workflow
            {
                Id = "template-smart-notification",
                Metadata = new WorkflowMetadata
                {
                    Name = "Smart Notification",
                    Description = "Health-check a service and branch based on status code. Success logs, failure alerts and retries.",
                    Author = "FlowForge",
                    Version = "1.0.0"
                },
                Nodes = nodes,
                Edges = edges
            }
        };
    }

    // ─────────────────────── Template 3: Data Sync Loop ─────────────────────────

    private static WorkflowTemplate CreateDataSyncLoop()
    {
        var webhookId = "t3-webhook";
        var httpId = "t3-http";
        var transformId = "t3-transform";
        var loopId = "t3-loop";
        var httpItemId = "t3-http-item";
        var loggerItemId = "t3-logger-item";
        var loggerDoneId = "t3-logger-done";

        var nodes = new List<FlowNode>
        {
            CreateNode(webhookId, "webhook-trigger", "Webhook Trigger", 100, 300,
                new NodeConfig { ["Path"] = "/api/sync", ["Method"] = "POST" },
                inputPorts: new List<NodePort>(),
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{webhookId}-out", Name = "output", Type = PortType.Output, NodeId = webhookId }
                }),

            CreateNode(httpId, "http-request", "Fetch Data", 260, 300,
                new NodeConfig { ["Method"] = "GET", ["Url"] = "https://jsonplaceholder.typicode.com/posts" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{httpId}-in", Name = "input", Type = PortType.Input, NodeId = httpId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{httpId}-success", Name = "success", Type = PortType.Output, NodeId = httpId },
                    new() { Id = $"{httpId}-error", Name = "error", Type = PortType.Output, NodeId = httpId }
                }),

            CreateNode(transformId, "transform", "Prepare Items", 420, 300,
                new NodeConfig { ["Mapping"] = "{\"items\": \"{{responseBody}}\"}" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{transformId}-in", Name = "input", Type = PortType.Input, NodeId = transformId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{transformId}-out", Name = "output", Type = PortType.Output, NodeId = transformId }
                }),

            CreateNode(loopId, "loop", "For Each Item", 580, 300,
                new NodeConfig { ["Collection"] = "items", ["ItemVariable"] = "currentItem" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{loopId}-in", Name = "input", Type = PortType.Input, NodeId = loopId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{loopId}-iter", Name = "iteration", Type = PortType.Output, NodeId = loopId },
                    new() { Id = $"{loopId}-done", Name = "completed", Type = PortType.Output, NodeId = loopId }
                }),

            CreateNode(httpItemId, "http-request", "Sync Item", 700, 180,
                new NodeConfig { ["Method"] = "POST", ["Url"] = "https://api.example.com/sync", ["Body"] = "{{currentItem}}" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{httpItemId}-in", Name = "input", Type = PortType.Input, NodeId = httpItemId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{httpItemId}-success", Name = "success", Type = PortType.Output, NodeId = httpItemId },
                    new() { Id = $"{httpItemId}-error", Name = "error", Type = PortType.Output, NodeId = httpItemId }
                }),

            CreateNode(loggerItemId, "logger", "Log Item Sync", 860, 180,
                new NodeConfig { ["Message"] = "Synced item {{loopIndex}}: {{currentItem}}", ["Level"] = "Info" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{loggerItemId}-in", Name = "input", Type = PortType.Input, NodeId = loggerItemId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{loggerItemId}-out", Name = "output", Type = PortType.Output, NodeId = loggerItemId }
                }),

            CreateNode(loggerDoneId, "logger", "Sync Complete", 700, 450,
                new NodeConfig { ["Message"] = "Data sync completed successfully.", ["Level"] = "Info" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{loggerDoneId}-in", Name = "input", Type = PortType.Input, NodeId = loggerDoneId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{loggerDoneId}-out", Name = "output", Type = PortType.Output, NodeId = loggerDoneId }
                })
        };

        var edges = new List<FlowEdge>
        {
            CreateEdge(webhookId, "output", httpId, "input"),
            CreateEdge(httpId, "success", transformId, "input"),
            CreateEdge(transformId, "output", loopId, "input"),
            CreateEdge(loopId, "iteration", httpItemId, "input"),
            CreateEdge(httpItemId, "success", loggerItemId, "input"),
            CreateEdge(loopId, "completed", loggerDoneId, "input")
        };

        return new WorkflowTemplate
        {
            Id = "data-sync-loop",
            Name = "Data Sync Loop",
            Description = "Fetch items from an API, iterate through each one, POST them to a target service, and log the results.",
            Icon = "\ud83d\udd01",
            Workflow = new Workflow
            {
                Id = "template-data-sync-loop",
                Metadata = new WorkflowMetadata
                {
                    Name = "Data Sync Loop",
                    Description = "Demonstrates loop iteration: fetch, transform, and sync each item individually.",
                    Author = "FlowForge",
                    Version = "1.0.0"
                },
                Nodes = nodes,
                Edges = edges
            }
        };
    }

    // ─────────────────────── Template 4: Error Handling Demo ─────────────────────

    private static WorkflowTemplate CreateErrorHandlingDemo()
    {
        var webhookId = "t4-webhook";
        var codeBlockId = "t4-code";
        var loggerSuccessId = "t4-logger-success";
        var loggerErrorId = "t4-logger-error";

        var nodes = new List<FlowNode>
        {
            CreateNode(webhookId, "webhook-trigger", "Webhook Trigger", 100, 300,
                new NodeConfig { ["Path"] = "/api/risky", ["Method"] = "POST" },
                inputPorts: new List<NodePort>(),
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{webhookId}-out", Name = "output", Type = PortType.Output, NodeId = webhookId }
                }),

            CreateNode(codeBlockId, "code-block", "Risky Operation", 320, 300,
                new NodeConfig { ["Code"] = "var rng = new Random();\nvar value = rng.Next(0, 10);\nif (value < 5) throw new Exception(\"Random failure!\");\nvalue" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{codeBlockId}-in", Name = "input", Type = PortType.Input, NodeId = codeBlockId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{codeBlockId}-out", Name = "output", Type = PortType.Output, NodeId = codeBlockId },
                    new() { Id = $"{codeBlockId}-err", Name = "error", Type = PortType.Output, NodeId = codeBlockId }
                }),

            CreateNode(loggerSuccessId, "logger", "Log Success", 580, 180,
                new NodeConfig { ["Message"] = "Code executed successfully. Result: {{codeResult}}", ["Level"] = "Info" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{loggerSuccessId}-in", Name = "input", Type = PortType.Input, NodeId = loggerSuccessId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{loggerSuccessId}-out", Name = "output", Type = PortType.Output, NodeId = loggerSuccessId }
                }),

            CreateNode(loggerErrorId, "logger", "Log Failure", 580, 420,
                new NodeConfig { ["Message"] = "Code execution failed!", ["Level"] = "Error" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{loggerErrorId}-in", Name = "input", Type = PortType.Input, NodeId = loggerErrorId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{loggerErrorId}-out", Name = "output", Type = PortType.Output, NodeId = loggerErrorId }
                })
        };

        var edges = new List<FlowEdge>
        {
            CreateEdge(webhookId, "output", codeBlockId, "input"),
            CreateEdge(codeBlockId, "output", loggerSuccessId, "input"),
            CreateEdge(codeBlockId, "error", loggerErrorId, "input")
        };

        return new WorkflowTemplate
        {
            Id = "error-handling-demo",
            Name = "Error Handling Demo",
            Description = "Run a code block that may fail. Success and error paths each log their own message.",
            Icon = "\u26a0\ufe0f",
            Workflow = new Workflow
            {
                Id = "template-error-handling-demo",
                Metadata = new WorkflowMetadata
                {
                    Name = "Error Handling Demo",
                    Description = "Demonstrates error branching from a code block with separate success and failure paths.",
                    Author = "FlowForge",
                    Version = "1.0.0"
                },
                Nodes = nodes,
                Edges = edges
            }
        };
    }

    // ─────────────────────── Template 5: Multi-Step Approval ────────────────────

    private static WorkflowTemplate CreateMultiStepApproval()
    {
        var webhookId = "t5-webhook";
        var httpId = "t5-http";
        var conditionId = "t5-condition";
        var loggerApprovedId = "t5-logger-approved";
        var emailRejectedId = "t5-email-rejected";

        var nodes = new List<FlowNode>
        {
            CreateNode(webhookId, "webhook-trigger", "Webhook Trigger", 100, 300,
                new NodeConfig { ["Path"] = "/api/approval", ["Method"] = "POST" },
                inputPorts: new List<NodePort>(),
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{webhookId}-out", Name = "output", Type = PortType.Output, NodeId = webhookId }
                }),

            CreateNode(httpId, "http-request", "Fetch Request", 300, 300,
                new NodeConfig { ["Method"] = "GET", ["Url"] = "https://api.example.com/approval/pending" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{httpId}-in", Name = "input", Type = PortType.Input, NodeId = httpId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{httpId}-success", Name = "success", Type = PortType.Output, NodeId = httpId },
                    new() { Id = $"{httpId}-error", Name = "error", Type = PortType.Output, NodeId = httpId }
                }),

            CreateNode(conditionId, "condition", "Is Approved?", 500, 300,
                new NodeConfig { ["Expression"] = "{{statusCode}} == 200" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{conditionId}-in", Name = "input", Type = PortType.Input, NodeId = conditionId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{conditionId}-true", Name = "true", Type = PortType.Output, NodeId = conditionId },
                    new() { Id = $"{conditionId}-false", Name = "false", Type = PortType.Output, NodeId = conditionId }
                }),

            CreateNode(loggerApprovedId, "logger", "Approved", 720, 180,
                new NodeConfig { ["Message"] = "Request approved. Processing...", ["Level"] = "Info" },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{loggerApprovedId}-in", Name = "input", Type = PortType.Input, NodeId = loggerApprovedId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{loggerApprovedId}-out", Name = "output", Type = PortType.Output, NodeId = loggerApprovedId }
                }),

            CreateNode(emailRejectedId, "email-sender", "Rejected", 720, 420,
                new NodeConfig { ["To"] = "requester@company.com", ["Subject"] = "Request Rejected", ["Body"] = "Your approval request has been rejected." },
                inputPorts: new List<NodePort>
                {
                    new() { Id = $"{emailRejectedId}-in", Name = "input", Type = PortType.Input, NodeId = emailRejectedId }
                },
                outputPorts: new List<NodePort>
                {
                    new() { Id = $"{emailRejectedId}-success", Name = "success", Type = PortType.Output, NodeId = emailRejectedId },
                    new() { Id = $"{emailRejectedId}-error", Name = "error", Type = PortType.Output, NodeId = emailRejectedId }
                })
        };

        var edges = new List<FlowEdge>
        {
            CreateEdge(webhookId, "output", httpId, "input"),
            CreateEdge(httpId, "success", conditionId, "input"),
            CreateEdge(conditionId, "true", loggerApprovedId, "input"),
            CreateEdge(conditionId, "false", emailRejectedId, "input")
        };

        return new WorkflowTemplate
        {
            Id = "multi-step-approval",
            Name = "Multi-Step Approval",
            Description = "Fetch an approval request, check the result, approve or reject with appropriate notifications.",
            Icon = "\u2705",
            Workflow = new Workflow
            {
                Id = "template-multi-step-approval",
                Metadata = new WorkflowMetadata
                {
                    Name = "Multi-Step Approval",
                    Description = "Demonstrates conditional branching for an approval workflow with email notifications.",
                    Author = "FlowForge",
                    Version = "1.0.0"
                },
                Nodes = nodes,
                Edges = edges
            }
        };
    }

    // ─────────────────────── Helper methods ─────────────────────────────────────

    private static FlowNode CreateNode(
        string id, string type, string name,
        double x, double y, NodeConfig config,
        List<NodePort> inputPorts, List<NodePort> outputPorts)
    {
        return new FlowNode
        {
            Id = id,
            Type = type,
            Name = name,
            X = x,
            Y = y,
            Config = config,
            InputPorts = inputPorts,
            OutputPorts = outputPorts
        };
    }

    private static FlowEdge CreateEdge(
        string sourceNodeId, string sourcePortName,
        string targetNodeId, string targetPortName)
    {
        return new FlowEdge
        {
            Id = $"edge-{sourceNodeId}-{targetNodeId}",
            SourceNodeId = sourceNodeId,
            SourcePortName = sourcePortName,
            TargetNodeId = targetNodeId,
            TargetPortName = targetPortName
        };
    }
}
