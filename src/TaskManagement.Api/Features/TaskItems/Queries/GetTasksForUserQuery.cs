using MediatR;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Infrastructure.Common.Models;

namespace TaskManagement.Api.Features.TaskItems.Queries
{
    public class GetTasksForUserQuery : IRequest<Result<IReadOnlyList<TaskItemDto>>>
    {
        public string UserId { get; set; }
    }
}
