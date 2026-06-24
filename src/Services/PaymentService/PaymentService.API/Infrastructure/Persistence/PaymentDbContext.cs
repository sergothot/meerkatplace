using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentService.API.Domain.Entities;

namespace PaymentService.API.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<ProcessedIntegrationMessage> ProcessedIntegrationMessages => Set<ProcessedIntegrationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentTransaction>()
            .HasIndex(t => t.OrderId)
            .IsUnique();

        modelBuilder.Entity<PaymentTransaction>()
            .Property(t => t.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Wallet>()
            .HasIndex(w => w.UserId)
            .IsUnique();

        modelBuilder.Entity<Wallet>()
            .Property(w => w.Balance)
            .HasPrecision(18, 2);

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
