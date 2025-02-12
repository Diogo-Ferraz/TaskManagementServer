using MediatR;
using TaskManagement.Api.Application.TaskItems.DTOs;
using TaskManagement.Api.Domain.Common;

namespace TaskManagement.Api.Application.TaskItems.Queries
{
    public class GetTasksForProjectQuery : IRequest<Result<IReadOnlyList<TaskItemDto>>>
    {
        public Guid ProjectId { get; set; }
        public string RequestingUserId { get; set; }
    }
}
