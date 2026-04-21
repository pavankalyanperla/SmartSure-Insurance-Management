namespace IdentityService.Application.Interfaces;

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode);
}
