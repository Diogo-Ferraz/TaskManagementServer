using FluentValidation;
using MediatR;
using TaskManagement.Api.Features.Projects.Models.DTOs;

namespace TaskManagement.Api.Features.Projects.Commands
{
    public class CreateProjectCommand : IRequest<ProjectDto>
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
    }

    public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
    {
        public CreateProjectCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
        }
    }
}
