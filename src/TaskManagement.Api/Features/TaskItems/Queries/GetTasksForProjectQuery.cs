using MediatR;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;

namespace TaskManagement.Api.Features.TaskItems.Queries
{
    public class GetTasksForProjectQuery : IRequest<IReadOnlyList<TaskItemDto>>
    {
        public Guid ProjectId { get; set; }
    }
}
