using SadcOrders.Domain.Enums;

namespace SadcOrders.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Customer Customer { get; set; } = null!;
    public ICollection<OrderLineItem> LineItems { get; set; } = new List<OrderLineItem>();
}
