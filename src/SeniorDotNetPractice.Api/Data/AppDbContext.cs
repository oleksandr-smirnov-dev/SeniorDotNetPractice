using Microsoft.EntityFrameworkCore;
using SeniorDotNetPractice.Api.Entities;

namespace SeniorDotNetPractice.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>()
            .Property(o => o.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Order>()
            .ToTable(tableBuilder =>
                tableBuilder.HasCheckConstraint(
                    "CK_Orders_Status_Valid",
                    "\"Status\" IN ('Pending', 'Rejected', 'Completed', 'Cancelled')"));

        modelBuilder.Entity<OrderItem>()
            .Property(i => i.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.OrderNumber)
            .IsUnique();

        modelBuilder.Entity<Order>()
            .Property(o => o.Version)
            .IsRowVersion();
    }
}
