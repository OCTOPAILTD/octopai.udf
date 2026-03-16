using NiFiMetadataPlatform.Domain.Entities;
using NiFiMetadataPlatform.Domain.Enums;
using NiFiMetadataPlatform.Domain.Events;
using NiFiMetadataPlatform.Domain.Exceptions;
using NiFiMetadataPlatform.Domain.ValueObjects;

namespace NiFiMetadataPlatform.Domain.Tests.Entities;

public sealed class NiFiProcessorTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateProcessor()
    {
        // Arrange
        var fqn = ProcessorFqn.Create("w1", "proc-123");
        var name = ProcessorName.Create("ExecuteSQL");
        var type = ProcessorType.Parse("org.apache.nifi.processors.standard.ExecuteSQL");
        var parentId = ProcessGroupId.Parse("pg-root");

        // Act
        var processor = NiFiProcessor.Create(fqn, name, type, parentId);

        // Assert
        processor.Should().NotBeNull();
        processor.Id.Should().NotBeNull();
        processor.Fqn.Should().Be(fqn);
        processor.Name.Should().Be(name);
        processor.Type.Should().Be(type);
        processor.Status.Should().Be(ProcessorStatus.Active);
        processor.ParentProcessGroupId.Should().Be(parentId);
        processor.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        processor.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_ShouldRaiseProcessorCreatedEvent()
    {
        // Arrange
        var fqn = ProcessorFqn.Create("w1", "proc-123");
        var name = ProcessorName.Create("ExecuteSQL");
        var type = ProcessorType.Parse("org.apache.nifi.processors.standard.ExecuteSQL");
        var parentId = ProcessGroupId.Parse("pg-root");

        // Act
        var processor = NiFiProcessor.Create(fqn, name, type, parentId);

        // Assert
        processor.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<ProcessorCreatedEvent>()
            .Which.ProcessorId.Should().Be(processor.Id);
    }

    [Fact]
    public void UpdateProperties_WithValidProperties_ShouldUpdateProperties()
    {
        // Arrange
        var processor = CreateTestProcessor();
        var properties = ProcessorProperties.Create(new Dictionary<string, string>
        {
            { "SQL select query", "SELECT * FROM users" },
            { "Database Connection", "dbcp-service" }
        });

        // Act
        processor.UpdateProperties(properties);

        // Assert
        processor.Properties.Should().Be(properties);
        processor.Properties.GetValue("SQL select query").Should().Be("SELECT * FROM users");
    }

    [Fact]
    public void UpdateProperties_ShouldRaiseProcessorPropertiesUpdatedEvent()
    {
        // Arrange
        var processor = CreateTestProcessor();
        processor.ClearDomainEvents();
        var properties = ProcessorProperties.Create(new Dictionary<string, string>
        {
            { "SQL select query", "SELECT * FROM users" }
        });

        // Act
        processor.UpdateProperties(properties);

        // Assert
        processor.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<ProcessorPropertiesUpdatedEvent>();
    }

    [Fact]
    public void UpdateProperties_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var processor = CreateTestProcessor();

        // Act
        var act = () => processor.UpdateProperties(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateDescription_ShouldUpdateDescription()
    {
        // Arrange
        var processor = CreateTestProcessor();
        var description = "This processor executes SQL queries";

        // Act
        processor.UpdateDescription(description);

        // Assert
        processor.Description.Should().Be(description);
    }

    [Fact]
    public void SetOwner_ShouldSetOwner()
    {
        // Arrange
        var processor = CreateTestProcessor();
        var owner = "data_team";

        // Act
        processor.SetOwner(owner);

        // Assert
        processor.Owner.Should().Be(owner);
    }

    [Fact]
    public void AddTag_WithValidTag_ShouldAddTag()
    {
        // Arrange
        var processor = CreateTestProcessor();
        var tag = "production";

        // Act
        processor.AddTag(tag);

        // Assert
        processor.Tags.Should().Contain(tag);
    }

    [Fact]
    public void AddTag_WithDuplicateTag_ShouldNotAddDuplicate()
    {
        // Arrange
        var processor = CreateTestProcessor();
        var tag = "production";

        // Act
        processor.AddTag(tag);
        processor.AddTag(tag);

        // Assert
        processor.Tags.Should().ContainSingle().Which.Should().Be(tag);
    }

    [Fact]
    public void RemoveTag_WithExistingTag_ShouldRemoveTag()
    {
        // Arrange
        var processor = CreateTestProcessor();
        var tag = "production";
        processor.AddTag(tag);

        // Act
        processor.RemoveTag(tag);

        // Assert
        processor.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldChangeStatusToInactive()
    {
        // Arrange
        var processor = CreateTestProcessor();

        // Act
        processor.Deactivate();

        // Assert
        processor.Status.Should().Be(ProcessorStatus.Inactive);
    }

    [Fact]
    public void Deactivate_ShouldRaiseProcessorDeactivatedEvent()
    {
        // Arrange
        var processor = CreateTestProcessor();
        processor.ClearDomainEvents();

        // Act
        processor.Deactivate();

        // Assert
        processor.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<ProcessorDeactivatedEvent>();
    }

    [Fact]
    public void Deactivate_WhenDeleted_ShouldThrowInvalidProcessorStateException()
    {
        // Arrange
        var processor = CreateTestProcessor();
        processor.Delete();

        // Act
        var act = () => processor.Deactivate();

        // Assert
        act.Should().Throw<InvalidProcessorStateException>()
            .WithMessage("*Cannot deactivate a deleted processor*");
    }

    [Fact]
    public void Activate_WhenInactive_ShouldChangeStatusToActive()
    {
        // Arrange
        var processor = CreateTestProcessor();
        processor.Deactivate();

        // Act
        processor.Activate();

        // Assert
        processor.Status.Should().Be(ProcessorStatus.Active);
    }

    [Fact]
    public void Activate_WhenDeleted_ShouldThrowInvalidProcessorStateException()
    {
        // Arrange
        var processor = CreateTestProcessor();
        processor.Delete();

        // Act
        var act = () => processor.Activate();

        // Assert
        act.Should().Throw<InvalidProcessorStateException>()
            .WithMessage("*Cannot activate a deleted processor*");
    }

    [Fact]
    public void Delete_ShouldChangeStatusToDeleted()
    {
        // Arrange
        var processor = CreateTestProcessor();

        // Act
        processor.Delete();

        // Assert
        processor.Status.Should().Be(ProcessorStatus.Deleted);
    }

    [Fact]
    public void Delete_ShouldRaiseProcessorDeletedEvent()
    {
        // Arrange
        var processor = CreateTestProcessor();
        processor.ClearDomainEvents();

        // Act
        processor.Delete();

        // Assert
        processor.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<ProcessorDeletedEvent>();
    }

    [Fact]
    public void HasSqlQuery_WithExecuteSqlProcessorAndSqlProperty_ShouldReturnTrue()
    {
        // Arrange
        var processor = CreateTestProcessor();
        var properties = ProcessorProperties.Create(new Dictionary<string, string>
        {
            { "SQL select query", "SELECT * FROM users" }
        });
        processor.UpdateProperties(properties);

        // Act
        var result = processor.HasSqlQuery();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetSqlQuery_WithExecuteSqlProcessor_ShouldReturnSqlQuery()
    {
        // Arrange
        var processor = CreateTestProcessor();
        var sqlQuery = "SELECT * FROM users";
        var properties = ProcessorProperties.Create(new Dictionary<string, string>
        {
            { "SQL select query", sqlQuery }
        });
        processor.UpdateProperties(properties);

        // Act
        var result = processor.GetSqlQuery();

        // Assert
        result.Should().Be(sqlQuery);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var processor = CreateTestProcessor();

        // Act
        processor.ClearDomainEvents();

        // Assert
        processor.GetDomainEvents().Should().BeEmpty();
    }

    private static NiFiProcessor CreateTestProcessor()
    {
        return NiFiProcessor.Create(
            ProcessorFqn.Create("w1", "proc-test-123"),
            ProcessorName.Create("ExecuteSQL"),
            ProcessorType.Parse("org.apache.nifi.processors.standard.ExecuteSQL"),
            ProcessGroupId.Parse("pg-root"));
    }
}
