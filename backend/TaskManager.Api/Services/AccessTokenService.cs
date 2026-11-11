
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services;

public class AccessTokenService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOptionsMonitor<BearerTokenOptions> _bearerOptions;

    public AccessTokenService(
        SignInManager<ApplicationUser> signInManager,
        IOptionsMonitor<BearerTokenOptions> bearerOptions)
    {
        _signInManager = signInManager;
        _bearerOptions = bearerOptions;
    }

    public async Task<string> GenerateAsync(ApplicationUser user)
    {
        var principal =
            await _signInManager.CreateUserPrincipalAsync(user);

        var options = _bearerOptions.Get(
            BearerTokenDefaults.AuthenticationScheme);

        var expiresAt = DateTimeOffset.UtcNow
            .Add(options.BearerTokenExpiration);

        var properties = new AuthenticationProperties
        {
            IssuedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = expiresAt
        };

        var ticket = new AuthenticationTicket(
            principal,
            properties,
            BearerTokenDefaults.AuthenticationScheme
        );

        return options.BearerTokenProtector.Protect(ticket);
    }
}
