using System.Text.Json;
using BuildingBlocks.Messaging.Abstractions;
using BuildingBlocks.Persistence.EntityFramework;
using BuildingBlocks.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Persistence;

public sealed class OutboxWorker<TDbContext>(
    IServiceProvider serviceProvider,
    ILogger<OutboxWorker<TDbContext>> logger) : BackgroundService
    where TDbContext : AppDbContext
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    private static readonly IReadOnlyDictionary<string, Type> _typeCache =
        AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => !string.IsNullOrEmpty(t.Name))
            .GroupBy(t => t.Name)
            .ToDictionary(g => g.Key, g => g.First());

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox processing cycle failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken ct)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        var messages = await db.Set<OutboxMessage>()
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(20)
            .ToListAsync(ct);

        if (messages.Count == 0) return;

        foreach (var message in messages)
        {
            try
            {
                _typeCache.TryGetValue(message.Type, out var eventType);

                if (eventType is null)
                {
                    message.Error = $"Unknown event type: {message.Type}";
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    logger.LogWarning("No type found for outbox message type '{Type}'", message.Type);
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(message.Content, eventType);
                if (domainEvent is null)
                {
                    message.Error = "Failed to deserialize event payload.";
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    continue;
                }

                // Publish to RabbitMQ via the generic PublishAsync<T>.
                // We use reflection because the event type is only known at runtime.
                var publishMethod = typeof(IMessagePublisher)
                    .GetMethod(nameof(IMessagePublisher.PublishAsync))!
                    .MakeGenericMethod(eventType);

                await (Task)publishMethod.Invoke(publisher, [domainEvent, ct])!;

                message.ProcessedOnUtc = DateTime.UtcNow;
                logger.LogDebug("Published outbox message {Id} ({Type}) to message bus", message.Id, message.Type);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process outbox message {Id}", message.Id);
                message.Error = ex.Message;
                message.ProcessedOnUtc = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
