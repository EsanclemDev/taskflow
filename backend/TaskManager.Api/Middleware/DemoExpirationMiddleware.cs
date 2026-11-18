using System.Globalization;
using TaskManager.Api.Services;

namespace TaskManager.Api.Middleware;

public sealed class DemoExpirationMiddleware
{
    private readonly RequestDelegate _next;

    public DemoExpirationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var expires = context.User.FindFirst(
                AccessTokenService.DemoExpiresClaim)?.Value;

            if (expires != null &&
                (!long.TryParse(expires, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var timestamp) ||
                 DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= timestamp))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await _next(context);
    }
}
