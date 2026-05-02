namespace PolicyService.Application.Exceptions;

/// <summary>Thrown when a policy type cannot be found by the given ID.</summary>
public sealed class PolicyTypeNotFoundException : PolicyException
{
    public PolicyTypeNotFoundException(int id)
        : base($"Policy type with ID {id} was not found.", 404) { }
}
