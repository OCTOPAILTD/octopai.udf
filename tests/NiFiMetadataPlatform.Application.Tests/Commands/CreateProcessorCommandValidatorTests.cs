using NiFiMetadataPlatform.Application.Commands.CreateProcessor;

namespace NiFiMetadataPlatform.Application.Tests.Commands;

public sealed class CreateProcessorCommandValidatorTests
{
    private readonly CreateProcessorCommandValidator _validator;

    public CreateProcessorCommandValidatorTests()
    {
        _validator = new CreateProcessorCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateProcessorCommand
        {
            ContainerId = "w1",
            ProcessorId = "proc-123",
            Name = "ExecuteSQL",
            Type = "org.apache.nifi.processors.standard.ExecuteSQL",
            ParentProcessGroupId = "pg-root"
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyContainerId_ShouldFail(string containerId)
    {
        // Arrange
        var command = new CreateProcessorCommand
        {
            ContainerId = containerId!,
            ProcessorId = "proc-123",
            Name = "ExecuteSQL",
            Type = "org.apache.nifi.processors.standard.ExecuteSQL",
            ParentProcessGroupId = "pg-root"
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContainerId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyName_ShouldFail(string name)
    {
        // Arrange
        var command = new CreateProcessorCommand
        {
            ContainerId = "w1",
            ProcessorId = "proc-123",
            Name = name!,
            Type = "org.apache.nifi.processors.standard.ExecuteSQL",
            ParentProcessGroupId = "pg-root"
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithTooLongName_ShouldFail()
    {
        // Arrange
        var command = new CreateProcessorCommand
        {
            ContainerId = "w1",
            ProcessorId = "proc-123",
            Name = new string('a', 501),
            Type = "org.apache.nifi.processors.standard.ExecuteSQL",
            ParentProcessGroupId = "pg-root"
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("InvalidType")]
    public async Task Validate_WithInvalidType_ShouldFail(string type)
    {
        // Arrange
        var command = new CreateProcessorCommand
        {
            ContainerId = "w1",
            ProcessorId = "proc-123",
            Name = "ExecuteSQL",
            Type = type,
            ParentProcessGroupId = "pg-root"
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }
}
