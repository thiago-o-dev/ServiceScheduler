namespace ServiceScheduler.Api.Requests.Service;

public record UpdateServiceRequest(string Name, string Description, decimal Value);