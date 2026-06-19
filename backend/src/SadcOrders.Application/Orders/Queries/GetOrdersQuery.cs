using MediatR;
using Microsoft.EntityFrameworkCore;
using SadcOrders.Application.Abstractions;
using SadcOrders.Application.Common;
using SadcOrders.Application.Dtos;
using SadcOrders.Application.Mapping;
using SadcOrders.Domain.Enums;

namespace SadcOrders.Application.Orders.Queries;

public record GetOrdersQuery(
    Guid? CustomerId,
    OrderStatus? Status,
    int Page,
    int PageSize,
    string? Sort) : IRequest<PagedResult<OrderDto>>;

public class GetOrdersQueryHandler(IApplicationDbContext db) : IRequestHandler<GetOrdersQuery, PagedResult<OrderDto>>
{
    public async Task<PagedResult<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize <= 0 ? PaginationDefaults.DefaultPageSize : request.PageSize, 1, PaginationDefaults.MaxPageSize);

        var query = db.Orders.AsNoTracking()
            .Include(o => o.LineItems)
            .Include(o => o.Customer)
            .AsQueryable();

        if (request.CustomerId.HasValue)
            query = query.Where(o => o.CustomerId == request.CustomerId.Value);

        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);

        query = (request.Sort?.Trim().ToLowerInvariant()) switch
        {
            "createdat_asc" => query.OrderBy(o => o.CreatedAt),
            "totalamount_desc" => query.OrderByDescending(o => o.TotalAmount),
            _ => query.OrderByDescending(o => o.CreatedAt) // createdAt_desc default
        };

        var total = await query.CountAsync(cancellationToken);
        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = orders.Select(o => OrderMappings.ToDto(o, o.Customer.Name)).ToList();
        return new PagedResult<OrderDto>(items, total, page, pageSize);
    }
}
