using System.Collections.Concurrent;
using EasyNetQ;

namespace BuildingBlocks.Messaging.EasyNetQ;

// Routes messages by short class name only, ignoring namespace and assembly.
// This allows Scheduling.Domain.Events.ConsultationScheduledEvent and
// Notifications.Contracts.Events.ConsultationScheduledEvent to share the same queue.
public sealed class SimpleTypeNameSerializer : ITypeNameSerializer
{
    private static readonly ConcurrentDictionary<string, Type> _cache = new();

    public string Serialize(Type type) => type.Name;

    public Type Deserialize(string typeName)
        => _cache.GetOrAdd(typeName, name =>
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == name)
            ?? throw new InvalidOperationException(
                $"Cannot resolve message type '{name}'. " +
                $"Ensure the assembly containing this type is referenced."));
}
