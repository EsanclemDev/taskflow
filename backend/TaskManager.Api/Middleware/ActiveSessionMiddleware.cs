
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Services;

namespace TaskManager.Api.Middleware;

public sealed class ActiveSessionMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveSessionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ApplicationDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var familyClaim = context.User.FindFirst(
                AccessTokenService.FamilyIdClaim)?.Value;

            var userId = context.User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            var validFamilyId = Guid.TryParse(
                familyClaim,
                out var familyId);

            var active = false;

            if (validFamilyId && !string.IsNullOrWhiteSpace(userId))
            {
                var now = DateTime.UtcNow;

                active = await db.RefreshSessions
                    .AsNoTracking()
                    .AnyAsync(session =>
                        session.FamilyId == familyId &&
                        session.UserId == userId &&
                        session.RevokedAt == null &&
                        session.ExpiresAt > now);
            }

            if (!active)
            {
                context.User = new ClaimsPrincipal(
                    new ClaimsIdentity());
            }
        }

        await _next(context);
    }
}
