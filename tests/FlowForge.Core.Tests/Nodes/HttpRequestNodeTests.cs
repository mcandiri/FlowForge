using FlowForge.Core.Models;
using FlowForge.Core.Nodes.Actions;
using FluentAssertions;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Tests.Nodes;

public class HttpRequestNodeTests
{
    private readonly HttpRequestNode _node = new();

    [Fact]
    public void Type_ReturnsCorrectType()
    {
        _node.Type.Should().Be("http-request");
    }

    [Fact]
    public void Definition_HasRequiredPorts()
    {
        var def = _node.Definition;
        def.InputPorts.Should().ContainSingle(p => p.Name == "input");
        def.OutputPorts.Should().Contain(p => p.Name == "success");
        def.OutputPorts.Should().Contain(p => p.Name == "error");
    }

    [Fact]
    public async Task ExecuteAsync_GetRequest_ReturnsSuccessWithMockData()
    {
        // Arrange
        var config = new NodeConfig
        {
            ["Method"] = "GET",
            ["Url"] = "https://jsonplaceholder.typicode.com/users"
        };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputPort.Should().Be("success");
        result.Data.Should().NotBeNull();

        var data = result.Data as Dictionary<string, object?>;
        data.Should().NotBeNull();
        data!["statusCode"].Should().Be(200);
        data["method"].Should().Be("GET");
    }

    [Fact]
    public async Task ExecuteAsync_PostRequest_ReturnsSuccessWithCreatedData()
    {
        // Arrange
        var config = new NodeConfig
        {
            ["Method"] = "POST",
            ["Url"] = "https://api.example.com/data",
            ["Body"] = "{\"name\": \"test\"}"
        };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputPort.Should().Be("success");
        result.OutputVariables.Should().ContainKey("statusCode");
        result.OutputVariables["statusCode"].Should().Be(200);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyUrl_ReturnsError()
    {
        // Arrange
        var config = new NodeConfig
        {
            ["Method"] = "GET",
            ["Url"] = ""
        };
        var context = new ExecutionContext();

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeFalse();
        result.OutputPort.Should().Be("error");
    }

    [Fact]
    public async Task ExecuteAsync_SetsContextVariables()
    {
        // Arrange
        var config = new NodeConfig
        {
            ["Method"] = "GET",
            ["Url"] = "https://example.com/api"
        };
        var context = new ExecutionContext();

        // Act
        await _node.ExecuteAsync(config, context);

        // Assert
        context.GetVariable("statusCode").Should().Be(200);
        context.HasVariable("responseBody").Should().BeTrue();
        context.HasVariable("httpResponse").Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_InterpolatesUrlWithContextVariables()
    {
        // Arrange
        var config = new NodeConfig
        {
            ["Method"] = "GET",
            ["Url"] = "https://api.example.com/users/{{userId}}"
        };
        var context = new ExecutionContext();
        context.SetVariable("userId", "42");

        // Act
        var result = await _node.ExecuteAsync(config, context);

        // Assert
        result.Success.Should().BeTrue();
        var data = result.Data as Dictionary<string, object?>;
        data!["url"].Should().Be("https://api.example.com/users/42");
    }
}
