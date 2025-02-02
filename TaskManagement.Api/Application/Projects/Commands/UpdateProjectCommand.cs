using FluentValidation;
using MediatR;
using TaskManagement.Api.Application.Projects.DTOs;
using TaskManagement.Api.Domain.Common;

namespace TaskManagement.Api.Application.Projects.Commands
{
    public class UpdateProjectCommand : IRequest<Result<ProjectDto>>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string UserId { get; set; }
    }

    public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
    {
        public UpdateProjectCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Project ID is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Project User ID is required");
        }
    }
}
