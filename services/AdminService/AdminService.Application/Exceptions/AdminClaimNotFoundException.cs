namespace AdminService.Application.Exceptions;

/// <summary>Thrown when a claim cannot be found via the claims service.</summary>
public sealed class AdminClaimNotFoundException : AdminException
{
    public AdminClaimNotFoundException(int claimId)
        : base($"Claim with ID {claimId} was not found.", 404) { }
}
