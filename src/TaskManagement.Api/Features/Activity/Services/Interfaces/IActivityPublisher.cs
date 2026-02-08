using TaskManagement.Api.Features.Activity.Models;

namespace TaskManagement.Api.Features.Activity.Services.Interfaces
{
    public interface IActivityPublisher
    {
        Task PublishAsync(ActivityLog activityLog, CancellationToken cancellationToken);
    }
}
