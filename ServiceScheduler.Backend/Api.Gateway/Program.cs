using Api.Gateway.Services;
using Scalar.AspNetCore;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddDefaultAuthentication();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()));

builder.Services.AddControllers();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(transformContext =>
        {
            var authHeader =
                transformContext.HttpContext.Request.Headers.Authorization.ToString();

            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                transformContext.ProxyRequest.Headers.Authorization =
                    System.Net.Http.Headers.AuthenticationHeaderValue.Parse(authHeader);
            }

            return ValueTask.CompletedTask;
        });
    })
    .AddServiceDiscoveryDestinationResolver();

builder.Services.AddHttpClient("keycloak", client =>
{
    client.BaseAddress = new Uri("https://localhost:8081");
});

builder.Services.AddHttpClient("scheduler", client =>
{
    client.BaseAddress = new Uri("https+http://servicescheduler-api");
});

builder.Services.AddOpenApi();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IKeycloakService, KeycloakService>();
builder.Services.AddScoped<ISchedulerService, SchedulerService>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "ServiceScheduler Gateway";

        options
            .AddPreferredSecuritySchemes("Bearer")
            .AddHttpAuthentication("Bearer", auth =>
            {
                auth.Token = "";
            });

        options.AddDocument("v1", "Gateway API");

        options.AddDocument(
            "Core API",
            routePattern: "/servicescheduler-api/openapi/v1-gateway.json");
    });
}

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.MapReverseProxy();

app.MapControllers();

app.Run();
