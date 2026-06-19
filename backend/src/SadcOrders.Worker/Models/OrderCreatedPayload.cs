namespace SadcOrders.Worker.Models;

public sealed class OrderCreatedPayload
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
