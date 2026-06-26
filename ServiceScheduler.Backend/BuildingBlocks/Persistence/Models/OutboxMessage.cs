namespace BuildingBlocks.Persistence.Models;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public string Type { get; set; } = default!;

    public string Content { get; set; } = default!;

    public DateTime OccurredOnUtc { get; set; }

    public DateTime? ProcessedOnUtc { get; set; }

    public string? Error { get; set; }
}
