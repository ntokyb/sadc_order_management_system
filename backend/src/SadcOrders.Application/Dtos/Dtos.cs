using SadcOrders.Domain.Enums;

namespace SadcOrders.Application.Dtos;

public record CustomerDto(
    Guid Id,
    string Name,
    string Email,
    string CountryCode,
    DateTimeOffset CreatedAt);

public record OrderLineItemDto(
    Guid Id,
    string ProductSku,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record OrderDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    OrderStatus Status,
    string CurrencyCode,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderLineItemDto> LineItems);
