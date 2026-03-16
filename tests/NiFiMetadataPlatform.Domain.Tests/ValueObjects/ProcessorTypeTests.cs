using NiFiMetadataPlatform.Domain.ValueObjects;

namespace NiFiMetadataPlatform.Domain.Tests.ValueObjects;

public sealed class ProcessorTypeTests
{
    [Fact]
    public void Parse_WithValidType_ShouldParseProcessorType()
    {
        // Arrange
        var type = "org.apache.nifi.processors.standard.ExecuteSQL";

        // Act
        var processorType = ProcessorType.Parse(type);

        // Assert
        processorType.Should().NotBeNull();
        processorType.Value.Should().Be(type);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("InvalidType")]
    public void Parse_WithInvalidType_ShouldThrowArgumentException(string type)
    {
        // Act
        var act = () => ProcessorType.Parse(type);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsExecuteSql_WithExecuteSqlProcessor_ShouldReturnTrue()
    {
        // Arrange
        var processorType = ProcessorType.Parse("org.apache.nifi.processors.standard.ExecuteSQL");

        // Act
        var result = processorType.IsExecuteSql();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExecuteSql_WithNonExecuteSqlProcessor_ShouldReturnFalse()
    {
        // Arrange
        var processorType = ProcessorType.Parse("org.apache.nifi.processors.standard.PutFile");

        // Act
        var result = processorType.IsExecuteSql();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsDatabaseProcessor_WithDatabaseProcessor_ShouldReturnTrue()
    {
        // Arrange
        var processorType = ProcessorType.Parse("org.apache.nifi.processors.standard.ExecuteSQL");

        // Act
        var result = processorType.IsDatabaseProcessor();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetSimpleName_ShouldReturnLastPartOfClassName()
    {
        // Arrange
        var processorType = ProcessorType.Parse("org.apache.nifi.processors.standard.ExecuteSQL");

        // Act
        var simpleName = processorType.GetSimpleName();

        // Assert
        simpleName.Should().Be("ExecuteSQL");
    }
}
