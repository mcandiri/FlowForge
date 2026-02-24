using FlowForge.Core.Models;
using FlowForge.Core.Nodes.Data;
using FluentAssertions;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Tests.Nodes;

public class TransformNodeTests
{
    private readonly TransformNode _node = new();

    [Fact]
    public void Type_ReturnsCorrectType()
    {
        _node.Type.Should().Be("transform");
    }

    [Fact]
    public void Definition_HasOutputPort()
    {
        var def = _node.Definition;
        def.OutputPorts.Should().ContainSingle(p => p.Name == "output");
    }

    [Fact]
    public async Task ExecuteAsync_ValidMapping_TransformsData()
    {
        // Arrange
        var config = new NodeConfig
        {
            ["Mapping"] = "{\"greeting\": \"Hello, {{name}}!\"}"
        };
        var context = new ExecutionContext();
        context.SetVariable("name", "World");

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputPort.Should().Be("output");

        var data = result.Data as Dictionary<string, object?>;
        data.Should().NotBeNull();
        data!["greeting"].Should().Be("Hello, World!");
    }

    [Fact]
    public async Task ExecuteAsync_MultipleKeys_AllTransformed()
    {
        // Arrange
        var config = new NodeConfig
        {
            ["Mapping"] = "{\"first\": \"{{a}}\", \"second\": \"{{b}}\"}"
        };
        var context = new ExecutionContext();
        context.SetVariable("a", "Alpha");
        context.SetVariable("b", "Beta");

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeTrue();
        var data = result.Data as Dictionary<string, object?>;
        data!["first"].Should().Be("Alpha");
        data["second"].Should().Be("Beta");
    }

    [Fact]
    public async Task ExecuteAsync_SetsContextVariables()
    {
        // Arrange
        var config = new NodeConfig
        {
            ["Mapping"] = "{\"outputKey\": \"value123\"}"
        };
        var context = new ExecutionContext();

        // Act
        await _node.ExecuteAsync(config, context);

        // Assert
        context.GetVariable("outputKey").Should().Be("value123");
    }

    [Fact]
    public async Task ExecuteAsync_InvalidJson_ReturnsError()
    {
        // Arrange
        var config = new NodeConfig
        {
            ["Mapping"] = "not valid json"
        };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Invalid mapping JSON");
    }

    [Fact]
    public async Task ExecuteAsync_EmptyMapping_ReturnsError()
    {
        // Arrange
        var config = new NodeConfig
        {
            ["Mapping"] = "{}"
        };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public async Task ExecuteAsync_OutputVariablesPopulated()
    {
        // Arrange
        var config = new NodeConfig
        {
            ["Mapping"] = "{\"result\": \"computed\"}"
        };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.OutputVariables.Should().ContainKey("result");
        result.OutputVariables["result"].Should().Be("computed");
    }
}
