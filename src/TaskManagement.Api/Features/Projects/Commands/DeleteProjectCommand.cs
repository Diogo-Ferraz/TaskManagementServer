using FluentValidation;
using MediatR;
using TaskManagement.Api.Infrastructure.Common.Models;

namespace TaskManagement.Api.Features.Projects.Commands
{
    public class DeleteProjectCommand : IRequest<Result<bool>>
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
    }

    public class DeleteProjectCommandValidator : AbstractValidator<DeleteProjectCommand>
    {
        public DeleteProjectCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Project ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Project User ID is required");
        }
    }
}
