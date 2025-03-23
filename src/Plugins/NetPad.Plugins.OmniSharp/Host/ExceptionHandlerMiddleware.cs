using System.Net;
using NetPad.Common;
using NetPad.Plugins.OmniSharp.Exceptions;

namespace NetPad.Plugins.OmniSharp.Host;

public class ExceptionHandlerMiddleware(
    RequestDelegate next,
    IWebHostEnvironment webHostEnvironment)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (OmniSharpInstanceNotFound ex)
        {
            var result = new ErrorResult(
                ex.Message,
                webHostEnvironment.IsProduction() ? null : ex.ToString());

            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(JsonSerializer.Serialize(result));
        }
    }
}

public class ErrorResult(string message, string? details = null)
{
    public string Message { get; } = message;
    public string? Details { get; } = details;
}
