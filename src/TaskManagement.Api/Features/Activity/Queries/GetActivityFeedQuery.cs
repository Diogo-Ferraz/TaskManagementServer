using MediatR;
using TaskManagement.Api.Features.Activity.Models.DTOs;

namespace TaskManagement.Api.Features.Activity.Queries
{
    public class GetActivityFeedQuery : IRequest<IReadOnlyList<ActivityLogDto>>
    {
        public Guid? ProjectId { get; set; }
        public int? Limit { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public bool MineOnly { get; set; }
    }
}
