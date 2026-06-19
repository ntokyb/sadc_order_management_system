using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SadcOrders.Application.Abstractions;
using SadcOrders.Application.Dtos;
using SadcOrders.Application.Exceptions;
using SadcOrders.Application.Mapping;
using SadcOrders.Domain.Enums;
using SadcOrders.Domain.Orders;

namespace SadcOrders.Application.Orders.Commands;

public record UpdateOrderStatusCommand(Guid OrderId, OrderStatus NewStatus, string IdempotencyKey) : IRequest<OrderDto>;

public class UpdateOrderStatusCommandHandler(
    IApplicationDbContext db,
    IMemoryCache cache) : IRequestHandler<UpdateOrderStatusCommand, OrderDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public async Task<OrderDto> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var cacheKey = $"idempotency:{request.IdempotencyKey}";
        if (cache.TryGetValue(cacheKey, out OrderDto? cached) && cached is not null)
            return cached;

        var order = await db.Orders
            .Include(o => o.LineItems)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order not found.");

        if (!OrderStatusTransitions.IsAllowed(order.Status, request.NewStatus))
            throw new ValidationAppException($"Transition from {order.Status} to {request.NewStatus} is not allowed.");

        order.Status = request.NewStatus;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The order was modified by another request. Refresh and retry.");
        }

        var dto = OrderMappings.ToDto(order, order.Customer.Name);
        cache.Set(cacheKey, dto, CacheTtl);
        return dto;
    }
}
