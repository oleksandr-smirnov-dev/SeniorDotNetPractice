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
}
