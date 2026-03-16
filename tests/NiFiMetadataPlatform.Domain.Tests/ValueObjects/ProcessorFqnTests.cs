using NiFiMetadataPlatform.Domain.ValueObjects;

namespace NiFiMetadataPlatform.Domain.Tests.ValueObjects;

public sealed class ProcessorFqnTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateFqn()
    {
        // Arrange
        var containerId = "w1";
        var processorId = "proc-123";

        // Act
        var fqn = ProcessorFqn.Create(containerId, processorId);

        // Assert
        fqn.Should().NotBeNull();
        fqn.Value.Should().Be("nifi://container/w1/processor/proc-123");
    }

    [Theory]
    [InlineData("", "proc-123")]
    [InlineData("w1", "")]
    [InlineData(null, "proc-123")]
    [InlineData("w1", null)]
    public void Create_WithInvalidParameters_ShouldThrowArgumentException(
        string containerId,
        string processorId)
    {
        // Act
        var act = () => ProcessorFqn.Create(containerId, processorId);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_WithValidFqn_ShouldParseFqn()
    {
        // Arrange
        var fqnString = "nifi://container/w1/processor/proc-123";

        // Act
        var fqn = ProcessorFqn.Parse(fqnString);

        // Assert
        fqn.Should().NotBeNull();
        fqn.Value.Should().Be(fqnString);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("http://container/w1/processor/proc-123")]
    [InlineData("nifi://wrong/w1/processor/proc-123")]
    public void Parse_WithInvalidFqn_ShouldThrowArgumentException(string fqnString)
    {
        // Act
        var act = () => ProcessorFqn.Parse(fqnString);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetContainerId_ShouldExtractContainerId()
    {
        // Arrange
        var fqn = ProcessorFqn.Create("w1", "proc-123");

        // Act
        var containerId = fqn.GetContainerId();

        // Assert
        containerId.Should().Be("w1");
    }

    [Fact]
    public void GetProcessorId_ShouldExtractProcessorId()
    {
        // Arrange
        var fqn = ProcessorFqn.Create("w1", "proc-123");

        // Act
        var processorId = fqn.GetProcessorId();

        // Assert
        processorId.Should().Be("proc-123");
    }

    [Fact]
    public void TryParse_WithValidFqn_ShouldReturnTrueAndParseFqn()
    {
        // Arrange
        var fqnString = "nifi://container/w1/processor/proc-123";

        // Act
        var success = ProcessorFqn.TryParse(fqnString, out var fqn);

        // Assert
        success.Should().BeTrue();
        fqn.Should().NotBeNull();
        fqn!.Value.Should().Be(fqnString);
    }

    [Fact]
    public void TryParse_WithInvalidFqn_ShouldReturnFalse()
    {
        // Arrange
        var fqnString = "invalid";

        // Act
        var success = ProcessorFqn.TryParse(fqnString, out var fqn);

        // Assert
        success.Should().BeFalse();
        fqn.Should().BeNull();
    }

    [Fact]
    public void ToString_ShouldReturnFqnValue()
    {
        // Arrange
        var fqn = ProcessorFqn.Create("w1", "proc-123");

        // Act
        var result = fqn.ToString();

        // Assert
        result.Should().Be("nifi://container/w1/processor/proc-123");
    }
}
