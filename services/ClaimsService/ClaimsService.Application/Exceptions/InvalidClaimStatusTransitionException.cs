namespace ClaimsService.Application.Exceptions;

/// <summary>Thrown when an invalid status transition is attempted on a claim.</summary>
public sealed class InvalidClaimStatusTransitionException : ClaimException
{
    public InvalidClaimStatusTransitionException(string fromStatus, string toStatus)
        : base($"Cannot transition claim from '{fromStatus}' to '{toStatus}'. This transition is not permitted.", 400) { }
}
