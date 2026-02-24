using FluentAssertions;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Tests.Engine;

public class ExecutionContextTests
{
    [Fact]
    public void SetVariable_And_GetVariable_RoundTrips()
    {
        // Arrange
        var context = new ExecutionContext();

        // Act
        context.SetVariable("name", "FlowForge");

        // Assert
        context.GetVariable("name").Should().Be("FlowForge");
    }

    [Fact]
    public void SetVariable_OverwritesExistingValue()
    {
        // Arrange
        var context = new ExecutionContext();
        context.SetVariable("count", 1);

        // Act
        context.SetVariable("count", 42);

        // Assert
        context.GetVariable("count").Should().Be(42);
    }

    [Fact]
    public void GetVariable_NonExistent_ReturnsNull()
    {
        // Arrange
        var context = new ExecutionContext();

        // Act
        var result = context.GetVariable("missing");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetVariable_IsCaseInsensitive()
    {
        // Arrange
        var context = new ExecutionContext();
        context.SetVariable("MyVar", "value");

        // Act & Assert
        context.GetVariable("myvar").Should().Be("value");
        context.GetVariable("MYVAR").Should().Be("value");
        context.GetVariable("MyVar").Should().Be("value");
    }

    [Fact]
    public void HasVariable_ReturnsTrueWhenExists()
    {
        // Arrange
        var context = new ExecutionContext();
        context.SetVariable("exists", true);

        // Act & Assert
        context.HasVariable("exists").Should().BeTrue();
    }

    [Fact]
    public void HasVariable_ReturnsFalseWhenMissing()
    {
        // Arrange
        var context = new ExecutionContext();

        // Act & Assert
        context.HasVariable("missing").Should().BeFalse();
    }

    [Fact]
    public void HasVariable_IsCaseInsensitive()
    {
        // Arrange
        var context = new ExecutionContext();
        context.SetVariable("TestKey", 123);

        // Act & Assert
        context.HasVariable("testkey").Should().BeTrue();
        context.HasVariable("TESTKEY").Should().BeTrue();
    }

    [Fact]
    public void GetVariable_Generic_ReturnsTypedValue()
    {
        // Arrange
        var context = new ExecutionContext();
        context.SetVariable("count", 42);

        // Act
        var result = context.GetVariable<int>("count");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void GetVariable_Generic_ReturnsDefaultWhenMissing()
    {
        // Arrange
        var context = new ExecutionContext();

        // Act
        var result = context.GetVariable<int>("missing", -1);

        // Assert
        result.Should().Be(-1);
    }

    [Fact]
    public void GetVariable_Generic_ConvertsType()
    {
        // Arrange
        var context = new ExecutionContext();
        context.SetVariable("number", 42);

        // Act
        var result = context.GetVariable<string>("number");

        // Assert
        result.Should().Be("42");
    }

    [Fact]
    public void GetVariable_Generic_ReturnsDefaultOnConversionFailure()
    {
        // Arrange
        var context = new ExecutionContext();
        context.SetVariable("text", "not-a-number");

        // Act
        var result = context.GetVariable<int>("text", -999);

        // Assert
        result.Should().Be(-999);
    }

    [Fact]
    public void InterpolateString_ReplacesVariables()
    {
        // Arrange
        var context = new ExecutionContext();
        context.SetVariable("name", "World");
        context.SetVariable("greeting", "Hello");

        // Act
        var result = context.InterpolateString("{{greeting}}, {{name}}!");

        // Assert
        result.Should().Be("Hello, World!");
    }

    [Fact]
    public void InterpolateString_LeavesUnknownVariablesUntouched()
    {
        // Arrange
        var context = new ExecutionContext();
        context.SetVariable("known", "yes");

        // Act
        var result = context.InterpolateString("{{known}} and {{unknown}}");

        // Assert
        // InterpolateString only replaces variables that exist in the context;
        // unknown placeholders remain as-is in the output string.
        result.Should().Be("yes and {{unknown}}");
    }

    [Fact]
    public void InterpolateString_EmptyTemplate_ReturnsEmpty()
    {
        // Arrange
        var context = new ExecutionContext();

        // Act
        var result = context.InterpolateString("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void InterpolateString_NullTemplate_ReturnsNull()
    {
        // Arrange
        var context = new ExecutionContext();

        // Act
        var result = context.InterpolateString(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void InterpolateString_NoVariables_ReturnsSameString()
    {
        // Arrange
        var context = new ExecutionContext();

        // Act
        var result = context.InterpolateString("No variables here");

        // Assert
        result.Should().Be("No variables here");
    }

    [Fact]
    public void Variables_Property_ReturnsAllSetVariables()
    {
        // Arrange
        var context = new ExecutionContext();
        context.SetVariable("a", 1);
        context.SetVariable("b", "two");
        context.SetVariable("c", true);

        // Act
        var variables = context.Variables;

        // Assert
        variables.Should().HaveCount(3);
        variables.Should().ContainKey("a");
        variables.Should().ContainKey("b");
        variables.Should().ContainKey("c");
    }

    [Fact]
    public void SetVariable_NullValue_StoredAndRetrievable()
    {
        // Arrange
        var context = new ExecutionContext();

        // Act
        context.SetVariable("nullVal", null);

        // Assert
        context.HasVariable("nullVal").Should().BeTrue();
        context.GetVariable("nullVal").Should().BeNull();
    }
}
