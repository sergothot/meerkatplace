using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.API.Domain.Entities;

namespace OrderService.API.Infrastructure.Persistence;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ProcessedIntegrationMessage> ProcessedIntegrationMessages => Set<ProcessedIntegrationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>()
            .HasIndex(c => c.BuyerId)
            .IsUnique();

        modelBuilder.Entity<Cart>()
            .HasMany(c => c.Items)
            .WithOne()
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.BuyerId);

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CartItem>()
            .Property(i => i.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .Property(i => i.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Shipment>()
            .HasIndex(s => s.OrderId);

        modelBuilder.Entity<ProcessedIntegrationMessage>()
            .HasIndex(x => x.MessageId)
            .IsUnique();

        modelBuilder.Entity<ProcessedIntegrationMessage>()
            .Property(x => x.MessageId)
            .HasMaxLength(128);

        modelBuilder.Entity<ProcessedIntegrationMessage>()
            .Property(x => x.Consumer)
            .HasMaxLength(128);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
