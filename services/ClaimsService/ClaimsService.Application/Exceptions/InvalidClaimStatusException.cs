namespace ClaimsService.Application.Exceptions;

/// <summary>Thrown when an unrecognised claim status string is provided.</summary>
public sealed class InvalidClaimStatusException : ClaimException
{
    public InvalidClaimStatusException(string status)
        : base($"'{status}' is not a valid claim status.", 400) { }
}
