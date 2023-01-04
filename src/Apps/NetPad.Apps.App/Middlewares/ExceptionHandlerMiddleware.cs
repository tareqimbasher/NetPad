using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetPad.Common;

namespace NetPad.Middlewares;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, IWebHostEnvironment webHostEnvironment, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in request pipeline");

            var result = new ErrorResult(ex.Message, _webHostEnvironment.IsProduction() ? null : ex.ToString());

            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(JsonSerializer.Serialize(result));
        }
    }

    private class ErrorResult
    {
        public ErrorResult(string message, string? details = null)
        {
            Message = message;
            Details = details;
        }

        public string Message { get; }
        public string? Details { get; }
    }
}
