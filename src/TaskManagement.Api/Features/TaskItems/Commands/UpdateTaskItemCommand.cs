using FluentValidation;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Features.TaskItems.Commands
{
    public class UpdateTaskItemCommand : IRequest<TaskItemDto>
    {
        [SwaggerSchema(ReadOnly = true)]
        [JsonIgnore]
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        public string? AssignedUserId { get; set; }
    }

    public class UpdateTaskItemCommandValidator : AbstractValidator<UpdateTaskItemCommand>
    {
        public UpdateTaskItemCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");
            RuleFor(x => x.Status).IsInEnum();
        }
    }
}
