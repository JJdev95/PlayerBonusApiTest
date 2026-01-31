using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PlayerBonusApi.Common.Errors;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            ApiException api => (api.StatusCode, "Request failed", api.Message),
            KeyNotFoundException knf => (StatusCodes.Status404NotFound, "Not found", knf.Message),
            ArgumentException ae => (StatusCodes.Status400BadRequest, "Bad request", ae.Message),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", "Something went wrong.")
        };

        _logger.LogError(exception, "Unhandled exception");

        var problem = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = status,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

}
