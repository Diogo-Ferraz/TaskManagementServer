using FluentValidation;
using MediatR;

namespace TaskManagement.Api.Features.Projects.Commands
{
    public class DeleteProjectCommand : IRequest
    {
        public Guid Id { get; set; }
    }

    public class DeleteProjectCommandValidator : AbstractValidator<DeleteProjectCommand>
    {
        public DeleteProjectCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Project ID is required");
        }
    }
}
