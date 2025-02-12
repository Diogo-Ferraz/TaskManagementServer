using FluentValidation;
using MediatR;
using TaskManagement.Api.Application.TaskItems.DTOs;
using TaskManagement.Api.Domain.Common;

namespace TaskManagement.Api.Application.TaskItems.Commands
{
    public class UpdateTaskItemCommand : IRequest<Result<TaskItemDto>>
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Domain.Entities.TaskStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        public string AssignedUserId { get; set; }
        public string RequestingUserId { get; set; }
    }

    public class UpdateTaskItemCommandValidator : AbstractValidator<UpdateTaskItemCommand>
    {
        public UpdateTaskItemCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Task ID is required");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

            RuleFor(x => x.AssignedUserId)
                .NotEmpty().WithMessage("Assigned User ID is required");

            RuleFor(x => x.RequestingUserId)
                .NotEmpty().WithMessage("Requesting User ID is required");
        }
    }
}
