using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure;

public sealed class MasterDbContext(DbContextOptions<MasterDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.ConnectionString).IsRequired();
            e.Property(t => t.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(t => t.Name).IsUnique();
        });
    }
}
