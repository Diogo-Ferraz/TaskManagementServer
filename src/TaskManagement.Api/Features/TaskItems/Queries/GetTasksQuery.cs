using MediatR;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Features.TaskItems.Queries
{
    public class GetTasksQuery : IRequest<IReadOnlyList<TaskItemDto>>
    {
        public Guid? ProjectId { get; set; }
        public string? AssignedUserId { get; set; }
        public TaskStatus? Status { get; set; }
        public bool? UnassignedOnly { get; set; }
        public int? Limit { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
