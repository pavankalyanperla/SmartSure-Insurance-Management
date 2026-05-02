namespace ClaimsService.Application.Exceptions;

/// <summary>Thrown when a document upload or deletion is attempted on a non-Draft claim.</summary>
public sealed class ClaimNotEditableException : ClaimException
{
    public ClaimNotEditableException(int claimId, string currentStatus)
        : base($"Claim ID {claimId} is in '{currentStatus}' status. Documents can only be managed on Draft claims.", 400) { }
}
