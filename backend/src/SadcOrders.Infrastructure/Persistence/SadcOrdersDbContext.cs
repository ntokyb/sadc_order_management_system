using Microsoft.EntityFrameworkCore;
using SadcOrders.Application.Abstractions;
using SadcOrders.Domain.Entities;

namespace SadcOrders.Infrastructure.Persistence;

public class SadcOrdersDbContext(DbContextOptions<SadcOrdersDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLineItem> OrderLineItems => Set<OrderLineItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("Customers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(320).IsRequired();
            e.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
            e.HasIndex(x => x.Email);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("Orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasIndex(x => new { x.CustomerId, x.Status, x.CreatedAt });
            e.HasOne(x => x.Customer).WithMany(x => x.Orders).HasForeignKey(x => x.CustomerId);
        });

        modelBuilder.Entity<OrderLineItem>(e =>
        {
            e.ToTable("OrderLineItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductSku).HasMaxLength(64).IsRequired();
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.HasOne(x => x.Order).WithMany(x => x.LineItems).HasForeignKey(x => x.OrderId);
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("OutboxMessages");
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            e.Property(x => x.Payload).IsRequired();
            e.HasIndex(x => x.PublishedAt);
        });

        modelBuilder.Entity<IdempotencyRecord>(e =>
        {
            e.ToTable("IdempotencyRecords");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.Key).IsUnique();
        });
    }
}
