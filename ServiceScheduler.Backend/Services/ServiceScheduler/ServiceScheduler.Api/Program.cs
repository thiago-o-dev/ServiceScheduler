using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using ServiceScheduler.Application.Slices;
using ServiceScheduler.Infrastructure.Configuration;
using ServiceScheduler.Infrastructure.Persistence;
using SharedKernel.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddKeycloakAuthentication();
builder.AddProblemDetailsHandling();

builder.Services.AddControllers();

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info.Title = "Scheduler API v1";
        ApplyBearerSecurity(document);
        return Task.CompletedTask;
    });
});

builder.Services.AddOpenApi("v1-gateway", options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info.Title = "Scheduler API v1 (Gateway)";
        document.Servers.Clear();
        document.Servers.Add(new() { Url = "/servicescheduler-api" });
        ApplyBearerSecurity(document);
        return Task.CompletedTask;
    });
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.AddCQRSPipeline(typeof(FeaturesAssemblyReference).Assembly);
builder.Services.AddRequestDispatcher();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
    await db.Database.MigrateAsync();
    await SchedulerDbContextSeeder.SeedAsync(db);
}

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{documentName}.json");
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static void ApplyBearerSecurity(OpenApiDocument document)
{
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(); // IOpenApiSecurityScheme, not OpenApiSecurityScheme

    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT bearer token"
    };

    document.Security ??= new List<OpenApiSecurityRequirement>();

    var requirement = new OpenApiSecurityRequirement();
    requirement[new OpenApiSecuritySchemeReference("Bearer")] = new List<string>();
    document.Security.Add(requirement);
}