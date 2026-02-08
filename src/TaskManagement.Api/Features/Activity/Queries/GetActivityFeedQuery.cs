using MediatR;
using TaskManagement.Api.Features.Activity.Models.DTOs;

namespace TaskManagement.Api.Features.Activity.Queries
{
    public class GetActivityFeedQuery : IRequest<IReadOnlyList<ActivityLogDto>>
    {
        public Guid? ProjectId { get; set; }
        public int Limit { get; set; } = 50;
    }
}
