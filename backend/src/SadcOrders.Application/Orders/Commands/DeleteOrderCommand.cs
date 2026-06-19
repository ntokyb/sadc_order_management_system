using MediatR;
using Microsoft.EntityFrameworkCore;
using SadcOrders.Application.Abstractions;
using SadcOrders.Application.Exceptions;
using SadcOrders.Domain.Enums;

namespace SadcOrders.Application.Orders.Commands;

public record DeleteOrderCommand(Guid Id) : IRequest;

public class DeleteOrderCommandHandler(IApplicationDbContext db) : IRequestHandler<DeleteOrderCommand>
{
    public async Task Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(o => o.LineItems)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Order not found.");

        if (order.Status != OrderStatus.Pending)
            throw new ValidationAppException("Only pending orders can be deleted.");

        db.OrderLineItems.RemoveRange(order.LineItems);
        db.Orders.Remove(order);
        await db.SaveChangesAsync(cancellationToken);
    }
}
