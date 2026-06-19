using MediatR;
using Microsoft.EntityFrameworkCore;
using SadcOrders.Application.Abstractions;
using SadcOrders.Application.Common;
using SadcOrders.Application.Dtos;
using SadcOrders.Application.Mapping;

namespace SadcOrders.Application.Customers.Queries;

public record GetCustomersQuery(string? Search, int Page, int PageSize, bool? HasOrders) : IRequest<PagedResult<CustomerDto>>;

public class GetCustomersQueryHandler(IApplicationDbContext db) : IRequestHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    public async Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize <= 0 ? PaginationDefaults.DefaultPageSize : request.PageSize, 1, PaginationDefaults.MaxPageSize);

        var query = db.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(c => c.Name.Contains(term) || c.Email.Contains(term));
        }

        if (request.HasOrders == true)
            query = query.Where(c => c.Orders.Any());

        var total = await query.CountAsync(cancellationToken);
        var customers = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = customers.Select(OrderMappings.ToDto).ToList();

        return new PagedResult<CustomerDto>(items, total, page, pageSize);
    }
}
