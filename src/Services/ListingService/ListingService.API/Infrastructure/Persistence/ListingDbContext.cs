using MassTransit;
using ListingService.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ListingService.API.Infrastructure.Persistence;

public class ListingDbContext : DbContext
{
    public ListingDbContext(DbContextOptions<ListingDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryStock> InventoryStocks => Set<InventoryStock>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.SellerId);

        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<InventoryStock>()
            .HasIndex(s => s.ProductId)
            .IsUnique();

        modelBuilder.Entity<StockReservation>()
            .HasIndex(r => r.OrderId)
            .IsUnique();

        modelBuilder.Entity<StockReservation>()
            .Property(r => r.FailureReason)
            .HasMaxLength(512);

        modelBuilder.Entity<Review>()
            .HasIndex(r => new { r.ProductId, r.BuyerId })
            .IsUnique();

        modelBuilder.Entity<Review>()
            .HasIndex(r => r.SellerId);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
