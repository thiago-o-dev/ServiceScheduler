using ServiceScheduler.Application.Abstractions;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceScheduler.Application.Features.Customers;

public sealed record GetCustomerByIdQuery(Guid Id) : IQueryRequest<CustomerDto>;

public sealed class GetCustomerByIdQueryHandler(ICustomerRepository customerRepository) 
    : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    public async Task<CustomerDto> HandleAsync(GetCustomerByIdQuery query, CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException($"Cliente com ID '{query.Id}' não encontrado.");

        return new CustomerDto(customer.Id, customer.Name, customer.Phone, customer.Email);
    }
}

public sealed record ListCustomersQuery(string? SearchTerm, int Page = 1, int PageSize = 10) 
    : IQueryRequest<PagedResult<CustomerDto>>;

public sealed class ListCustomersQueryHandler(ICustomerRepository customerRepository) 
    : IRequestHandler<ListCustomersQuery, PagedResult<CustomerDto>>
{
    public async Task<PagedResult<CustomerDto>> HandleAsync(ListCustomersQuery query, CancellationToken cancellationToken = default)
    {
        var customers = await customerRepository.GetAllAsync(cancellationToken);

        var queryable = customers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim();
            queryable = queryable.Where(c => 
                c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                c.Phone.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                c.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var totalCount = queryable.Count();
        var items = queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CustomerDto(c.Id, c.Name, c.Phone, c.Email))
            .ToList();

        return new PagedResult<CustomerDto>(items, query.Page, query.PageSize, totalCount);
    }
}
