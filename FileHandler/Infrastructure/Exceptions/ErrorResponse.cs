using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace FileHandler.Infrastructure.Exceptions;

[ExcludeFromCodeCoverage]
public class ErrorResponse(Exception ex, HttpStatusCode statusCode)
{
    public HttpStatusCode StatusCode { get; set; } = statusCode;
    public string Message { get; set; } = ex.Message;
}
