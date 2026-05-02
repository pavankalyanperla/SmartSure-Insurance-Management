namespace PolicyService.Application.Exceptions;

/// <summary>Thrown when a policy renewal is attempted on a policy in an ineligible status.</summary>
public sealed class PolicyNotRenewableException : PolicyException
{
    public PolicyNotRenewableException(string currentStatus)
        : base($"Policy cannot be renewed because its current status is '{currentStatus}'. Only Active or Expired policies can be renewed.", 400) { }
}
