using System.Net;
using System.Text.Json;

namespace real_time_task_management.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var statusCode = HttpStatusCode.InternalServerError;
        var message = "An internal server error occurred.";

        // You can add more specific exception types here
        switch (exception)
        {
            case ArgumentNullException:
                statusCode = HttpStatusCode.BadRequest;
                message = "A required argument was null.";
                break;
            case InvalidOperationException:
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
                break;
            case KeyNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                message = "The requested resource was not found.";
                break;
        }

        context.Response.StatusCode = (int)statusCode;

        var errorResponse = new
        {
            StatusCode = context.Response.StatusCode,
            Message = message,
            Detailed = exception.Message // Remove in production
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(errorResponse, options));
    }
}

// Extension method for easy registration
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}