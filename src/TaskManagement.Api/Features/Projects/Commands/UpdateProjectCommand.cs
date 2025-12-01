using FluentValidation;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;
using TaskManagement.Api.Features.Projects.Models.DTOs;

namespace TaskManagement.Api.Features.Projects.Commands
{
    public class UpdateProjectCommand : IRequest<ProjectDto>
    {
        [SwaggerSchema(ReadOnly = true)]
        [JsonIgnore]
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
    {
        public UpdateProjectCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Project ID is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
        }
    }
}
