namespace AdminService.Application.Exceptions;

/// <summary>Thrown when an unrecognised report type is requested.</summary>
public sealed class InvalidReportTypeException : AdminException
{
    public InvalidReportTypeException(string reportType)
        : base($"'{reportType}' is not a valid report type.", 400) { }
}
