namespace PolicyService.Application.Exceptions;

/// <summary>Thrown when an unrecognised policy status string is provided.</summary>
public sealed class InvalidPolicyStatusException : PolicyException
{
    public InvalidPolicyStatusException(string status)
        : base($"'{status}' is not a valid policy status.", 400) { }
}
