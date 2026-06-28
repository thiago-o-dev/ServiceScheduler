namespace ServiceScheduler.Application.Features.Customers;

public record CustomerDto(Guid Id, string Name, string Phone, string Email);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
