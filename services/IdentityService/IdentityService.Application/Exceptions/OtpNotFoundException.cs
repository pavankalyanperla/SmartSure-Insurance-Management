namespace IdentityService.Application.Exceptions;

/// <summary>Thrown when no active (unused, unexpired) OTP exists for the email.</summary>
public sealed class OtpNotFoundException : IdentityException
{
    public OtpNotFoundException()
        : base("No active OTP found. Please request a new OTP.", 400) { }
}
