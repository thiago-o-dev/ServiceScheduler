using Api.Gateway.Models;
using System.Net;

namespace Api.Gateway.Services;

public sealed class SchedulerService(
    IHttpClientFactory httpClientFactory,
    ILogger<SchedulerService> logger)
    : ISchedulerService
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("scheduler");

    public async Task<ServiceResult> RegisterAsync(
    RegisterRequest request,
    CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = request.RegisterType switch
        {
            RegisterType.Customer =>
                await _client.PostAsJsonAsync(
                    "/api/Customers",
                    new
                    {
                        request.Name,
                        request.Phone,
                        request.Email
                    },
                    cancellationToken),

            RegisterType.Worker =>
                await _client.PostAsJsonAsync(
                    "/api/Workers",
                    new
                    {
                        request.Name,
                        request.Phone,
                        request.Email,
                        Cpf = request.Document
                    },
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
}