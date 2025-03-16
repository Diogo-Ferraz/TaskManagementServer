using MediatR;
using TaskManagement.Api.Features.Tasks.Models.DTOs;
using TaskManagement.Api.Infrastructure.Common.Models;

namespace TaskManagement.Api.Features.Tasks.Queries
{
    public class GetTaskItemQuery : IRequest<Result<TaskItemDto>>
    {
        public Guid Id { get; set; }
        public string RequestingUserId { get; set; }
    }
}
