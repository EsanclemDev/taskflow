using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskManager.Api.Middleware;
using TaskManager.Api.Services;
using Xunit;

namespace TaskManager.Api.Tests;

public class DemoExpirationMiddlewareTests
{
    [Fact]
    public async Task ExpiredDemoClaim_ShouldReturnUnauthorizedWithoutCallingNext()
    {
        var called = false;
        var middleware = new DemoExpirationMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "demo-user"),
                new Claim(AccessTokenService.DemoExpiresClaim,
                    DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds().ToString())
            }, "Test"))
        };

        await middleware.InvokeAsync(context);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(called);
    }

    [Fact]
    public async Task ValidDemoClaim_ShouldContinue()
    {
        var called = false;
        var middleware = new DemoExpirationMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(AccessTokenService.DemoExpiresClaim,
                    DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString())
            }, "Test"))
        };
        await middleware.InvokeAsync(context);
        Assert.True(called);
    }

    [Fact]
    public async Task NormalAuthenticatedUser_ShouldContinue()
    {
        var called = false;
        var middleware = new DemoExpirationMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "normal-user")
            }, "Test"))
        };
        await middleware.InvokeAsync(context);
        Assert.True(called);
    }
}
