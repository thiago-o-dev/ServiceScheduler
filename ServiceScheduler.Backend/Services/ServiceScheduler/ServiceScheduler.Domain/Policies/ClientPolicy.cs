using ServiceScheduler.Domain.Entities;
using SharedKernel.Exceptions;
using System.Net.Mail;

namespace ServiceScheduler.Domain.Policies;

public static class ClientPolicy
{
    public static void EnsureCanCreateClient(Client client)
    {
        if (client.Name.Length < 2)
        {
            throw new DomainValidationException("Nome deve ser maior que duas letras");
        }

        try
        {
            _ = new MailAddress(client.Email);
        }
        catch
        {
            throw new DomainValidationException("O e-mail informado é inválido.");
        }
    }
}
