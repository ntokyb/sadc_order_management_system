using MediatR;
using Microsoft.EntityFrameworkCore;
using SadcOrders.Application.Abstractions;
using SadcOrders.Application.Dtos;
using SadcOrders.Application.Mapping;

namespace SadcOrders.Application.Customers.Queries;

public record GetCustomerQuery(Guid Id) : IRequest<CustomerDto?>;

public class GetCustomerQueryHandler(IApplicationDbContext db) : IRequestHandler<GetCustomerQuery, CustomerDto?>
{
    public async Task<CustomerDto?> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        return customer is null ? null : OrderMappings.ToDto(customer);
    }
}
