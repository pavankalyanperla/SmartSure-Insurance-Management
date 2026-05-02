namespace AdminService.Application.Exceptions;

/// <summary>Thrown when a user cannot be found in the identity service.</summary>
public sealed class AdminUserNotFoundException : AdminException
{
    public AdminUserNotFoundException(int userId)
        : base($"User with ID {userId} was not found.", 404) { }
}
