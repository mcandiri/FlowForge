using FlowForge.Core.Export;
using FlowForge.Core.Models;
using FluentAssertions;

namespace FlowForge.Core.Tests.Export;

public class JsonExporterTests
{
    private readonly JsonExporter _exporter = new();

    [Fact]
    public void Format_ReturnsJson()
    {
        _exporter.Format.Should().Be("json");
    }

    [Fact]
    public void Export_ProducesValidJson()
    {
        // Arrange
        var workflow = CreateSampleWorkflow();

        // Act
        var json = _exporter.Export(workflow);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("Test Workflow");
        json.Should().Contain("webhook-trigger");
    }

    [Fact]
    public void Export_Import_RoundTrip_PreservesWorkflowId()
    {
        // Arrange
        var workflow = CreateSampleWorkflow();

        // Act
        var json = _exporter.Export(workflow);
        var imported = JsonExporter.Import(json);

        // Assert
        imported.Should().NotBeNull();
        imported!.Id.Should().Be(workflow.Id);
    }

    [Fact]
    public void Export_Import_RoundTrip_PreservesMetadata()
    {
        // Arrange
        var workflow = CreateSampleWorkflow();

        // Act
        var json = _exporter.Export(workflow);
        var imported = JsonExporter.Import(json);

        // Assert
        imported!.Metadata.Name.Should().Be("Test Workflow");
        imported.Metadata.Description.Should().Be("A test workflow");
        imported.Metadata.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void Export_Import_RoundTrip_PreservesNodes()
    {
        // Arrange
        var workflow = CreateSampleWorkflow();

        // Act
        var json = _exporter.Export(workflow);
        var imported = JsonExporter.Import(json);

        // Assert
        imported!.Nodes.Should().HaveCount(workflow.Nodes.Count);
        imported.Nodes[0].Type.Should().Be("webhook-trigger");
        imported.Nodes[0].Name.Should().Be("Start");
        imported.Nodes[1].Type.Should().Be("logger");
    }

    [Fact]
    public void Export_Import_RoundTrip_PreservesEdges()
    {
        // Arrange
        var workflow = CreateSampleWorkflow();

        // Act
        var json = _exporter.Export(workflow);
        var imported = JsonExporter.Import(json);

        // Assert
        imported!.Edges.Should().HaveCount(workflow.Edges.Count);
        imported.Edges[0].SourcePortName.Should().Be("output");
        imported.Edges[0].TargetPortName.Should().Be("input");
    }

    [Fact]
    public void Export_Import_RoundTrip_PreservesNodeConfig()
    {
        // Arrange
        var workflow = CreateSampleWorkflow();

        // Act
        var json = _exporter.Export(workflow);
        var imported = JsonExporter.Import(json);

        // Assert
        var loggerNode = imported!.Nodes.First(n => n.Type == "logger");
        loggerNode.Config.GetString("Message").Should().Be("Hello from test");
    }

    [Fact]
    public void Export_Import_RoundTrip_PreservesNodePositions()
    {
        // Arrange
        var workflow = CreateSampleWorkflow();

        // Act
        var json = _exporter.Export(workflow);
        var imported = JsonExporter.Import(json);

        // Assert
        imported!.Nodes[0].X.Should().Be(100);
        imported.Nodes[0].Y.Should().Be(200);
    }

    [Fact]
    public void Export_EmptyWorkflow_ProducesValidJson()
    {
        // Arrange
        var workflow = new Workflow
        {
            Id = "empty",
            Metadata = new WorkflowMetadata { Name = "Empty" },
            Nodes = new List<FlowNode>(),
            Edges = new List<FlowEdge>()
        };

        // Act
        var json = _exporter.Export(workflow);
        var imported = JsonExporter.Import(json);

        // Assert
        imported.Should().NotBeNull();
        imported!.Nodes.Should().BeEmpty();
        imported.Edges.Should().BeEmpty();
    }

    [Fact]
    public void Import_InvalidJson_ThrowsJsonException()
    {
        // Act & Assert
        // JsonSerializer throws JsonException on invalid JSON
        var act = () => JsonExporter.Import("{ invalid json");
        act.Should().Throw<System.Text.Json.JsonException>();
    }

    private static Workflow CreateSampleWorkflow()
    {
        var webhookId = "node-webhook";
        var loggerId = "node-logger";

        return new Workflow
        {
            Id = "test-workflow-001",
            Metadata = new WorkflowMetadata
            {
                Name = "Test Workflow",
                Description = "A test workflow",
                Author = "Tester",
                Version = "1.0.0"
            },
            Nodes = new List<FlowNode>
            {
                new()
                {
                    Id = webhookId,
                    Type = "webhook-trigger",
                    Name = "Start",
                    X = 100,
                    Y = 200,
                    Config = new NodeConfig { ["Path"] = "/test", ["Method"] = "POST" },
                    InputPorts = new List<NodePort>(),
                    OutputPorts = new List<NodePort>
                    {
                        new() { Id = "port-1", Name = "output", Type = PortType.Output, NodeId = webhookId }
                    }
                },
                new()
                {
                    Id = loggerId,
                    Type = "logger",
                    Name = "Log",
                    X = 350,
                    Y = 200,
                    Config = new NodeConfig { ["Message"] = "Hello from test", ["Level"] = "Info" },
                    InputPorts = new List<NodePort>
                    {
                        new() { Id = "port-2", Name = "input", Type = PortType.Input, NodeId = loggerId }
                    },
                    OutputPorts = new List<NodePort>
                    {
                        new() { Id = "port-3", Name = "output", Type = PortType.Output, NodeId = loggerId }
                    }
                }
            },
            Edges = new List<FlowEdge>
            {
                new()
                {
                    Id = "edge-1",
                    SourceNodeId = webhookId,
                    SourcePortName = "output",
                    TargetNodeId = loggerId,
                    TargetPortName = "input"
                }
            }
        };
    }
}
