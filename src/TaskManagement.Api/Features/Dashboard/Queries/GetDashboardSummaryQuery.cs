using MediatR;
using TaskManagement.Api.Features.Dashboard.Models.DTOs;

namespace TaskManagement.Api.Features.Dashboard.Queries
{
    public class GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>
    {
    }
}
