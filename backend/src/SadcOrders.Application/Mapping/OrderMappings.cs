using SadcOrders.Application.Dtos;
using SadcOrders.Domain.Entities;
using SadcOrders.Domain.Enums;

namespace SadcOrders.Application.Mapping;

public static class OrderMappings
{
    public static OrderDto ToDto(Order order, string customerName) =>
        new(
            order.Id,
            order.CustomerId,
            customerName,
            order.Status,
            order.CurrencyCode,
            order.TotalAmount,
            order.CreatedAt,
            order.LineItems
                .Select(li => new OrderLineItemDto(li.Id, li.ProductSku, li.Quantity, li.UnitPrice, li.Quantity * li.UnitPrice))
                .ToList());

    public static CustomerDto ToDto(Customer customer) =>
        new(customer.Id, customer.Name, customer.Email, customer.CountryCode, customer.CreatedAt);
}
