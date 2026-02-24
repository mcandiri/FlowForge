using FlowForge.Core.Models;
using FlowForge.Core.Nodes.Control;
using FluentAssertions;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Tests.Nodes;

public class ConditionNodeTests
{
    private readonly ConditionNode _node = new();

    [Fact]
    public void Type_ReturnsCorrectType()
    {
        _node.Type.Should().Be("condition");
    }

    [Fact]
    public void Definition_HasTrueAndFalseOutputPorts()
    {
        var def = _node.Definition;
        def.OutputPorts.Should().Contain(p => p.Name == "true");
        def.OutputPorts.Should().Contain(p => p.Name == "false");
    }

    [Fact]
    public async Task ExecuteAsync_EqualNumbers_ReturnsTrue()
    {
        // Arrange
        var config = new NodeConfig { ["Expression"] = "200 == 200" };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputPort.Should().Be("true");
    }

    [Fact]
    public async Task ExecuteAsync_UnequalNumbers_ReturnsFalse()
    {
        // Arrange
        var config = new NodeConfig { ["Expression"] = "200 == 404" };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputPort.Should().Be("false");
    }

    [Fact]
    public async Task ExecuteAsync_NotEqual_ReturnsTrue()
    {
        // Arrange
        var config = new NodeConfig { ["Expression"] = "1 != 2" };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputPort.Should().Be("true");
    }

    [Fact]
    public async Task ExecuteAsync_GreaterThan_WorksCorrectly()
    {
        // Arrange
        var config = new NodeConfig { ["Expression"] = "10 > 5" };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.OutputPort.Should().Be("true");
    }

    [Fact]
    public async Task ExecuteAsync_LessThan_WorksCorrectly()
    {
        // Arrange
        var config = new NodeConfig { ["Expression"] = "3 < 1" };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.OutputPort.Should().Be("false");
    }

    [Fact]
    public async Task ExecuteAsync_InterpolatesVariables()
    {
        // Arrange
        var config = new NodeConfig { ["Expression"] = "{{statusCode}} == 200" };
        var context = new ExecutionContext();
        context.SetVariable("statusCode", 200);

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputPort.Should().Be("true");
    }

    [Fact]
    public async Task ExecuteAsync_TruthyValue_ReturnsTrue()
    {
        // Arrange - expression with no operator is evaluated as truthy
        var config = new NodeConfig { ["Expression"] = "something" };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.OutputPort.Should().Be("true");
    }

    [Fact]
    public async Task ExecuteAsync_FalsyValue_ReturnsFalse()
    {
        // Arrange
        var config = new NodeConfig { ["Expression"] = "false" };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.OutputPort.Should().Be("false");
    }

    [Fact]
    public async Task ExecuteAsync_EmptyExpression_ReturnsError()
    {
        // Arrange
        var config = new NodeConfig { ["Expression"] = "" };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_StringComparison_Works()
    {
        // Arrange
        var config = new NodeConfig { ["Expression"] = "\"hello\" == \"hello\"" };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.OutputPort.Should().Be("true");
    }

    [Fact]
    public async Task ExecuteAsync_SetsConditionResultVariable()
    {
        // Arrange
        var config = new NodeConfig { ["Expression"] = "1 == 1" };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.OutputVariables.Should().ContainKey("conditionResult");
        result.OutputVariables["conditionResult"].Should().Be(true);
    }
}
