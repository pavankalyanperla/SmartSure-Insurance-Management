namespace IdentityService.Application.Exceptions;

/// <summary>Thrown when a user account has been deactivated.</summary>
public sealed class AccountDeactivatedException : IdentityException
{
    public AccountDeactivatedException()
        : base("Your account has been deactivated. Please contact support.", 401) { }
}
