using Api.Gateway.Models;
using Api.Gateway.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Api.Gateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    IAuthService authService)
    : ControllerBase
{
    [HttpPost("token")]
    public async Task<IActionResult> Token(
        TokenRequest request,
        CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);

        return result.Value != null ? Ok(result.Value) : new ContentResult
        {
            StatusCode = result.StatusCode,
            Content = result.Content,
            ContentType = result.ContentType
        };
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);

        return result.Value != null ? Ok(result.Value) : new ContentResult
        {
            StatusCode = result.StatusCode,
            Content = result.Content,
            ContentType = result.ContentType
        };
    }
}