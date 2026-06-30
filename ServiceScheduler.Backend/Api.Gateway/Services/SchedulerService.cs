using Api.Gateway.Models;
using System.Net;
using System.Net.Http.Headers;

namespace Api.Gateway.Services;

public sealed class SchedulerService(
    IHttpClientFactory httpClientFactory,
    ILogger<SchedulerService> logger)
    : ISchedulerService
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("scheduler");

    public async Task<ServiceResult> RegisterAsync(
    RegisterRequest request,
    string keycloakToken,
    CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = request.RegisterType switch
        {
            RegisterType.Customer =>
                await PostAsJsonWithAuthAsync(
                    "/api/Customers",
                    new
                    {
                        request.Name,
                        request.Phone,
                        request.Email
                    },
                    keycloakToken,
                    cancellationToken),

            RegisterType.Worker =>
                await PostAsJsonWithAuthAsync(
                    "/api/Workers",
                    new
                    {
                        request.Name,
                        request.Phone,
                        request.Email,
                        Cpf = request.Cpf
                    },
                    keycloakToken,
                    cancellationToken),

            _ => throw new NotSupportedException()
        };

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return response.IsSuccessStatusCode
            ? new ServiceResult(true)
            : new ServiceResult(
                false,
                (int)response.StatusCode,
                body,
                response.Content.Headers.ContentType?.ToString() ?? "application/json");
    }

    private Task<HttpResponseMessage> PostAsJsonWithAuthAsync<T>(
    string url,
    T data,
    string token,
    CancellationToken ct)
    {
        Console.WriteLine(token);
        var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(data);

        Console.WriteLine(req.Headers.Authorization?.ToString());

        return _client.SendAsync(req, ct);
    }
}