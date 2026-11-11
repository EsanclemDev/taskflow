
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TaskManager.Api.Filters;

public class ValidateAuthOriginAttribute : ActionFilterAttribute
{
    private static readonly HashSet<string> AllowedOrigins =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "http://localhost:4200",
            "https://taskflow-azh.pages.dev"
        };

    public override void OnActionExecuting(
        ActionExecutingContext context)
    {
        var origin = context.HttpContext.Request.Headers.Origin
            .ToString();

        if (string.IsNullOrWhiteSpace(origin) ||
            !AllowedOrigins.Contains(origin))
        {
            context.Result = new StatusCodeResult(
                StatusCodes.Status403Forbidden
            );

            return;
        }

        base.OnActionExecuting(context);
    }
}
