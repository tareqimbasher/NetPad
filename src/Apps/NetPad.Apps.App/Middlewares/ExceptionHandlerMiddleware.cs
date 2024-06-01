using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Dtos;

namespace NetPad.Middlewares;

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

            var result = new ErrorResult(ex.Message, webHostEnvironment.IsProduction() ? null : ex.ToString());

            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(JsonSerializer.Serialize(result));
        }
    }
}
