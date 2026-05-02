namespace AdminService.Application.Exceptions;

/// <summary>Base class for all AdminService domain exceptions.</summary>
public abstract class AdminException : Exception
{
    public int StatusCode { get; }

    protected AdminException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
