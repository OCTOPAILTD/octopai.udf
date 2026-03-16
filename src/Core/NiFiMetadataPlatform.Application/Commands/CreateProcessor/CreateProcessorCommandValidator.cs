using FluentValidation;

namespace NiFiMetadataPlatform.Application.Commands.CreateProcessor;

/// <summary>
/// Validator for CreateProcessorCommand.
/// </summary>
public sealed class CreateProcessorCommandValidator : AbstractValidator<CreateProcessorCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProcessorCommandValidator"/> class.
    /// </summary>
    public CreateProcessorCommandValidator()
    {
        RuleFor(x => x.ContainerId)
            .NotEmpty()
            .WithMessage("Container ID is required");

        RuleFor(x => x.ProcessorId)
            .NotEmpty()
            .WithMessage("Processor ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Processor name is required")
            .MaximumLength(500)
            .WithMessage("Processor name cannot exceed 500 characters");

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Processor type is required")
            .Must(type => type.Contains('.'))
            .WithMessage("Processor type must be a fully qualified class name");

        RuleFor(x => x.ParentProcessGroupId)
            .NotEmpty()
            .WithMessage("Parent process group ID is required");
    }
}
