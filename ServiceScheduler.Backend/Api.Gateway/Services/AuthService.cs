using Api.Gateway.Models;

namespace Api.Gateway.Services;

public sealed class AuthService(
    IKeycloakService keycloakService,
    ISchedulerService schedulerService)
    : IAuthService
{
    public async Task<ServiceResult<TokenResponse>> LoginAsync(
        TokenRequest request,
        CancellationToken cancellationToken = default)
    {
        return await keycloakService.LoginAsync(
            request.Email,
            request.Password,
            cancellationToken);
    }

    public async Task<ServiceResult<TokenResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminToken = await keycloakService.GetAdminTokenAsync(cancellationToken);

        if (!adminToken.Success)
        {
            return new ServiceResult<TokenResponse>(
                false,
                StatusCode: adminToken.StatusCode,
                Content: adminToken.Content,
                ContentType: adminToken.ContentType);
        }

        var user = await keycloakService.CreateUserAsync(
            request,
            adminToken.Value!,
            cancellationToken);

        if (!user.Success)
        {
            return new ServiceResult<TokenResponse>(
                false,
                StatusCode: user.StatusCode,
                Content: user.Content,
                ContentType: user.ContentType);
        }

        var password = await keycloakService.SetPasswordAsync(
            user.Value!,
            request.Password,
            adminToken.Value!,
            cancellationToken);

        if (!password.Success)
        {
            await keycloakService.DeleteUserAsync(user.Value!, cancellationToken);

            return new ServiceResult<TokenResponse>(
                false,
                StatusCode: password.StatusCode,
                Content: password.Content,
                ContentType: password.ContentType);
        }

        var role = await keycloakService.AssignRoleAsync(
            user.Value!,
            request.RegisterType,
            adminToken.Value!,
            cancellationToken);

        if (!role.Success)
        {
            await keycloakService.DeleteUserAsync(user.Value!, cancellationToken);

            return new ServiceResult<TokenResponse>(
                false,
                StatusCode: role.StatusCode,
                Content: role.Content,
                ContentType: role.ContentType);
        }

        var scheduler = await schedulerService.RegisterAsync(
            request,
            cancellationToken);

        if (!scheduler.Success)
        {
            await keycloakService.DeleteUserAsync(user.Value!, cancellationToken);

            return new ServiceResult<TokenResponse>(
                false,
                StatusCode: scheduler.StatusCode,
                Content: scheduler.Content,
                ContentType: scheduler.ContentType);
        }

        return await keycloakService.LoginAsync(
            request.Email,
            request.Password,
            cancellationToken);
    }
}