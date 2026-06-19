using MediatR;
using Microsoft.EntityFrameworkCore;
using SadcOrders.Application.Abstractions;
using SadcOrders.Application.Exceptions;

namespace SadcOrders.Application.Customers.Commands;

public record DeleteCustomerCommand(Guid Id) : IRequest;

public class DeleteCustomerCommandHandler(IApplicationDbContext db) : IRequestHandler<DeleteCustomerCommand>
{
    public async Task Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Customer not found.");

        if (customer.Orders.Count > 0)
            throw new ConflictException("Cannot delete a customer that has orders.");

        db.Customers.Remove(customer);
        await db.SaveChangesAsync(cancellationToken);
    }
}
