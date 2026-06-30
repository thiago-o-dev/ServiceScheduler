namespace ServiceScheduler.Application.Features.ServiceBundles;

public record ServiceBundleDto(Guid Id, string Name, string Description, IReadOnlyList<Guid> ServiceIds, decimal Value, decimal Discount);
