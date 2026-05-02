namespace IdentityService.Application.Exceptions;

/// <summary>Thrown when login credentials are invalid.</summary>
public sealed class InvalidCredentialsException : IdentityException
{
    public InvalidCredentialsException()
        : base("Invalid email or password.", 401) { }
}
