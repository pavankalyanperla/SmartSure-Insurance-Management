namespace PolicyService.Application.Exceptions;

/// <summary>Thrown when a policy cannot be found by the given ID.</summary>
public sealed class PolicyNotFoundException : PolicyException
{
    public PolicyNotFoundException(int id)
        : base($"Policy with ID {id} was not found.", 404) { }
}
