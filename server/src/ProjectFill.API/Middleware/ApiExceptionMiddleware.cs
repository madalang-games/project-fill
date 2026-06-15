using System.Net;
using MySqlConnector;
using ProjectFill.Application.Common;
using ProjectFill.Contracts.Common;

namespace ProjectFill.API.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (GameApiException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsJsonAsync(new ErrorResponse { Code = ex.Code });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized [{CorrelationId}]", context.Items["CorrelationId"]);
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsJsonAsync(new ErrorResponse { Code = ErrorCodes.Unauthorized });
        }
        catch (MySqlException ex) when (ex.Number is 1205 or 1213 or 3572)
        {
            _logger.LogWarning(ex, "DB concurrency [{CorrelationId}]", context.Items["CorrelationId"]);
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            await context.Response.WriteAsJsonAsync(new ErrorResponse { Code = ErrorCodes.ConcurrentModification });
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString();
            _logger.LogError(ex, "Unhandled exception [{CorrelationId}] {Method} {Path}",
                correlationId, context.Request.Method, context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsJsonAsync(new ErrorResponse { Code = ErrorCodes.InternalError });
        }
    }
}
