namespace ClaimsService.Application.Exceptions;

/// <summary>Base class for all ClaimsService domain exceptions.</summary>
public abstract class ClaimException : Exception
{
    public int StatusCode { get; }

    protected ClaimException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
