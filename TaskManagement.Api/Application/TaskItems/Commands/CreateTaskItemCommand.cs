using FluentValidation;
using MediatR;
using TaskManagement.Api.Application.TaskItems.DTOs;
using TaskManagement.Api.Domain.Common;

namespace TaskManagement.Api.Application.TaskItems.Commands
{
    public class CreateTaskItemCommand : IRequest<Result<TaskItemDto>>
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public Domain.Entities.TaskStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid ProjectId { get; set; }
        public string AssignedUserId { get; set; }
        public string RequestingUserId { get; set; }
    }

    public class CreateTaskItemCommandValidator : AbstractValidator<CreateTaskItemCommand>
    {
        public CreateTaskItemCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Project ID is required");

            RuleFor(x => x.AssignedUserId)
                .NotEmpty().WithMessage("Assigned User ID is required");

            RuleFor(x => x.RequestingUserId)
                .NotEmpty().WithMessage("Requesting User ID is required");
        }
    }
}
