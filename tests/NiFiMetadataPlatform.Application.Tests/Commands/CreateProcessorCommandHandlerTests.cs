using Microsoft.Extensions.Logging;
using NiFiMetadataPlatform.Application.Commands.CreateProcessor;
using NiFiMetadataPlatform.Application.Interfaces;
using NiFiMetadataPlatform.Domain.Entities;

namespace NiFiMetadataPlatform.Application.Tests.Commands;

public sealed class CreateProcessorCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGraphRepository _graphRepository;
    private readonly ISearchRepository _searchRepository;
    private readonly ILogger<CreateProcessorCommandHandler> _logger;
    private readonly CreateProcessorCommandHandler _handler;

    public CreateProcessorCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _graphRepository = Substitute.For<IGraphRepository>();
        _searchRepository = Substitute.For<ISearchRepository>();
        _logger = Substitute.For<ILogger<CreateProcessorCommandHandler>>();

        _unitOfWork.GraphRepository.Returns(_graphRepository);
        _unitOfWork.SearchRepository.Returns(_searchRepository);

        _handler = new CreateProcessorCommandHandler(_unitOfWork, _logger);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateProcessor()
    {
        // Arrange
        var command = new CreateProcessorCommand
        {
            ContainerId = "w1",
            ProcessorId = "proc-123",
            Name = "ExecuteSQL",
            Type = "org.apache.nifi.processors.standard.ExecuteSQL",
            ParentProcessGroupId = "pg-root",
            Properties = new Dictionary<string, string>
            {
                { "SQL select query", "SELECT * FROM users" }
            }
        };

        _graphRepository.AddVertexAsync(Arg.Any<NiFiProcessor>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _searchRepository.IndexEntityAsync(Arg.Any<NiFiProcessor>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("ExecuteSQL");
        result.Value.Fqn.Should().Be("nifi://container/w1/processor/proc-123");

        await _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _graphRepository.Received(1).AddVertexAsync(
            Arg.Any<NiFiProcessor>(),
            Arg.Any<CancellationToken>());
        await _searchRepository.Received(1).IndexEntityAsync(
            Arg.Any<NiFiProcessor>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenGraphRepositoryFails_ShouldRollbackAndReturnFailure()
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

        _graphRepository.AddVertexAsync(Arg.Any<NiFiProcessor>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure("Graph repository error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Graph repository error");

        await _unitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _searchRepository.DidNotReceive().IndexEntityAsync(
            Arg.Any<NiFiProcessor>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSearchRepositoryFails_ShouldRollbackAndReturnFailure()
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

        _graphRepository.AddVertexAsync(Arg.Any<NiFiProcessor>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _searchRepository.IndexEntityAsync(Arg.Any<NiFiProcessor>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure("Search repository error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Search repository error");

        await _unitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPropertiesAndTags_ShouldIncludeThemInProcessor()
    {
        // Arrange
        var command = new CreateProcessorCommand
        {
            ContainerId = "w1",
            ProcessorId = "proc-123",
            Name = "ExecuteSQL",
            Type = "org.apache.nifi.processors.standard.ExecuteSQL",
            ParentProcessGroupId = "pg-root",
            Properties = new Dictionary<string, string>
            {
                { "SQL select query", "SELECT * FROM users" },
                { "Database Connection", "dbcp-service" }
            },
            Description = "Test processor",
            Owner = "data_team",
            Tags = new List<string> { "production", "critical" }
        };

        _graphRepository.AddVertexAsync(Arg.Any<NiFiProcessor>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _searchRepository.IndexEntityAsync(Arg.Any<NiFiProcessor>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Properties.Should().HaveCount(2);
        result.Value.Properties["SQL select query"].Should().Be("SELECT * FROM users");
        result.Value.Description.Should().Be("Test processor");
        result.Value.Owner.Should().Be("data_team");
        result.Value.Tags.Should().Contain(new[] { "production", "critical" });
    }
}
