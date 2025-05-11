using MediatR;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;

namespace TaskManagement.Api.Features.TaskItems.Queries
{
    public class GetTasksForUserQuery : IRequest<IReadOnlyList<TaskItemDto>>
    {
    }
}
