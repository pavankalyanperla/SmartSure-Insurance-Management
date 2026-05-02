namespace AdminService.Application.Exceptions;

/// <summary>
/// Thrown when a required downstream service call fails and the operation cannot proceed.
/// </summary>
public sealed class DownstreamServiceException : AdminException
{
    public string ServiceName { get; }

    public DownstreamServiceException(string serviceName, string detail)
        : base($"The '{serviceName}' service is currently unavailable. {detail}", 502)
    {
        ServiceName = serviceName;
    }
}
