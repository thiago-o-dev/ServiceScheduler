using Api.Gateway.Models;

namespace Api.Gateway.Services;

public interface IAuthService
{
    Task<ServiceResult<TokenResponse>> LoginAsync(
        TokenRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TokenResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);
}