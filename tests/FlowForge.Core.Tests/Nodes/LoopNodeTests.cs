using FlowForge.Core.Models;
using FlowForge.Core.Nodes.Control;
using FluentAssertions;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Tests.Nodes;

public class LoopNodeTests
{
    private readonly LoopNode _node = new();

    [Fact]
    public void Type_ReturnsCorrectType()
    {
        _node.Type.Should().Be("loop");
    }

    [Fact]
    public void Definition_HasIterationAndCompletedPorts()
    {
        var def = _node.Definition;
        def.OutputPorts.Should().Contain(p => p.Name == "iteration");
        def.OutputPorts.Should().Contain(p => p.Name == "completed");
    }

    [Fact]
    public async Task ExecuteAsync_WithCollection_ReturnsIterationPort()
    {
        // Arrange
        var config = new NodeConfig
        {
            ["Collection"] = "items",
            ["ItemVariable"] = "item"
        };
        var context = new ExecutionContext();
        context.SetVariable("items", new List<object?> { "a", "b", "c" });

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputPort.Should().Be("iteration");
        context.GetVariable("item").Should().Be("a");
        context.GetVariable("loopIndex").Should().Be(0);
        context.GetVariable("loopCount").Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyCollectionVariable_ReturnsError()
    {
        // Arrange
        var config = new NodeConfig
        {
            ["Collection"] = "",
            ["ItemVariable"] = "item"
        };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_MissingCollectionVariable_ReturnsError()
    {
        // Arrange
        var config = new NodeConfig
        {
            ["Collection"] = "nonexistent",
            ["ItemVariable"] = "item"
        };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("nonexistent");
    }

    [Fact]
    public async Task ExecuteAsync_NonCollectionVariable_ReturnsError()
    {
        // Arrange
        var config = new NodeConfig
        {
            ["Collection"] = "notAList",
            ["ItemVariable"] = "item"
        };
        var context = new ExecutionContext();
        context.SetVariable("notAList", "just a string");

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not a collection");
    }

    [Fact]
    public async Task ExecuteAsync_SetsItemVariable()
    {
        // Arrange
        var config = new NodeConfig
        {
            ["Collection"] = "data",
            ["ItemVariable"] = "currentItem"
        };
        var context = new ExecutionContext();
        context.SetVariable("data", new List<object?> { "first", "second" });

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeTrue();
        context.GetVariable("currentItem").Should().Be("first");
        result.OutputVariables.Should().ContainKey("currentItem");
    }

    [Fact]
    public async Task ExecuteAsync_OutputContainsLoopMetadata()
    {
        // Arrange
        var config = new NodeConfig
        {
            ["Collection"] = "items",
            ["ItemVariable"] = "item"
        };
        var context = new ExecutionContext();
        context.SetVariable("items", new List<object?> { 10, 20, 30 });

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        var data = result.Data as Dictionary<string, object?>;
        data.Should().NotBeNull();
        data!["index"].Should().Be(0);
        data["total"].Should().Be(3);
        data["item"].Should().Be(10);
    }
}
