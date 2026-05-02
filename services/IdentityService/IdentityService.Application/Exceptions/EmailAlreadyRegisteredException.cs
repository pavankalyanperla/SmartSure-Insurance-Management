namespace IdentityService.Application.Exceptions;

/// <summary>Thrown when a registration attempt uses an email that already exists.</summary>
public sealed class EmailAlreadyRegisteredException : IdentityException
{
    public EmailAlreadyRegisteredException(string email)
        : base($"The email '{email}' is already registered.", 409) { }
}
