using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Dtos;
using NetPad.Exceptions;

namespace NetPad.Host.Middlewares;

public class ExceptionHandlerMiddleware(
    RequestDelegate next,
    IWebHostEnvironment webHostEnvironment,
    ILogger<ExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred in request pipeline");

            var statusCode = ex is ScriptNotFoundException or EnvironmentNotFoundException
                ? HttpStatusCode.NotFound
                : HttpStatusCode.InternalServerError;

            var result = new ErrorResult(ex.Message, webHostEnvironment.IsProduction() ? null : ex.ToString());

            httpContext.Response.StatusCode = (int)statusCode;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(JsonSerializer.Serialize(result));
        }
    }
}
