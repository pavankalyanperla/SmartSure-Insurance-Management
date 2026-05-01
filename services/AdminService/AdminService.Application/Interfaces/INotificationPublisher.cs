using AdminService.Application.DTOs;

namespace AdminService.Application.Interfaces;

public interface INotificationPublisher
{
    Task PublishClaimStatusChangedAsync(ClaimStatusNotificationDto notification);
}
