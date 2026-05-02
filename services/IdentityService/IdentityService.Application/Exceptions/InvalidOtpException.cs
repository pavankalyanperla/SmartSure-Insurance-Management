namespace IdentityService.Application.Exceptions;

/// <summary>Thrown when the submitted OTP code does not match the stored code.</summary>
public sealed class InvalidOtpException : IdentityException
{
    public InvalidOtpException()
        : base("Invalid OTP code. Please check and try again.", 400) { }
}
