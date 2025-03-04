using MediatR;
using TaskManagement.Api.Application.TaskItems.DTOs;
using TaskManagement.Api.Domain.Common;

namespace TaskManagement.Api.Application.TaskItems.Queries
{
    public class GetTaskItemQuery : IRequest<Result<TaskItemDto>>
    {
        public Guid Id { get; set; }
        public string RequestingUserId { get; set; }
    }
}
