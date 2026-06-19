using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SadcOrders.Application.Abstractions;
using SadcOrders.Application.Dtos;
using SadcOrders.Application.Exceptions;
using SadcOrders.Application.Mapping;
using SadcOrders.Domain.Entities;
using SadcOrders.Domain.Enums;
using SadcOrders.Domain.ValueObjects;

namespace SadcOrders.Application.Orders.Commands;

public record CreateOrderLineItemInput(string ProductSku, int Quantity, decimal UnitPrice);

public record CreateOrderCommand(
    Guid CustomerId,
    string CurrencyCode,
    IReadOnlyList<CreateOrderLineItemInput> LineItems) : IRequest<OrderDto>;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.LineItems).NotEmpty();
        RuleForEach(x => x.LineItems).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductSku).NotEmpty().MaximumLength(100);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

public class CreateOrderCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new ValidationAppException("Customer not found.");

        var pairing = SadcCountryCurrency.Create(customer.CountryCode, request.CurrencyCode);
        if (!pairing.IsValid())
            throw new ValidationAppException(pairing.ErrorMessage ?? "Invalid country/currency pairing.");

        var now = DateTimeOffset.UtcNow;
        var orderId = Guid.NewGuid();
        var lineItems = request.LineItems.Select(l => new OrderLineItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductSku = l.ProductSku.Trim(),
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice
        }).ToList();

        var total = lineItems.Sum(l => l.Quantity * l.UnitPrice);

        var order = new Order
        {
            Id = orderId,
            CustomerId = customer.Id,
            Status = OrderStatus.Pending,
            CurrencyCode = pairing.CurrencyCode,
            TotalAmount = total,
            CreatedAt = now,
            UpdatedAt = now,
            LineItems = lineItems
        };

        var outboxPayload = JsonSerializer.Serialize(new
        {
            OrderId = order.Id,
            CustomerId = customer.Id,
            CurrencyCode = pairing.CurrencyCode,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt
        });

        var outbox = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "OrderCreated",
            Payload = outboxPayload,
            CreatedAt = now
        };

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Orders.Add(order);
        db.OutboxMessages.Add(outbox);
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return OrderMappings.ToDto(order, customer.Name);
    }
}
