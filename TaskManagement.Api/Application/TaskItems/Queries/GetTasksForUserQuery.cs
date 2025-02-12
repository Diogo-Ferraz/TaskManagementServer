using MediatR;
using TaskManagement.Api.Application.TaskItems.DTOs;
using TaskManagement.Api.Domain.Common;

namespace TaskManagement.Api.Application.TaskItems.Queries
{
    public class GetTasksForUserQuery : IRequest<Result<IReadOnlyList<TaskItemDto>>>
    {
        public string UserId { get; set; }
    }
}
