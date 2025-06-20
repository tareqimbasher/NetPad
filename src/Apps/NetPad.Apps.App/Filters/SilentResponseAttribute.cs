using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NetPad.Filters;

/// <summary>
/// Used to force return an empty response (204 No Content) to the caller, hiding any
/// exceptions that might be thrown and any return values that the action produces.
/// </summary>
public class SilentResponseAttribute() : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        try
        {
            var executedContext = await next();

            if (executedContext.Exception != null && !executedContext.ExceptionHandled)
            {
                GetLogger(context).LogError(executedContext.Exception, "Exception occurred during action execution.");
                executedContext.ExceptionHandled = true;
            }

            executedContext.Result = new NoContentResult();
        }
        catch (Exception ex)
        {
            GetLogger(context).LogError(ex, "Unhandled exception during action filter execution.");
        }

        context.Result = new NoContentResult();
    }

    private static ILogger GetLogger(ActionExecutingContext context)
    {
        return context.HttpContext.RequestServices.GetRequiredService<ILogger<SilentResponseAttribute>>();
    }
}
