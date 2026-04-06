using IdentityService.Domain.Entities;

namespace IdentityService.Domain.Interfaces;

public interface IAuthRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateUserAsync(User user);
    Task<User?> GetByIdAsync(int id);
    Task<bool> EmailExistsAsync(string email);
    Task<List<User>> GetAllUsersAsync();
    Task<int> GetUsersCountAsync();
    Task<bool> UpdateUserStatusAsync(int userId, bool isActive);
}
