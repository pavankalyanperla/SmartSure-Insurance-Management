namespace ClaimsService.Application.Exceptions;

/// <summary>Thrown when a claim document cannot be found by the given ID.</summary>
public sealed class ClaimDocumentNotFoundException : ClaimException
{
    public ClaimDocumentNotFoundException(int id)
        : base($"Document with ID {id} was not found.", 404) { }
}
