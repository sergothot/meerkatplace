using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UserService.API.Domain.Entities;
using UserService.API.Domain.Enums;

namespace UserService.API.Infrastructure.Persistence;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<BuyerProfile> BuyerProfiles => Set<BuyerProfile>();
    public DbSet<SellerProfile> SellerProfiles => Set<SellerProfile>();
    public DbSet<Address> Addresses => Set<Address>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var rolesConverter = new ValueConverter<List<UserRole>, string>(
            v => string.Join(',', v.Select(r => (int)r)),
            v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(r => (UserRole)int.Parse(r))
                  .ToList()
        );

        modelBuilder.Entity<User>()
            .Property(u => u.Roles)
            .HasConversion(rolesConverter);

        modelBuilder.Entity<BuyerProfile>()
            .HasIndex(b => b.UserId)
            .IsUnique();

        modelBuilder.Entity<SellerProfile>()
            .HasIndex(s => s.UserId)
            .IsUnique();

        modelBuilder.Entity<SellerProfile>()
            .HasIndex(s => s.StoreName)
            .IsUnique();

        modelBuilder.Entity<Address>()
            .HasIndex(a => a.UserId);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Login)
            .IsUnique();
    }
}
