
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services;

public class AccessTokenService
{
    public const string FamilyIdClaim = "taskflow:family_id";
    public const string DemoExpiresClaim = "taskflow:demo_expires";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOptionsMonitor<BearerTokenOptions> _bearerOptions;

    public AccessTokenService(
        SignInManager<ApplicationUser> signInManager,
        IOptionsMonitor<BearerTokenOptions> bearerOptions)
    {
        _signInManager = signInManager;
        _bearerOptions = bearerOptions;
    }

    public async Task<string> GenerateAsync(
        ApplicationUser user,
        Guid familyId)
    {
        var originalPrincipal =
            await _signInManager.CreateUserPrincipalAsync(user);

        var identity = new ClaimsIdentity(
            originalPrincipal.Claims,
            originalPrincipal.Identity?.AuthenticationType);

        identity.AddClaim(new Claim(
            FamilyIdClaim,
            familyId.ToString("D")));

        if (user.IsDemo && user.DemoExpiresAt.HasValue)
        {
            var expires = new DateTimeOffset(user.DemoExpiresAt.Value)
                .ToUnixTimeSeconds();
            identity.AddClaim(new Claim(
                DemoExpiresClaim,
                expires.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        var principal = new ClaimsPrincipal(identity);

        var options = _bearerOptions.Get(
            BearerTokenDefaults.AuthenticationScheme);

        var now = DateTimeOffset.UtcNow;

        var properties = new AuthenticationProperties
        {
            IssuedUtc = now,
            ExpiresUtc = now.Add(options.BearerTokenExpiration)
        };

        var ticket = new AuthenticationTicket(
            principal,
            properties,
            BearerTokenDefaults.AuthenticationScheme);

        return options.BearerTokenProtector.Protect(ticket);
    }
}
