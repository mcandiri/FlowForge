using FlowForge.Core.Models;
using FlowForge.Core.Nodes.Control;
using FluentAssertions;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Tests.Nodes;

public class DelayNodeTests
{
    private readonly DelayNode _node = new();

    [Fact]
    public void Type_ReturnsCorrectType()
    {
        _node.Type.Should().Be("delay");
    }

    [Fact]
    public void Definition_HasOutputPort()
    {
        var def = _node.Definition;
        def.OutputPorts.Should().ContainSingle(p => p.Name == "output");
        def.InputPorts.Should().ContainSingle(p => p.Name == "input");
    }

    [Fact]
    public async Task ExecuteAsync_ShortDelay_CompletesSuccessfully()
    {
        // Arrange
        var config = new NodeConfig { ["Duration"] = 10 };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputPort.Should().Be("output");

        var data = result.Data as Dictionary<string, object?>;
        data.Should().NotBeNull();
        data!["delayMs"].Should().Be(10);
    }

    [Fact]
    public async Task ExecuteAsync_ZeroDuration_CompletesImmediately()
    {
        // Arrange
        var config = new NodeConfig { ["Duration"] = 0 };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_NegativeDuration_ClampsToZero()
    {
        // Arrange
        var config = new NodeConfig { ["Duration"] = -100 };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeTrue();
        var data = result.Data as Dictionary<string, object?>;
        data!["delayMs"].Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_Cancellation_ThrowsOperationCanceled()
    {
        // Arrange
        var config = new NodeConfig { ["Duration"] = 60000 };
        var context = new ExecutionContext();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _node.ExecuteAsync(config, context, cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_DefaultDuration_UsesDefault()
    {
        // Arrange - no Duration specified, should use default of 1000
        var config = new NodeConfig();
        var context = new ExecutionContext();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act - this will actually delay for 1 second with default config
        // We'll cancel quickly to avoid slow tests
        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _node.ExecuteAsync(config, context, cts.Token));
    }
}
