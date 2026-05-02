namespace IdentityService.Application.Exceptions;

/// <summary>Thrown when no account is found for the given email.</summary>
public sealed class UserNotFoundException : IdentityException
{
    public UserNotFoundException(string email)
        : base($"No account found with email '{email}'.", 404) { }
}
