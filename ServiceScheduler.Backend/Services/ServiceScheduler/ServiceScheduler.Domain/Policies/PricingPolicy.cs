using ServiceScheduler.Domain.Entities;
using SharedKernel.Exceptions;

namespace ServiceScheduler.Domain.Policies;

public static class PricingPolicy
{
    public static decimal CalculateTotalValue(
        ICollection<Service> services,
        ICollection<ServiceBundle> activeBundles)
    {
        if (services == null || !services.Any())
            return 0;

        if (services.Any(s => s == null))
            throw new DomainValidationException("Lista de serviços contém elementos nulos.");

        // Obter os IDs de todos os serviços selecionados
        var serviceIds = services.Select(s => s.Id).ToList();
        decimal totalValue = 0;

        // Ordenar os pacotes pelo maior número de serviços inclusos para priorizar pacotes mais complexos/vantajosos
        var sortedBundles = (activeBundles ?? new List<ServiceBundle>())
            .OrderByDescending(b => b.ServiceIds.Count)
            .ToList();

        // Conjunto para rastrear quais serviços já foram agrupados em um pacote
        var matchedServiceIds = new HashSet<Guid>();

        foreach (var bundle in sortedBundles)
        {
            var bundleServiceIds = bundle.ServiceIds;

            // Enquanto o pacote puder ser aplicado com os serviços que restam não-consumidos
            while (true)
            {
                // Verifica se todos os serviços exigidos por este pacote estão presentes e não foram consumidos ainda
                var canApply = bundleServiceIds.All(id =>
                    serviceIds.Contains(id) &&
                    !matchedServiceIds.Contains(id));

                if (!canApply)
                    break;

                // Aplica o preço com desconto do pacote
                totalValue += bundle.Value;

                // Consome estes serviços marcando-os como agrupados
                foreach (var id in bundleServiceIds)
                {
                    matchedServiceIds.Add(id);
                }
            }
        }

        // Soma o valor padrão individual de todos os serviços que não foram cobertos por nenhum pacote
        foreach (var service in services)
        {
            if (!matchedServiceIds.Contains(service.Id))
            {
                totalValue += service.Value;
            }
        }

        return totalValue;
    }
}
