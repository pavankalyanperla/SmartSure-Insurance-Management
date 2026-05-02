namespace PolicyService.Application.Exceptions;

/// <summary>Thrown when a user attempts to access or modify a policy they do not own.</summary>
public sealed class PolicyAccessDeniedException : PolicyException
{
    public PolicyAccessDeniedException(int policyId)
        : base($"You do not have permission to access policy ID {policyId}.", 403) { }
}
