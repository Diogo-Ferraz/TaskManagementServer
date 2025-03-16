using MediatR;
using TaskManagement.Api.Features.Tasks.Models.DTOs;
using TaskManagement.Api.Infrastructure.Common.Models;

namespace TaskManagement.Api.Features.Tasks.Queries
{
    public class GetTasksForUserQuery : IRequest<Result<IReadOnlyList<TaskItemDto>>>
    {
        public string UserId { get; set; }
    }
}
