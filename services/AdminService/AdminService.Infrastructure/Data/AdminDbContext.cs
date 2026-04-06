namespace AdminService.Infrastructure.Data;

using AdminService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options)
    {
    }

    public DbSet<AdminLog> AdminLogs { get; set; }
    public DbSet<Report> Reports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure AdminLog
        modelBuilder.Entity<AdminLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TargetType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // Configure Report
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReportType).IsRequired();
            entity.Property(e => e.GeneratedBy).IsRequired();
            entity.Property(e => e.GeneratedAt).IsRequired();
            entity.Property(e => e.Data).HasColumnType("nvarchar(max)").IsRequired();
        });
    }
}
