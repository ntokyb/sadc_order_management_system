using SadcOrders.Domain.Enums;
using SadcOrders.Domain.Orders;
using SadcOrders.Domain.ValueObjects;

using FluentValidation;
using SadcOrders.Application.Orders.Commands;

namespace SadcOrders.UnitTests;

public class SadcCountryCurrencyTests
{
    [Fact]
    public void Valid_ZA_ZAR_returns_true()
    {
        var pairing = SadcCountryCurrency.Create("ZA", "ZAR");
        Assert.True(pairing.IsValid());
    }

    [Fact]
    public void Valid_BW_BWP_returns_true()
    {
        var pairing = SadcCountryCurrency.Create("BW", "BWP");
        Assert.True(pairing.IsValid());
    }

    [Fact]
    public void Valid_ZW_USD_returns_true()
    {
        var pairing = SadcCountryCurrency.Create("ZW", "USD");
        Assert.True(pairing.IsValid());
    }

    [Fact]
    public void Valid_NA_ZAR_returns_true()
    {
        var pairing = SadcCountryCurrency.Create("NA", "ZAR");
        Assert.True(pairing.IsValid());
    }

    [Fact]
    public void Invalid_ZA_BWP_returns_false()
    {
        var pairing = SadcCountryCurrency.Create("ZA", "BWP");
        Assert.False(pairing.IsValid());
    }

    [Fact]
    public void Invalid_unknown_country_returns_false()
    {
        var pairing = SadcCountryCurrency.Create("US", "USD");
        Assert.False(pairing.IsValid());
    }

    [Fact]
    public void ErrorMessage_contains_country_and_currency_when_invalid()
    {
        var pairing = SadcCountryCurrency.Create("ZA", "BWP");
        Assert.False(pairing.IsValid());
        Assert.NotNull(pairing.ErrorMessage);
        Assert.Contains("ZA", pairing.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BWP", pairing.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}

public class OrderStatusTransitionsTests
{
    [Fact]
    public void Pending_can_transition_to_Paid()
    {
        Assert.True(OrderStatusTransitions.IsAllowed(OrderStatus.Pending, OrderStatus.Paid));
    }

    [Fact]
    public void Pending_can_transition_to_Cancelled()
    {
        Assert.True(OrderStatusTransitions.IsAllowed(OrderStatus.Pending, OrderStatus.Cancelled));
    }

    [Fact]
    public void Pending_cannot_transition_to_Fulfilled()
    {
        Assert.False(OrderStatusTransitions.IsAllowed(OrderStatus.Pending, OrderStatus.Fulfilled));
    }

    [Fact]
    public void Paid_can_transition_to_Fulfilled()
    {
        Assert.True(OrderStatusTransitions.IsAllowed(OrderStatus.Paid, OrderStatus.Fulfilled));
    }

    [Fact]
    public void Paid_can_transition_to_Cancelled()
    {
        Assert.True(OrderStatusTransitions.IsAllowed(OrderStatus.Paid, OrderStatus.Cancelled));
    }

    [Fact]
    public void Fulfilled_cannot_transition_to_anything()
    {
        Assert.False(OrderStatusTransitions.IsAllowed(OrderStatus.Fulfilled, OrderStatus.Pending));
        Assert.False(OrderStatusTransitions.IsAllowed(OrderStatus.Fulfilled, OrderStatus.Paid));
        Assert.False(OrderStatusTransitions.IsAllowed(OrderStatus.Fulfilled, OrderStatus.Cancelled));
    }

    [Fact]
    public void Cancelled_cannot_transition_to_anything()
    {
        Assert.False(OrderStatusTransitions.IsAllowed(OrderStatus.Cancelled, OrderStatus.Pending));
        Assert.False(OrderStatusTransitions.IsAllowed(OrderStatus.Cancelled, OrderStatus.Paid));
        Assert.False(OrderStatusTransitions.IsAllowed(OrderStatus.Cancelled, OrderStatus.Fulfilled));
    }
}

public class TotalAmountCalculationTests
{
    [Fact]
    public void Single_line_item_calculates_correctly()
    {
        var total = CalculateTotal([(2, 50m)]);
        Assert.Equal(100m, total);
    }

    [Fact]
    public void Multiple_line_items_sum_correctly()
    {
        var total = CalculateTotal([(2, 50m), (1, 100m), (5, 20m)]);
        Assert.Equal(300m, total);
    }

    [Fact]
    public void Zero_quantity_not_allowed_throws()
    {
        var validator = new CreateOrderCommandValidator();
        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            "ZAR",
            [new CreateOrderLineItemInput("SKU-1", 0, 50m)]);

        var exception = Assert.Throws<FluentValidation.ValidationException>(() => validator.ValidateAndThrow(command));
        Assert.Contains("Quantity", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Negative_unit_price_not_allowed_throws()
    {
        var validator = new CreateOrderCommandValidator();
        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            "ZAR",
            [new CreateOrderLineItemInput("SKU-1", 1, -10m)]);

        var exception = Assert.Throws<FluentValidation.ValidationException>(() => validator.ValidateAndThrow(command));
        Assert.Contains("UnitPrice", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static decimal CalculateTotal(IEnumerable<(int Quantity, decimal UnitPrice)> lineItems) =>
        lineItems.Sum(item => item.Quantity * item.UnitPrice);
}
