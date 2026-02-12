using FluentValidation;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;
using TaskManagement.Api.Infrastructure.Common.Models;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Features.TaskItems.Commands
{
    public class PatchTaskItemCommand : IRequest<TaskItemDto>
    {
        [SwaggerSchema(ReadOnly = true)]
        [JsonIgnore]
        public Guid Id { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<string?> Title { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<string?> Description { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<TaskStatus> Status { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<DateTime?> DueDate { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Optional<string?> AssignedUserId { get; set; }
    }

    public class PatchTaskItemCommandValidator : AbstractValidator<PatchTaskItemCommand>
    {
        public PatchTaskItemCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();

            RuleFor(x => x.Title.Value)
                .Must(title => title == null || !string.IsNullOrWhiteSpace(title))
                .WithMessage("Title cannot be empty.")
                .MaximumLength(200).When(x => x.Title.HasValue)
                .WithMessage("Title must not exceed 200 characters.");

            RuleFor(x => x.Description.Value)
                .MaximumLength(1000).When(x => x.Description.HasValue && x.Description.Value != null)
                .WithMessage("Description must not exceed 1000 characters.");

            RuleFor(x => x)
                .Must(cmd =>
                    cmd.Title.HasValue ||
                    cmd.Description.HasValue ||
                    cmd.Status.HasValue ||
                    cmd.DueDate.HasValue ||
                    cmd.AssignedUserId.HasValue)
                .WithMessage("At least one field must be provided for patch.");
        }
    }
}
