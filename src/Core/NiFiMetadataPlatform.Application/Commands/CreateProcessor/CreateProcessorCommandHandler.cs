using Microsoft.Extensions.Logging;
using NiFiMetadataPlatform.Application.Common;
using NiFiMetadataPlatform.Application.Interfaces;
using NiFiMetadataPlatform.Domain.Entities;
using NiFiMetadataPlatform.Domain.ValueObjects;

namespace NiFiMetadataPlatform.Application.Commands.CreateProcessor;

/// <summary>
/// Handler for CreateProcessorCommand.
/// </summary>
public sealed class CreateProcessorCommandHandler
    : ICommandHandler<CreateProcessorCommand, Result<ProcessorDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateProcessorCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProcessorCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="logger">The logger.</param>
    public CreateProcessorCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateProcessorCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the CreateProcessorCommand.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the created processor DTO.</returns>
    public async Task<Result<ProcessorDto>> Handle(
        CreateProcessorCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Creating processor {ProcessorId} in container {ContainerId}",
                command.ProcessorId,
                command.ContainerId);

            var fqn = ProcessorFqn.Create(command.ContainerId, command.ProcessorId);
            var name = ProcessorName.Create(command.Name);
            var type = ProcessorType.Parse(command.Type);
            var parentId = ProcessGroupId.Parse(command.ParentProcessGroupId);

            var processor = NiFiProcessor.Create(fqn, name, type, parentId);

            if (command.Properties.Count > 0)
            {
                var properties = ProcessorProperties.Create(command.Properties);
                processor.UpdateProperties(properties);
            }

            if (!string.IsNullOrWhiteSpace(command.Description))
            {
                processor.UpdateDescription(command.Description);
            }

            if (!string.IsNullOrWhiteSpace(command.Owner))
            {
                processor.SetOwner(command.Owner);
            }

            foreach (var tag in command.Tags)
            {
                processor.AddTag(tag);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // Processors are stored in OpenSearch only (not in ArangoDB)
            var searchResult = await _unitOfWork.SearchRepository.IndexEntityAsync(
                processor,
                cancellationToken);

            if (searchResult.IsFailure)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return Result<ProcessorDto>.Failure(searchResult.Error!);
            }

            var commitResult = await _unitOfWork.CommitAsync(cancellationToken);

            if (commitResult.IsFailure)
            {
                return Result<ProcessorDto>.Failure(commitResult.Error!);
            }

            _logger.LogInformation(
                "Successfully created processor {Fqn}",
                processor.Fqn.Value);

            var dto = MapToDto(processor);
            return Result<ProcessorDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating processor {ProcessorId}",
                command.ProcessorId);

            await _unitOfWork.RollbackAsync(cancellationToken);
            return Result<ProcessorDto>.Failure($"Failed to create processor: {ex.Message}");
        }
    }

    private static ProcessorDto MapToDto(NiFiProcessor processor)
    {
        return new ProcessorDto
        {
            Id = processor.Id.Value.ToString(),
            Fqn = processor.Fqn.Value,
            Name = processor.Name.Value,
            Type = processor.Type.Value,
            Status = processor.Status.ToString(),
            Properties = processor.Properties.ToDictionary(),
            ParentProcessGroupId = processor.ParentProcessGroupId.Value,
            Description = processor.Description,
            Owner = processor.Owner,
            Tags = processor.Tags.ToList(),
            CreatedAt = processor.CreatedAt,
            UpdatedAt = processor.UpdatedAt
        };
    }
}
