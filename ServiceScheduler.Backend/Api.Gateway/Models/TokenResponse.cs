namespace Api.Gateway.Models;

public sealed record TokenResponse(string Token, int ExpiresIn);