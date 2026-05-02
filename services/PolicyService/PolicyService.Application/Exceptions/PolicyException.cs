namespace PolicyService.Application.Exceptions;

/// <summary>Base class for all PolicyService domain exceptions.</summary>
public abstract class PolicyException : Exception
{
    public int StatusCode { get; }

    protected PolicyException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
