using Api.Gateway.Models;

namespace Api.Gateway.Services;

public interface IKeycloakService
{
    Task<ServiceResult<TokenResponse>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<string>> GetAdminTokenAsync(
        CancellationToken cancellationToken = default);

    Task<ServiceResult<string>> CreateUserAsync(
        RegisterRequest request,
        string adminAccessToken,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> SetPasswordAsync(
        string userId,
        string password,
        string adminAccessToken,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> AssignRoleAsync(
        string userId,
        RegisterType registerType,
        string adminAccessToken,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteUserAsync(
        string userId,
        CancellationToken cancellationToken = default);
}