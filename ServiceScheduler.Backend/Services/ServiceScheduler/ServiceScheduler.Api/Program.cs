using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ServiceScheduler.Application.Slices;
using ServiceScheduler.Infrastructure.Configuration;
using ServiceScheduler.Infrastructure.Persistence;
using SharedKernel.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

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
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
