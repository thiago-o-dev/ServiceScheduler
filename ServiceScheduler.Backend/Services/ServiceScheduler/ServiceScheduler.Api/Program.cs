using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ServiceScheduler.Application.Slices;
using ServiceScheduler.Infrastructure.Configuration;
using ServiceScheduler.Infrastructure.Persistence;
using SharedKernel.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddProblemDetailsHandling();

builder.Services.AddControllers();

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Scheduler API v1";

        return Task.CompletedTask;
    });
});

builder.Services.AddOpenApi("v1-gateway", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Scheduler API v1 (Gateway)";

        document.Servers.Clear();
        document.Servers.Add(new()
        {
            Url = "/servicescheduler-api"
        });

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
}

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{documentName}.json");
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
