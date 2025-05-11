using MediatR;

namespace TaskManagement.Api.Features.TaskItems.Commands
{
    public class DeleteTaskItemCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
