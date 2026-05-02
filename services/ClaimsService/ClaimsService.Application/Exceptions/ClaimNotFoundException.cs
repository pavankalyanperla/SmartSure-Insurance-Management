namespace ClaimsService.Application.Exceptions;

/// <summary>Thrown when a claim cannot be found by the given ID.</summary>
public sealed class ClaimNotFoundException : ClaimException
{
    public ClaimNotFoundException(int id)
        : base($"Claim with ID {id} was not found.", 404) { }
}
