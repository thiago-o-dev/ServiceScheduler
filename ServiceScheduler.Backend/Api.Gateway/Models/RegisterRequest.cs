namespace Api.Gateway.Models;

public sealed record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string Phone,
    string Document,
    //CustomerUniqueData CustomerUniqueData,
    //WorkerUniqueData WorkerUniqueData,
    RegisterType RegisterType);

//public sealed record CustomerUniqueData();

//public sealed record WorkerUniqueData(string Cpf);

public enum RegisterType
{
    Customer,
    Worker,
    Admin
}