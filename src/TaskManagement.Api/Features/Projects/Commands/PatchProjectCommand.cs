using FluentValidation;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Infrastructure.Common.Models;

namespace TaskManagement.Api.Features.Projects.Commands
{
    public class PatchProjectCommand : IRequest<ProjectDto>
    {
        [SwaggerSchema(ReadOnly = true)]
        [JsonIgnore]
        public Guid Id { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<string?> Name { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<string?> Description { get; set; }
    }

    public class PatchProjectCommandValidator : AbstractValidator<PatchProjectCommand>
    {
        public PatchProjectCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Project ID is required.");

            RuleFor(x => x.Name.Value)
                .Must(name => name == null || !string.IsNullOrWhiteSpace(name))
                .WithMessage("Name cannot be empty.")
                .MaximumLength(100).When(x => x.Name.HasValue)
                .WithMessage("Name must not exceed 100 characters.");

            RuleFor(x => x.Description.Value)
                .MaximumLength(500).When(x => x.Description.HasValue && x.Description.Value != null)
                .WithMessage("Description must not exceed 500 characters.");

            RuleFor(x => x)
                .Must(cmd => cmd.Name.HasValue || cmd.Description.HasValue)
                .WithMessage("At least one field must be provided for patch.");
        }
    }
}
