using IdentityService.Application.DTOs;

namespace IdentityService.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<OtpSendResultDto> SendRegistrationOtpAsync(SendOtpRequestDto dto);
    Task<AuthResponseDto> VerifyRegistrationOtpAsync(VerifyRegistrationRequestDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<UserProfileDto?> GetProfileAsync(int userId);
    Task<List<UserProfileDto>> GetAllUsersAsync();
    Task<int> GetUsersCountAsync();
    Task<bool> UpdateUserStatusAsync(int userId, bool isActive);
}
