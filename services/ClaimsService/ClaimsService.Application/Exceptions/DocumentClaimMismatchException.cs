namespace ClaimsService.Application.Exceptions;

/// <summary>Thrown when a document does not belong to the specified claim.</summary>
public sealed class DocumentClaimMismatchException : ClaimException
{
    public DocumentClaimMismatchException(int documentId, int claimId)
        : base($"Document ID {documentId} does not belong to claim ID {claimId}.", 400) { }
}
