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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>()
            .Property(o => o.Status)
            .HasConversion<string>();
    }
}
