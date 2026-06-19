namespace SadcOrders.Domain.Messaging;

public record OrderCreatedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    string CurrencyCode,
    DateTimeOffset CreatedAt);
