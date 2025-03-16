using MediatR;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Infrastructure.Common.Models;

namespace TaskManagement.Api.Features.TaskItems.Queries
{
    public class GetTaskItemQuery : IRequest<Result<TaskItemDto>>
    {
        public Guid Id { get; set; }
        public string RequestingUserId { get; set; }
    }
}
