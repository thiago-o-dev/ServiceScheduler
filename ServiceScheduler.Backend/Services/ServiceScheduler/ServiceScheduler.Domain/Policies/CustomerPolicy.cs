using ServiceScheduler.Domain.Entities;
using SharedKernel.Exceptions;
using System.Net.Mail;

namespace ServiceScheduler.Domain.Policies;

public static class CustomerPolicy
{
    public static void EnsureCanCreateCustomer(Customer customer)
    {
        if (customer.Name.Length < 2)
        {
            throw new DomainValidationException("Nome deve ser maior que duas letras");
        }

        try
        {
            _ = new MailAddress(customer.Email);
        }
        catch
        {
            throw new DomainValidationException("O e-mail informado é inválido.");
        }
    }
}
