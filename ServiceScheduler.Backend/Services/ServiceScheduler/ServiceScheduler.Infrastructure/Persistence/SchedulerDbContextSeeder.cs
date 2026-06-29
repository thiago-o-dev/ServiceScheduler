using Microsoft.EntityFrameworkCore;
using ServiceScheduler.Domain.Entities;
using ServiceScheduler.Domain.ValueObjects;

namespace ServiceScheduler.Infrastructure.Persistence;

public static class SchedulerDbContextSeeder
{
    public static async Task SeedAsync(SchedulerDbContext context)
    {
        if (!await context.Services.AnyAsync())
        {
            var services = new List<Service>
            {
                Service.Create("Corte feminino", "Corte com lavagem e finalização.", 90m),
                Service.Create("Escova", "Escova modeladora.", 70m),
                Service.Create("Coloração", "Coloração completa com produtos premium.", 220m),
                Service.Create("Hidratação profunda", "Tratamento reparador.", 110m),
                Service.Create("Manicure", "Manicure clássica.", 45m),
                Service.Create("Pedicure", "Pedicure spa.", 55m)
            };

            await context.Services.AddRangeAsync(services);
            await context.SaveChangesAsync();

            var corteFeminino = services.First(s => s.Name == "Corte feminino");
            var escova = services.First(s => s.Name == "Escova");
            var hidratacao = services.First(s => s.Name == "Hidratação profunda");
            var manicure = services.First(s => s.Name == "Manicure");
            var pedicure = services.First(s => s.Name == "Pedicure");

            var bundles = new List<ServiceBundle>
            {
                ServiceBundle.Create(
                    "Dia da noiva",
                    "Pacote completo para o grande dia.",
                    new List<Guid> { corteFeminino.Id, escova.Id, hidratacao.Id },
                    229.50m
                ),
                ServiceBundle.Create(
                    "Mãos & pés",
                    "Manicure + pedicure com desconto.",
                    new List<Guid> { manicure.Id, pedicure.Id },
                    90m
                )
            };

            await context.ServiceBundles.AddRangeAsync(bundles);
            await context.SaveChangesAsync();
        }

        if (!await context.Workers.AnyAsync())
        {
            var days = new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
            
            var leila = Worker.Create("Leila Martins", "11999990001", "leila@cabeleleiraleila.com", "11111111111");
            foreach (var day in days)
            {
                leila.AddAvailablePeriod(new AvailablePeriod(day, new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0)));
            }

            var bruna = Worker.Create("Bruna Costa", "11999990002", "bruna@cabeleleiraleila.com", "22222222222");
            foreach (var day in days)
            {
                bruna.AddAvailablePeriod(new AvailablePeriod(day, new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0)));
                bruna.AddAvailablePeriod(new AvailablePeriod(day, new TimeSpan(13, 0, 0), new TimeSpan(18, 0, 0)));
            }

            var camila = Worker.Create("Camila Souza", "11999990003", "camila@cabeleleiraleila.com", "33333333333");
            foreach (var day in days)
            {
                camila.AddAvailablePeriod(new AvailablePeriod(day, new TimeSpan(9, 0, 0), new TimeSpan(12, 0, 0)));
            }

            await context.Workers.AddRangeAsync(leila, bruna, camila);
            await context.SaveChangesAsync();
        }

        if (!await context.Customers.AnyAsync())
        {
            var demoCustomer = Customer.Create("Cliente Demonstração", "11900000000", "cliente@demo.com");
            await context.Customers.AddAsync(demoCustomer);
            await context.SaveChangesAsync();
        }
    }
}
