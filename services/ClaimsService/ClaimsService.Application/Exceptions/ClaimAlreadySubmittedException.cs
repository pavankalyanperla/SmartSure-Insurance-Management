namespace ClaimsService.Application.Exceptions;

/// <summary>Thrown when a claim is submitted but is not in Draft status.</summary>
public sealed class ClaimAlreadySubmittedException : ClaimException
{
    public ClaimAlreadySubmittedException(int claimId)
        : base($"Claim ID {claimId} has already been submitted and cannot be submitted again.", 400) { }
}
