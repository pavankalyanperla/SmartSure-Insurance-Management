namespace IdentityService.Application.Exceptions;

/// <summary>Base class for all IdentityService domain exceptions.</summary>
public abstract class IdentityException : Exception
{
    public int StatusCode { get; }

    protected IdentityException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
