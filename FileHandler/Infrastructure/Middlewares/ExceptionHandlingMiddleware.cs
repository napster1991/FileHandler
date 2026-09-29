using FileHandler.Infrastructure.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace FileHandler.Infrastructure.Middlewares;

[ExcludeFromCodeCoverage]
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");

            var errorResponse = GetErrorResponse(ex);

            context.Response.ContentType = Application.Json;
            context.Response.StatusCode = (int)errorResponse.StatusCode;

            var jsonResponse = JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(jsonResponse);
        }
    }

    private static ErrorResponse GetErrorResponse(Exception exception) =>
        exception switch
        {
            FileNotFoundException => new(exception, HttpStatusCode.NotFound),
            ArgumentException => new(exception, HttpStatusCode.BadRequest),
            OperationCanceledException => new(exception, HttpStatusCode.RequestTimeout),
            _ => new(exception, HttpStatusCode.InternalServerError),
        };
}
