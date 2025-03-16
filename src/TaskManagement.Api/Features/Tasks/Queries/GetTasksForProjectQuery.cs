using MediatR;
using TaskManagement.Api.Features.Tasks.Models.DTOs;
using TaskManagement.Api.Infrastructure.Common.Models;

namespace TaskManagement.Api.Features.Tasks.Queries
{
    public class GetTasksForProjectQuery : IRequest<Result<IReadOnlyList<TaskItemDto>>>
    {
        public Guid ProjectId { get; set; }
        public string RequestingUserId { get; set; }
    }
}
