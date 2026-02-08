using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using TaskManagement.Api.Features.Activity.Hubs;
using TaskManagement.Api.Features.Activity.Models;
using TaskManagement.Api.Features.Activity.Models.DTOs;
using TaskManagement.Api.Features.Activity.Services.Interfaces;

namespace TaskManagement.Api.Features.Activity.Services
{
    public class SignalRActivityPublisher : IActivityPublisher
    {
        private readonly IHubContext<ActivityHub> _hubContext;
        private readonly IMapper _mapper;

        public SignalRActivityPublisher(IHubContext<ActivityHub> hubContext, IMapper mapper)
        {
            _hubContext = hubContext;
            _mapper = mapper;
        }

        public async Task PublishAsync(ActivityLog activityLog, CancellationToken cancellationToken)
        {
            if (!activityLog.ProjectId.HasValue)
            {
                return;
            }

            var dto = _mapper.Map<ActivityLogDto>(activityLog);
            await _hubContext.Clients
                .Groups(ActivityHub.GetProjectGroupName(activityLog.ProjectId.Value), ActivityHub.GetAdminGroupName())
                .SendAsync(ActivityHubEvents.ActivityCreated, dto, cancellationToken);
        }
    }
}
