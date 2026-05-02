using IdentityService.Application.Exceptions;
using System.Net;
using System.Text.Json;

namespace IdentityService.API.Middlewares;

/// <summary>
/// Global exception handling middleware for IdentityService.
/// Catches all unhandled exceptions, maps them to structured JSON error responses,
/// and ensures no raw exception details leak to the client in production.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            // ── Domain-specific exceptions (typed, expected) ──────────────────
            IdentityException identityEx
                => (identityEx.StatusCode, identityEx.Message),

            // ── Fallback for any remaining generic exceptions ─────────────────
            _   => (StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred. Please try again later.")
        };

        // Log at appropriate level: domain exceptions are warnings, unknowns are errors
        if (exception is IdentityException)
            _logger.LogWarning("Identity domain exception [{Type}]: {Message}", exception.GetType().Name, exception.Message);
        else
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = statusCode;

        var response = new ErrorResponse
        {
            StatusCode = statusCode,
            Message    = message,
            // Only expose stack trace in Development to avoid leaking internals
            Detail     = _env.IsDevelopment() && exception is not IdentityException
                             ? exception.ToString()
                             : null
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}

/// <summary>Standardised error response envelope.</summary>
public sealed record ErrorResponse
{
    public int     StatusCode { get; init; }
    public string  Message    { get; init; } = string.Empty;
    public string? Detail     { get; init; }
}
