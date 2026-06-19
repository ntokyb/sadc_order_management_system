namespace SadcOrders.API.Models;

/// <summary>
/// Request body for creating a customer.
/// </summary>
public record CreateCustomerRequest(string Name, string Email, string CountryCode);

/// <summary>
/// Request body for updating a customer.
/// </summary>
public record UpdateCustomerRequest(string Name, string Email, string CountryCode);

/// <summary>
/// Line item payload when creating an order.
/// </summary>
public record CreateOrderLineItemRequest(string ProductSku, int Quantity, decimal UnitPrice);

/// <summary>
/// Request body for creating an order.
/// </summary>
public record CreateOrderRequest(
    Guid CustomerId,
    string CurrencyCode,
    IReadOnlyList<CreateOrderLineItemRequest> LineItems);

/// <summary>
/// Request body for updating order status. Allowed values: Paid, Fulfilled, Cancelled.
/// </summary>
public record UpdateOrderStatusRequest(string Status);
