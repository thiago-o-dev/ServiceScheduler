namespace Api.Gateway.Models;

public sealed record ServiceResult(
    bool Success,
    int? StatusCode = null,
    string? Content = null,
    string ContentType = "application/json");

public sealed record ServiceResult<T>(
    bool Success,
    T? Value = default,
    int? StatusCode = null,
    string? Content = null,
    string ContentType = "application/json");