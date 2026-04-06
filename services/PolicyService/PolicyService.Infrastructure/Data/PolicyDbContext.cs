using Microsoft.EntityFrameworkCore;
using PolicyService.Domain.Entities;

namespace PolicyService.Infrastructure.Data;

public class PolicyDbContext : DbContext
{
    public PolicyDbContext(DbContextOptions<PolicyDbContext> options) : base(options) { }

    public DbSet<PolicyType> PolicyTypes => Set<PolicyType>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<Premium> Premiums => Set<Premium>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Policy>()
            .HasOne(p => p.PolicyType)
            .WithMany(t => t.Policies)
            .HasForeignKey(p => p.PolicyTypeId);

        modelBuilder.Entity<Policy>()
            .HasOne(p => p.Premium)
            .WithOne(pr => pr.Policy)
            .HasForeignKey<Premium>(pr => pr.PolicyId);

        modelBuilder.Entity<PolicyType>().HasData(
            new PolicyType { Id = 1, Name = "Health Insurance", Description = "Covers medical expenses", BaseAmount = 5000, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PolicyType { Id = 2, Name = "Life Insurance", Description = "Covers life risks", BaseAmount = 8000, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PolicyType { Id = 3, Name = "Auto Insurance", Description = "Covers vehicle damage", BaseAmount = 3000, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PolicyType { Id = 4, Name = "Home Insurance", Description = "Covers property damage", BaseAmount = 4000, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PolicyType { Id = 5, Name = "Travel Insurance", Description = "Covers travel risks", BaseAmount = 2000, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}