using Api.Gateway.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Api.Gateway.Services;

public sealed class KeycloakService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<KeycloakService> logger)
    : IKeycloakService
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("keycloak");

    private string Realm => configuration["Keycloak:Realm"]!;
    private string ClientId => configuration["Keycloak:ClientId"]!;
    private string AdminUser => configuration["Keycloak:AdminUser"]!;
    private string AdminPassword => configuration["Keycloak:AdminPassword"]!;

    public async Task<ServiceResult<TokenResponse>> LoginAsync(
    string email,
    string password,
    CancellationToken cancellationToken = default)
    {
        email = email.Trim().ToLowerInvariant();

        var form = new FormUrlEncodedContent(
        [
            new("grant_type", "password"),
        new("client_id", ClientId),
        new("username", email),
        new("password", password)
        ]);

        var response = await _client.PostAsync(
            $"/realms/{Realm}/protocol/openid-connect/token",
            form,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new ServiceResult<TokenResponse>(
                false,
                StatusCode: (int)response.StatusCode,
                Content: body,
                ContentType: response.Content.Headers.ContentType?.ToString() ?? "application/json");
        }

        using var json = JsonDocument.Parse(body);

        return new ServiceResult<TokenResponse>(
            true,
            new TokenResponse(
                json.RootElement.GetProperty("access_token").GetString()!,
                json.RootElement.GetProperty("expires_in").GetInt32()));
    }

    public async Task<ServiceResult<string>> GetAdminTokenAsync(
    CancellationToken cancellationToken = default)
    {
        var form = new FormUrlEncodedContent(
        [
            new("grant_type", "password"),
        new("client_id", "admin-cli"),
        new("username", AdminUser),
        new("password", AdminPassword)
        ]);

        var response = await _client.PostAsync(
            "/realms/master/protocol/openid-connect/token",
            form,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new ServiceResult<string>(
                false,
                StatusCode: (int)response.StatusCode,
                Content: body,
                ContentType: response.Content.Headers.ContentType?.ToString() ?? "application/json");
        }

        using var json = JsonDocument.Parse(body);

        return new ServiceResult<string>(
            true,
            json.RootElement.GetProperty("access_token").GetString()!);
    }

    public async Task<ServiceResult<string>> CreateUserAsync(
    RegisterRequest request,
    string adminAccessToken,
    CancellationToken cancellationToken = default)
    {
        var parts = request.Name.Trim().Split(' ', 2);

        var response = await SendAdminAsync(
            HttpMethod.Post,
            $"/admin/realms/{Realm}/users",
            new
            {
                username = request.Email.Trim().ToLowerInvariant(),
                email = request.Email.Trim().ToLowerInvariant(),
                firstName = parts[0],
                lastName = parts.Length > 1 ? parts[1] : parts[0],
                enabled = true,
                emailVerified = true,
                requiredActions = Array.Empty<string>()
            },
            adminAccessToken,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new ServiceResult<string>(
                false,
                StatusCode: (int)response.StatusCode,
                Content: body,
                ContentType: response.Content.Headers.ContentType?.ToString() ?? "application/json");
        }

        return new ServiceResult<string>(
            true,
            response.Headers.Location!.Segments.Last().TrimEnd('/'));
    }

    public async Task<ServiceResult> SetPasswordAsync(
    string userId,
    string password,
    string adminAccessToken,
    CancellationToken cancellationToken = default)
    {
        var response = await SendAdminAsync(
            HttpMethod.Put,
            $"/admin/realms/{Realm}/users/{userId}/reset-password",
            new
            {
                type = "password",
                value = password,
                temporary = false
            },
            adminAccessToken,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return response.IsSuccessStatusCode
            ? new ServiceResult(true)
            : new ServiceResult(
                false,
                (int)response.StatusCode,
                body,
                response.Content.Headers.ContentType?.ToString() ?? "application/json");
    }

    public async Task<ServiceResult> AssignRoleAsync(
    string userId,
    RegisterType registerType,
    string adminAccessToken,
    CancellationToken cancellationToken = default)
    {
        var roleName = registerType switch
        {
            RegisterType.Customer => "customer",
            RegisterType.Worker => "worker",
            RegisterType.Admin => "admin",
            _ => throw new ArgumentOutOfRangeException(nameof(registerType))
        };

        var roleRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"/admin/realms/{Realm}/roles/{roleName}");

        roleRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", adminAccessToken);

        var roleResponse = await _client.SendAsync(roleRequest, cancellationToken);

        var roleBody = await roleResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!roleResponse.IsSuccessStatusCode)
        {
            return new ServiceResult(
                false,
                (int)roleResponse.StatusCode,
                roleBody,
                roleResponse.Content.Headers.ContentType?.ToString() ?? "application/json");
        }

        var roleJson = await roleResponse.Content.ReadAsStringAsync(cancellationToken);

        var assignRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/admin/realms/{Realm}/users/{userId}/role-mappings/realm");

        assignRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", adminAccessToken);

        assignRequest.Content = new StringContent(
            $"[{roleJson}]",
            Encoding.UTF8,
            "application/json");

        var assignResponse = await _client.SendAsync(assignRequest, cancellationToken);

        var assignBody = await assignResponse.Content.ReadAsStringAsync(cancellationToken);

        return assignResponse.IsSuccessStatusCode
            ? new ServiceResult(true)
            : new ServiceResult(
                false,
                (int)assignResponse.StatusCode,
                assignBody,
                assignResponse.Content.Headers.ContentType?.ToString() ?? "application/json");
    }

    public async Task<ServiceResult> DeleteUserAsync(
    string userId,
    CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);

        if (!token.Success)
        {
            return new ServiceResult(
                false,
                token.StatusCode,
                token.Content,
                token.ContentType);
        }

        var response = await SendAdminAsync(
            HttpMethod.Delete,
            $"/admin/realms/{Realm}/users/{userId}",
            null,
            token.Value!,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return response.IsSuccessStatusCode
            ? new ServiceResult(true)
            : new ServiceResult(
                false,
                (int)response.StatusCode,
                body,
                response.Content.Headers.ContentType?.ToString() ?? "application/json");
    }

    private async Task<HttpResponseMessage> SendAdminAsync(
    HttpMethod method,
    string url,
    object? body,
    string accessToken,
    CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, url);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request, cancellationToken);
    }
}
