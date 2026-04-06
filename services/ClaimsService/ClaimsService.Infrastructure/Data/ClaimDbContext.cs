using ClaimsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClaimsService.Infrastructure.Data;

public class ClaimDbContext : DbContext
{
    public ClaimDbContext(DbContextOptions<ClaimDbContext> options) : base(options)
    {
    }

    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimDocument> ClaimDocuments => Set<ClaimDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Claim>(entity =>
        {
            entity.Property(c => c.ClaimNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(c => c.Description)
                .HasMaxLength(1000);

            entity.HasMany(c => c.ClaimDocuments)
                .WithOne(d => d.Claim)
                .HasForeignKey(d => d.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClaimDocument>(entity =>
        {
            entity.Property(d => d.FileName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(d => d.FilePath)
                .IsRequired()
                .HasMaxLength(500);
        });
    }
}
