using ServiceScheduler.Domain.Policies;
using SharedKernel.Abstractions;
using SharedKernel.Exceptions;

namespace ServiceScheduler.Domain.Entities;

public class Admin : LifeCycleEntity
{
    // n vo botar mais nada, vou deixar validação na camada de aplicação
    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    private Admin() { }

    public static Admin Create(string name, string phone, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainValidationException("Celular é obrigatório.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainValidationException("Email é obrigatório.");

        var admin = new Admin
        {
            Name = name.Trim(),
            Phone = phone.Trim(),
            Email = email.Trim(),
        };

        AdminPolicy.EnsureCanCreateAdmin(admin);

        // TODO: estourar evento pro mailhog pra mostrar q foi criado um novo usuário

        return admin;
    }
}
