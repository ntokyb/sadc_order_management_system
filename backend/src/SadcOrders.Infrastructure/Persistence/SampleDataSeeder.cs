using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SadcOrders.Domain.Entities;
using SadcOrders.Domain.Enums;

namespace SadcOrders.Infrastructure.Persistence;

public static class SampleDataSeeder
{
    public static async Task SeedIfEmptyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SadcOrdersDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SadcOrdersDbContext>>();

        if (await db.Customers.AnyAsync(cancellationToken))
            return;

        var now = DateTimeOffset.UtcNow;
        var customers = new[]
        {
            new Customer
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Acme Trading (ZA)",
                Email = "acme@example.co.za",
                CountryCode = "ZA",
                CreatedAt = now
            },
            new Customer
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Okavango Supplies (BW)",
                Email = "okavango@example.co.bw",
                CountryCode = "BW",
                CreatedAt = now
            },
            new Customer
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Harare Merchants (ZW)",
                Email = "harare@example.co.zw",
                CountryCode = "ZW",
                CreatedAt = now
            }
        };

        var orders = new List<Order>
        {
            CreateOrder(customers[0], "ZAR", 1250m, now, "SKU-ZA-001", "SKU-ZA-002"),
            CreateOrder(customers[0], "ZAR", 890.50m, now.AddMinutes(-5), "SKU-ZA-003", "SKU-ZA-004"),
            CreateOrder(customers[1], "BWP", 420m, now, "SKU-BW-001", "SKU-BW-002"),
            CreateOrder(customers[1], "BWP", 675.25m, now.AddMinutes(-10), "SKU-BW-003", "SKU-BW-004"),
            CreateOrder(customers[2], "USD", 310m, now, "SKU-ZW-001", "SKU-ZW-002"),
            CreateOrder(customers[2], "USD", 540m, now.AddMinutes(-15), "SKU-ZW-003", "SKU-ZW-004")
        };

        db.Customers.AddRange(customers);
        db.Orders.AddRange(orders);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {CustomerCount} customers and {OrderCount} orders", customers.Length, orders.Count);
    }

    private static Order CreateOrder(
        Customer customer,
        string currencyCode,
        decimal totalAmount,
        DateTimeOffset createdAt,
        string sku1,
        string sku2)
    {
        var orderId = Guid.NewGuid();
        var half = Math.Round(totalAmount / 2m, 2, MidpointRounding.AwayFromZero);
        var remainder = totalAmount - half;

        return new Order
        {
            Id = orderId,
            CustomerId = customer.Id,
            Customer = customer,
            Status = OrderStatus.Pending,
            CurrencyCode = currencyCode,
            TotalAmount = totalAmount,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            LineItems =
            [
                new OrderLineItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductSku = sku1,
                    Quantity = 1,
                    UnitPrice = half
                },
                new OrderLineItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductSku = sku2,
                    Quantity = 1,
                    UnitPrice = remainder
                }
            ]
        };
    }
}
