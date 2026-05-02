namespace PolicyService.Application.Exceptions;

/// <summary>Thrown when a payment record cannot be found for the given policy.</summary>
public sealed class PaymentNotFoundException : PolicyException
{
    public PaymentNotFoundException(int policyId)
        : base($"No payment record found for policy ID {policyId}.", 404) { }
}
