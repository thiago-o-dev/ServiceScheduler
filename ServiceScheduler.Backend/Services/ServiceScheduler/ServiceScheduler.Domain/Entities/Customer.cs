using ServiceScheduler.Domain.Policies;
using SharedKernel.Abstractions;
using SharedKernel.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceScheduler.Domain.Entities;

public class Customer : LifeCycleEntity
{
    // não vou guardar cpf pq ninguem gosta de dar cpf nessas coisas
    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    private Customer() { }

    public static Customer Create(string name, string phone, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainValidationException("Celular é obrigatório.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainValidationException("Email é obrigatório.");

        var customer = new Customer
        {
            Name = name.Trim(),
            Phone = phone.Trim(),
            Email = email.Trim(),
        };

        CustomerPolicy.EnsureCanCreateCustomer(customer);

        // TODO: estourar evento pro mailhog pra mostrar q foi criado um novo usuário

        return customer;
    }

    public void Update(string name, string phone)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainValidationException("Celular é obrigatório.");

        Name = name.Trim();
        Phone = phone.Trim();
        Touch();

        // Nunca alterar email depois de criado pois ele relaciona ao keycloak
    }
}
