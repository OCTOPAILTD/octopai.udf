using NiFiMetadataPlatform.Domain.ValueObjects;

namespace NiFiMetadataPlatform.Domain.Tests.ValueObjects;

public sealed class ProcessorNameTests
{
    [Fact]
    public void Create_WithValidName_ShouldCreateProcessorName()
    {
        // Arrange
        var name = "ExecuteSQL";

        // Act
        var processorName = ProcessorName.Create(name);

        // Assert
        processorName.Should().NotBeNull();
        processorName.Value.Should().Be(name);
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimName()
    {
        // Arrange
        var name = "  ExecuteSQL  ";

        // Act
        var processorName = ProcessorName.Create(name);

        // Assert
        processorName.Value.Should().Be("ExecuteSQL");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string name)
    {
        // Act
        var act = () => ProcessorName.Create(name);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Create_WithTooLongName_ShouldThrowArgumentException()
    {
        // Arrange
        var name = new string('a', 501);

        // Act
        var act = () => ProcessorName.Create(name);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot exceed 500 characters*");
    }
}
