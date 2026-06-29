using Api.Gateway.Models;

namespace Api.Gateway.Services;

public interface ISchedulerService
{
    Task<ServiceResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);
}