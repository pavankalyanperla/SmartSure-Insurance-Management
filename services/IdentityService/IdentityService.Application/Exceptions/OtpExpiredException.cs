namespace IdentityService.Application.Exceptions;

/// <summary>Thrown when the submitted OTP has passed its expiry time.</summary>
public sealed class OtpExpiredException : IdentityException
{
    public OtpExpiredException()
        : base("OTP has expired. Please request a new OTP.", 400) { }
}
