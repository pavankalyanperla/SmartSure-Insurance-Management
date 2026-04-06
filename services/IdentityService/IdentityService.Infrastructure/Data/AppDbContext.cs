using IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Role).HasDefaultValue("CUSTOMER");
        });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            FullName = "Super Admin",
            Email = "admin@smartsure.com",
            PasswordHash = "$2a$11$n4QFnj5Bj8GdgMwzVSNusesOqeVoQbcYo0d6YkRWJErooVyWTPNTi",
            Role = "ADMIN",
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
