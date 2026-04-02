using System.IO;
using Microsoft.AspNetCore.Http;
using NetPad.Apps.Security;

namespace NetPad.Host.Middlewares;

public class TokenValidationMiddleware(RequestDelegate next, SecurityToken securityToken)
{
    private static readonly PathString _identifierPath = new("/app/identifier");

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsExempt(context))
        {
            await next(context);
            return;
        }

        var token = ExtractToken(context);

        if (!securityToken.Validate(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }

    private static bool IsExempt(HttpContext context)
    {
        var path = context.Request.Path;

        // Static assets (.js, .css, .png, .woff, etc.) don't need token protection.
        // In release, UseSpaStaticFiles() short-circuits before this middleware.
        // In debug, the webpack dev server proxy serves these through UseSpa(),
        // so they pass through this middleware and must be explicitly exempted.
        if (Path.HasExtension(path.Value))
        {
            return true;
        }

        if (path.StartsWithSegments(_identifierPath))
        {
            return true;
        }

#if DEBUG
        if (path.StartsWithSegments("/swagger"))
        {
            return true;
        }
#endif

        return false;
    }

    private static string? ExtractToken(HttpContext context)
    {
        // Query parameter: ?token=xxx
        string? token = context.Request.Query["token"];
        if (!string.IsNullOrEmpty(token)) return token;

        // SignalR sends token as ?access_token=xxx
        token = context.Request.Query["access_token"];
        if (!string.IsNullOrEmpty(token)) return token;

        // Header: X-NetPad-Token
        token = context.Request.Headers["X-NetPad-Token"];
        if (!string.IsNullOrEmpty(token)) return token;

        // Authorization: Bearer xxx
        string? auth = context.Request.Headers.Authorization;
        if (auth != null && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return auth["Bearer ".Length..];
        }

        return null;
    }
}
