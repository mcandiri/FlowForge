using FlowForge.Core.Models;
using FlowForge.Core.Nodes.Actions;
using FluentAssertions;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Tests.Nodes;

public class CodeBlockNodeTests
{
    private readonly CodeBlockNode _node = new();

    [Fact]
    public void Type_ReturnsCorrectType()
    {
        _node.Type.Should().Be("code-block");
    }

    [Fact]
    public void Definition_HasOutputAndErrorPorts()
    {
        var def = _node.Definition;
        def.OutputPorts.Should().Contain(p => p.Name == "output");
        def.OutputPorts.Should().Contain(p => p.Name == "error");
    }

    [Fact]
    public void Definition_HasCodeConfigField()
    {
        var def = _node.Definition;
        def.ConfigFields.Should().Contain(f => f.Name == "Code" && f.Required);
    }

    [Fact]
    public void Definition_HasInputPort()
    {
        var def = _node.Definition;
        def.InputPorts.Should().ContainSingle(p => p.Name == "input");
    }

    [Fact]
    public async Task ExecuteAsync_CompilationError_ReturnsError()
    {
        // Arrange
        var config = new NodeConfig { ["Code"] = "this is not valid C#!!!" };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeFalse();
        result.OutputPort.Should().Be("error");
        result.Error.Should().Contain("Compilation error");
    }

    [Fact]
    public async Task ExecuteAsync_EmptyCode_ReturnsError()
    {
        // Arrange
        var config = new NodeConfig { ["Code"] = "" };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Code is required");
    }

    [Fact]
    public async Task ExecuteAsync_WhitespaceCode_ReturnsError()
    {
        // Arrange
        var config = new NodeConfig { ["Code"] = "   " };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Code is required");
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsExecutionResult()
    {
        // Arrange - any valid code; the node always returns an ExecutionResult
        var config = new NodeConfig { ["Code"] = "1 + 1" };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert - result is always returned (success or error); never null
        result.Should().NotBeNull();
        result.OutputPort.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_InvalidCode_HasErrorMessage()
    {
        // Arrange
        var config = new NodeConfig { ["Code"] = "class {}" };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
        result.OutputPort.Should().Be("error");
    }

    [Fact]
    public async Task ExecuteAsync_MissingCodeConfig_ReturnsError()
    {
        // Arrange - no Code key in config at all
        var config = new NodeConfig();
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Code is required");
    }

    [Fact]
    public async Task ExecuteAsync_ErrorResult_UsesErrorOutputPort()
    {
        // Arrange
        var config = new NodeConfig { ["Code"] = "invalid!!!" };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.OutputPort.Should().Be("error");
    }
}
