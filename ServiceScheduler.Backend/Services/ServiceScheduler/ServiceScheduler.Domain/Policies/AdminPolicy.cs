using ServiceScheduler.Domain.Entities;
using SharedKernel.Exceptions;
using System.Net.Mail;

namespace ServiceScheduler.Domain.Policies;

public static class AdminPolicy
{
    public static void EnsureCanCreateAdmin(Admin admin)
    {
        if (admin.Name.Length < 2)
        {
            throw new DomainValidationException("Nome deve ser maior que duas letras");
        }

        try
        {
            _ = new MailAddress(admin.Email);
        }
        catch
        {
            throw new DomainValidationException("O e-mail informado é inválido.");
        }
    }
}
