namespace ServiceScheduler.Api.Requests.Worker;

public sealed record UpdateWorkerRequest(string Name, string Phone, string Email, string Cpf);
