using MediatR;
using Microsoft.EntityFrameworkCore;
using SadcOrders.Application.Abstractions;
using SadcOrders.Application.Dtos;
using SadcOrders.Application.Mapping;

namespace SadcOrders.Application.Orders.Queries;

public record GetOrderQuery(Guid Id) : IRequest<OrderDto?>;

public class GetOrderQueryHandler(IApplicationDbContext db) : IRequestHandler<GetOrderQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.LineItems)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        return order is null ? null : OrderMappings.ToDto(order, order.Customer.Name);
    }
}
