using FluentValidation;
using MediatR;
using TaskManagement.Api.Infrastructure.Common.Models;

namespace TaskManagement.Api.Features.Tasks.Commands
{
    public class DeleteTaskItemCommand : IRequest<Result<bool>>
    {
        public Guid Id { get; set; }
        public string RequestingUserId { get; set; }
    }

    public class DeleteTaskItemCommandValidator : AbstractValidator<DeleteTaskItemCommand>
    {
        public DeleteTaskItemCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Task ID is required");

            RuleFor(x => x.RequestingUserId)
                .NotEmpty().WithMessage("Requesting User ID is required");
        }
    }
}
