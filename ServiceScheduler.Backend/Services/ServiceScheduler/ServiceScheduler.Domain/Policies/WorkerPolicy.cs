using ServiceScheduler.Domain.Exceptions;
using ServiceScheduler.Domain.ValueObjects;
using SharedKernel.Exceptions;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace ServiceScheduler.Domain.Policies;

public static class WorkerPolicy
{
    public static void EnsureNameIsValid(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("O nome do trabalhador deve ser informado.");

        if (name.Length > 150)
            throw new DomainValidationException("O nome do trabalhador deve possuir no máximo 150 caracteres.");
    }

    public static void EnsurePhoneIsValid(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainValidationException("O telefone do trabalhador deve ser informado.");
    }

    public static void EnsureEmailIsValid(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainValidationException("O e-mail do trabalhador deve ser informado.");

        try
        {
            _ = new MailAddress(email);
        }
        catch
        {
            throw new DomainValidationException("O e-mail informado é inválido.");
        }
    }

    public static void EnsureCpfIsValid(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            throw new DomainValidationException("O CPF do trabalhador deve ser informado.");

        if (cpf.Length != 11 || !cpf.All(char.IsDigit))
            throw new DomainValidationException("O CPF informado é inválido.");
    }

    public static void EnsureAvailablePeriodsDoNotOverlap(
        IEnumerable<AvailablePeriod> periods)
    {
        var grouped = periods.GroupBy(x => x.DayOfWeek);

        foreach (var day in grouped)
        {
            var ordered = day
                .OrderBy(x => x.StartTime)
                .ToList();

            for (int i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].StartTime < ordered[i - 1].EndTime)
                    throw new DomainOverlaidException($"Existem períodos de disponibilidade sobrepostos para {day.Key}.");
            }
        }
    }

    public static void EnsureUnavailablePeriodsDoNotOverlap(
        IEnumerable<UnavailablePeriod> periods)
    {
        var ordered = periods
            .OrderBy(x => x.Start)
            .ToList();

        for (int i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].Start < ordered[i - 1].End)
                throw new DomainOverlaidException("Existem períodos de indisponibilidade sobrepostos.");
        }
    }

    public static void EnsureAvailablePeriodCanBeAdded(
        AvailablePeriod period,
        IEnumerable<AvailablePeriod> existingPeriods)
    {
        if (existingPeriods.Any(x =>
            x.DayOfWeek == period.DayOfWeek &&
            period.StartTime < x.EndTime &&
            period.EndTime > x.StartTime))
        {
            throw new DomainOverlaidException("O período informado sobrepõe outro período de disponibilidade.");
        }
    }

    public static void EnsureUnavailablePeriodCanBeAdded(
        UnavailablePeriod period,
        IEnumerable<UnavailablePeriod> existingPeriods)
    {
        if (existingPeriods.Any(x =>
            period.Start < x.End &&
            period.End > x.Start))
        {
            throw new DomainOverlaidException("O período informado sobrepõe outro período de indisponibilidade.");
        }
    }
}
