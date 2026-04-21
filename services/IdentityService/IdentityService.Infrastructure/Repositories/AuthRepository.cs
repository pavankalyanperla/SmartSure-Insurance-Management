using IdentityService.Domain.Entities;
using IdentityService.Domain.Interfaces;
using IdentityService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Infrastructure.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly AppDbContext _context;

    public AuthRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLower());
    }

    public async Task<User> CreateUserAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email.ToLower());
    }

    public async Task<OtpVerification> UpsertOtpAsync(string email, string otpCode, DateTime expiresAt)
    {
        var normalizedEmail = email.ToLower().Trim();
        var existing = await _context.Set<OtpVerification>()
            .Where(o => o.Email == normalizedEmail && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            existing.OtpCode = otpCode;
            existing.ExpiresAt = expiresAt;
            existing.CreatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return existing;
        }

        var otp = new OtpVerification
        {
            Email = normalizedEmail,
            OtpCode = otpCode,
            ExpiresAt = expiresAt,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<OtpVerification>().Add(otp);
        await _context.SaveChangesAsync();
        return otp;
    }

    public async Task<OtpVerification?> GetLatestOtpAsync(string email)
    {
        var normalizedEmail = email.ToLower().Trim();
        return await _context.Set<OtpVerification>()
            .Where(o => o.Email == normalizedEmail)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task MarkOtpAsUsedAsync(int otpId)
    {
        var otp = await _context.Set<OtpVerification>().FindAsync(otpId);
        if (otp is null)
        {
            return;
        }

        otp.IsUsed = true;
        await _context.SaveChangesAsync();
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetUsersCountAsync()
    {
        return await _context.Users.CountAsync();
    }

    public async Task<bool> UpdateUserStatusAsync(int userId, bool isActive)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null)
        {
            return false;
        }

        user.IsActive = isActive;
        await _context.SaveChangesAsync();
        return true;
    }
}
