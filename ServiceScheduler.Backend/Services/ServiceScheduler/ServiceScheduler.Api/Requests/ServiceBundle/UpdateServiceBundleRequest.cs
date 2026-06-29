namespace ServiceScheduler.Api.Requests.ServiceBundle;

public record UpdateServiceBundleRequest(string Name, string Description, Guid[] ServiceIds, decimal Price);