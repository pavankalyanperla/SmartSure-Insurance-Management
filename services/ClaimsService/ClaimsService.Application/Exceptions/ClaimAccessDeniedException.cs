namespace ClaimsService.Application.Exceptions;

/// <summary>Thrown when a customer attempts to act on a claim they do not own.</summary>
public sealed class ClaimAccessDeniedException : ClaimException
{
    public ClaimAccessDeniedException(int claimId)
        : base($"You do not have permission to access claim ID {claimId}.", 403) { }
}
