using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NetPad.Filters;

/// <summary>
/// Mainly used to return a response that has no value to the caller, hiding any
/// exceptions that might be thrown and return values that the action might return.
/// </summary>
public class SilentResponseAttribute : ActionFilterAttribute
{
    private static readonly IActionResult _response = new NoContentResult();

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        context.Result = _response;
    }
}

