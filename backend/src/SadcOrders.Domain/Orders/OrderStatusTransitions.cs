using SadcOrders.Domain.Enums;

namespace SadcOrders.Domain.Orders;

public static class OrderStatusTransitions
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> Allowed = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Paid, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [OrderStatus.Fulfilled, OrderStatus.Cancelled],
        [OrderStatus.Fulfilled] = [],
        [OrderStatus.Cancelled] = []
    };

    public static bool IsAllowed(OrderStatus from, OrderStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);
}
