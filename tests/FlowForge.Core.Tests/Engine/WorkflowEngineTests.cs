using FlowForge.Core.Engine;
using FlowForge.Core.Models;
using FlowForge.Core.Nodes;
using FlowForge.Core.Nodes.Actions;
using FlowForge.Core.Nodes.Control;
using FlowForge.Core.Nodes.Triggers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Tests.Engine;

public class WorkflowEngineTests
{
    private readonly ILogger<WorkflowEngine> _logger = NullLogger<WorkflowEngine>.Instance;

    private NodeRegistry CreateRegistryWithNodes()
    {
        var registry = new NodeRegistry();
        registry.Register(new WebhookTriggerNode());
        registry.Register(new LoggerNode());
        registry.Register(new HttpRequestNode());
        registry.Register(new ConditionNode());
        registry.Register(new DelayNode());
        return registry;
    }

    [Fact]
    public async Task ExecuteAsync_SimpleLinearWorkflow_ExecutesAllNodes()
    {
        // Arrange
        var registry = CreateRegistryWithNodes();
        var engine = new WorkflowEngine(registry, _logger);

        var webhookId = "node-1";
        var loggerId = "node-2";

        var workflow = new Workflow
        {
            Id = "test-linear",
            Metadata = new WorkflowMetadata { Name = "Linear Test" },
            Nodes = new List<FlowNode>
            {
                new()
                {
                    Id = webhookId,
                    Type = "webhook-trigger",
                    Name = "Start",
                    Config = new NodeConfig { ["Path"] = "/test", ["Method"] = "POST" },
                    InputPorts = new List<NodePort>(),
                    OutputPorts = new List<NodePort>
                    {
                        new() { Name = "output", Type = PortType.Output, NodeId = webhookId }
                    }
                },
                new()
                {
                    Id = loggerId,
                    Type = "logger",
                    Name = "Log",
                    Config = new NodeConfig { ["Message"] = "Hello World", ["Level"] = "Info" },
                    InputPorts = new List<NodePort>
                    {
                        new() { Name = "input", Type = PortType.Input, NodeId = loggerId }
                    },
                    OutputPorts = new List<NodePort>
                    {
                        new() { Name = "output", Type = PortType.Output, NodeId = loggerId }
                    }
                }
            },
            Edges = new List<FlowEdge>
            {
                new()
                {
                    SourceNodeId = webhookId,
                    SourcePortName = "output",
                    TargetNodeId = loggerId,
                    TargetPortName = "input"
                }
            }
        };

        var context = new ExecutionContext();

        // Act
        var result = await engine.ExecuteAsync(workflow, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.NodesExecuted.Should().Be(2);
        result.Traces.Should().HaveCount(2);
        result.WorkflowId.Should().Be("test-linear");
    }

    [Fact]
    public async Task ExecuteAsync_ThreeNodeLinearWorkflow_ExecutesInOrder()
    {
        // Arrange
        var registry = CreateRegistryWithNodes();
        var engine = new WorkflowEngine(registry, _logger);

        var node1 = "n1";
        var node2 = "n2";
        var node3 = "n3";

        var workflow = new Workflow
        {
            Id = "test-three-node",
            Nodes = new List<FlowNode>
            {
                new()
                {
                    Id = node1, Type = "webhook-trigger", Name = "Trigger",
                    Config = new NodeConfig { ["Path"] = "/test" },
                    InputPorts = new List<NodePort>(),
                    OutputPorts = new List<NodePort> { new() { Name = "output", Type = PortType.Output, NodeId = node1 } }
                },
                new()
                {
                    Id = node2, Type = "http-request", Name = "HTTP",
                    Config = new NodeConfig { ["Method"] = "GET", ["Url"] = "https://example.com/api" },
                    InputPorts = new List<NodePort> { new() { Name = "input", Type = PortType.Input, NodeId = node2 } },
                    OutputPorts = new List<NodePort>
                    {
                        new() { Name = "success", Type = PortType.Output, NodeId = node2 },
                        new() { Name = "error", Type = PortType.Output, NodeId = node2 }
                    }
                },
                new()
                {
                    Id = node3, Type = "logger", Name = "Log",
                    Config = new NodeConfig { ["Message"] = "Done", ["Level"] = "Info" },
                    InputPorts = new List<NodePort> { new() { Name = "input", Type = PortType.Input, NodeId = node3 } },
                    OutputPorts = new List<NodePort> { new() { Name = "output", Type = PortType.Output, NodeId = node3 } }
                }
            },
            Edges = new List<FlowEdge>
            {
                new() { SourceNodeId = node1, SourcePortName = "output", TargetNodeId = node2, TargetPortName = "input" },
                new() { SourceNodeId = node2, SourcePortName = "success", TargetNodeId = node3, TargetPortName = "input" }
            }
        };

        var context = new ExecutionContext();

        // Act
        var result = await engine.ExecuteAsync(workflow, context);

        // Assert
        result.Success.Should().BeTrue();
        result.NodesExecuted.Should().Be(3);
        result.Traces.Count(t => t.Status == ExecutionStatus.Success).Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_ConditionBranching_FollowsTruePath()
    {
        // Arrange
        var registry = CreateRegistryWithNodes();
        var engine = new WorkflowEngine(registry, _logger);

        var conditionId = "cond";
        var trueLoggerId = "true-log";
        var falseLoggerId = "false-log";

        var workflow = new Workflow
        {
            Id = "test-condition-true",
            Nodes = new List<FlowNode>
            {
                new()
                {
                    Id = conditionId, Type = "condition", Name = "Check",
                    Config = new NodeConfig { ["Expression"] = "1 == 1" },
                    InputPorts = new List<NodePort>(),
                    OutputPorts = new List<NodePort>
                    {
                        new() { Name = "true", Type = PortType.Output, NodeId = conditionId },
                        new() { Name = "false", Type = PortType.Output, NodeId = conditionId }
                    }
                },
                new()
                {
                    Id = trueLoggerId, Type = "logger", Name = "True Path",
                    Config = new NodeConfig { ["Message"] = "Condition was true", ["Level"] = "Info" },
                    InputPorts = new List<NodePort> { new() { Name = "input", Type = PortType.Input, NodeId = trueLoggerId } },
                    OutputPorts = new List<NodePort> { new() { Name = "output", Type = PortType.Output, NodeId = trueLoggerId } }
                },
                new()
                {
                    Id = falseLoggerId, Type = "logger", Name = "False Path",
                    Config = new NodeConfig { ["Message"] = "Condition was false", ["Level"] = "Info" },
                    InputPorts = new List<NodePort> { new() { Name = "input", Type = PortType.Input, NodeId = falseLoggerId } },
                    OutputPorts = new List<NodePort> { new() { Name = "output", Type = PortType.Output, NodeId = falseLoggerId } }
                }
            },
            Edges = new List<FlowEdge>
            {
                new() { SourceNodeId = conditionId, SourcePortName = "true", TargetNodeId = trueLoggerId, TargetPortName = "input" },
                new() { SourceNodeId = conditionId, SourcePortName = "false", TargetNodeId = falseLoggerId, TargetPortName = "input" }
            }
        };

        var context = new ExecutionContext();

        // Act
        var result = await engine.ExecuteAsync(workflow, context);

        // Assert
        result.Success.Should().BeTrue();
        result.Traces.Should().Contain(t => t.NodeName == "True Path" && t.Status == ExecutionStatus.Success);
        result.Traces.Should().NotContain(t => t.NodeName == "False Path");
    }

    [Fact]
    public async Task ExecuteAsync_ConditionBranching_FollowsFalsePath()
    {
        // Arrange
        var registry = CreateRegistryWithNodes();
        var engine = new WorkflowEngine(registry, _logger);

        var conditionId = "cond";
        var trueLoggerId = "true-log";
        var falseLoggerId = "false-log";

        var workflow = new Workflow
        {
            Id = "test-condition-false",
            Nodes = new List<FlowNode>
            {
                new()
                {
                    Id = conditionId, Type = "condition", Name = "Check",
                    Config = new NodeConfig { ["Expression"] = "1 == 2" },
                    InputPorts = new List<NodePort>(),
                    OutputPorts = new List<NodePort>
                    {
                        new() { Name = "true", Type = PortType.Output, NodeId = conditionId },
                        new() { Name = "false", Type = PortType.Output, NodeId = conditionId }
                    }
                },
                new()
                {
                    Id = trueLoggerId, Type = "logger", Name = "True Path",
                    Config = new NodeConfig { ["Message"] = "true", ["Level"] = "Info" },
                    InputPorts = new List<NodePort> { new() { Name = "input", Type = PortType.Input, NodeId = trueLoggerId } },
                    OutputPorts = new List<NodePort> { new() { Name = "output", Type = PortType.Output, NodeId = trueLoggerId } }
                },
                new()
                {
                    Id = falseLoggerId, Type = "logger", Name = "False Path",
                    Config = new NodeConfig { ["Message"] = "false", ["Level"] = "Info" },
                    InputPorts = new List<NodePort> { new() { Name = "input", Type = PortType.Input, NodeId = falseLoggerId } },
                    OutputPorts = new List<NodePort> { new() { Name = "output", Type = PortType.Output, NodeId = falseLoggerId } }
                }
            },
            Edges = new List<FlowEdge>
            {
                new() { SourceNodeId = conditionId, SourcePortName = "true", TargetNodeId = trueLoggerId, TargetPortName = "input" },
                new() { SourceNodeId = conditionId, SourcePortName = "false", TargetNodeId = falseLoggerId, TargetPortName = "input" }
            }
        };

        var context = new ExecutionContext();

        // Act
        var result = await engine.ExecuteAsync(workflow, context);

        // Assert
        result.Success.Should().BeTrue();
        result.Traces.Should().Contain(t => t.NodeName == "False Path" && t.Status == ExecutionStatus.Success);
        result.Traces.Should().NotContain(t => t.NodeName == "True Path");
    }

    [Fact]
    public async Task ExecuteAsync_EmptyWorkflow_ReturnsErrorResult()
    {
        // Arrange
        var registry = CreateRegistryWithNodes();
        var engine = new WorkflowEngine(registry, _logger);

        var workflow = new Workflow
        {
            Id = "test-empty",
            Nodes = new List<FlowNode>(),
            Edges = new List<FlowEdge>()
        };

        var context = new ExecutionContext();

        // Act
        var result = await engine.ExecuteAsync(workflow, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
        result.NodesExecuted.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownNodeType_HandlesGracefully()
    {
        // Arrange
        var registry = CreateRegistryWithNodes();
        var engine = new WorkflowEngine(registry, _logger);

        var unknownId = "unknown-node";

        var workflow = new Workflow
        {
            Id = "test-unknown",
            Nodes = new List<FlowNode>
            {
                new()
                {
                    Id = unknownId,
                    Type = "nonexistent-node-type",
                    Name = "Unknown",
                    Config = new NodeConfig(),
                    InputPorts = new List<NodePort>(),
                    OutputPorts = new List<NodePort>
                    {
                        new() { Name = "output", Type = PortType.Output, NodeId = unknownId }
                    }
                }
            },
            Edges = new List<FlowEdge>()
        };

        var context = new ExecutionContext();

        // Act
        var result = await engine.ExecuteAsync(workflow, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Traces.Should().Contain(t => t.Status == ExecutionStatus.Error);
        result.Traces.First().Error.Should().Contain("nonexistent-node-type");
    }
}
