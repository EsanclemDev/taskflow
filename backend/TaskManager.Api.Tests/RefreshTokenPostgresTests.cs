
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Models;
using TaskManager.Api.Services;

namespace TaskManager.Api.Tests;

public class RefreshTokenPostgresTests
{
    // Base de datos exclusiva de pruebas.
    // Nunca utilizar taskmanager_beta ni Neon.
    private const string ConnectionString =
        "Host=localhost;Port=5432;" +
        "Database=taskmanager_refresh_tests;" +
        "Username=devuser;Password=devpassword123";

    private static ApplicationDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        ApplicationDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        var id = Guid.NewGuid().ToString();
        var email = $"refresh-{id}@taskflow.local";

        var user = new ApplicationUser
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString()
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    [Fact]
    public async Task ValidToken_ShouldRotateSuccessfully()
    {
        await using var context = CreateContext();

        var user = await CreateUserAsync(context);
        var service = new RefreshTokenService(context);

        var originalToken =
            await service.CreateSessionAsync(user);

        var result =
            await service.RotateSessionAsync(originalToken);

        Assert.Equal(
            RefreshRotationStatus.Success,
            result.Status);

        Assert.Equal(user.Id, result.UserId);

        Assert.False(
            string.IsNullOrWhiteSpace(result.RefreshToken));

        Assert.NotEqual(
            originalToken,
            result.RefreshToken);

        Assert.NotNull(result.FamilyId);

        var oldSession =
            await service.FindValidSessionAsync(originalToken);

        Assert.Null(oldSession);

        var newSession =
            await service.FindValidSessionAsync(
                result.RefreshToken!);

        Assert.NotNull(newSession);
        Assert.Equal(result.FamilyId, newSession.FamilyId);
    }

    [Fact]
    public async Task InvalidToken_ShouldBeRejected()
    {
        await using var context = CreateContext();

        await CreateUserAsync(context);

        var service = new RefreshTokenService(context);

        var result = await service.RotateSessionAsync(
            "invalid-refresh-token");

        Assert.Equal(
            RefreshRotationStatus.InvalidToken,
            result.Status);
    }

    [Fact]
    public async Task ExpiredToken_ShouldBeRejected()
    {
        await using var context = CreateContext();

        var user = await CreateUserAsync(context);
        var service = new RefreshTokenService(context);

        var token = await service.CreateSessionAsync(user);
        var hash = service.HashToken(token);

        await context.RefreshSessions
            .Where(s => s.TokenHash == hash)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    s => s.ExpiresAt,
                    DateTime.UtcNow.AddMinutes(-1)));

        var result = await service.RotateSessionAsync(token);

        Assert.Equal(
            RefreshRotationStatus.ExpiredToken,
            result.Status);
    }

    [Fact]
    public async Task RotatedToken_ShouldNotBeReusable()
    {
        await using var context = CreateContext();

        var user = await CreateUserAsync(context);
        var service = new RefreshTokenService(context);

        var originalToken =
            await service.CreateSessionAsync(user);

        var first =
            await service.RotateSessionAsync(originalToken);

        Assert.Equal(
            RefreshRotationStatus.Success,
            first.Status);

        var second =
            await service.RotateSessionAsync(originalToken);

        Assert.Equal(
            RefreshRotationStatus.AlreadyRotated,
            second.Status);

        Assert.Null(second.RefreshToken);

        // El token nuevo debe continuar siendo válido.
        var activeSession =
            await service.FindValidSessionAsync(
                first.RefreshToken!);

        Assert.NotNull(activeSession);
    }

    [Fact]
    public async Task Logout_ShouldInvalidateRefreshToken()
    {
        await using var context = CreateContext();

        var user = await CreateUserAsync(context);
        var service = new RefreshTokenService(context);

        var token = await service.CreateSessionAsync(user);

        var revoked = await service.RevokeSessionAsync(token);

        Assert.True(revoked);

        var result = await service.RotateSessionAsync(token);

        Assert.Equal(
            RefreshRotationStatus.LoggedOut,
            result.Status);
    }

    [Fact]
    public async Task ConcurrentRotation_OnlyOneShouldSucceed()
    {
        string token;

        await using (var setupContext = CreateContext())
        {
            var user = await CreateUserAsync(setupContext);

            var setupService =
                new RefreshTokenService(setupContext);

            token = await setupService.CreateSessionAsync(user);
        }

        // Cada petición usa su propio DbContext,
        // como sucede en las peticiones HTTP reales.
        await using var context1 = CreateContext();
        await using var context2 = CreateContext();

        var service1 = new RefreshTokenService(context1);
        var service2 = new RefreshTokenService(context2);

        var results = await Task.WhenAll(
            service1.RotateSessionAsync(token),
            service2.RotateSessionAsync(token));

        Assert.Single(
            results,
            r => r.Status == RefreshRotationStatus.Success);

        Assert.Single(
            results,
            r => r.Status != RefreshRotationStatus.Success);

        var successful =
            results.Single(
                r => r.Status == RefreshRotationStatus.Success);

        var rejected =
            results.Single(
                r => r.Status != RefreshRotationStatus.Success);

        Assert.Contains(
            rejected.Status,
            new[]
            {
                RefreshRotationStatus.AlreadyRotated,
                RefreshRotationStatus.InvalidToken
            });

        Assert.NotNull(successful.RefreshToken);

        await using var verificationContext = CreateContext();

        var verificationService =
            new RefreshTokenService(verificationContext);

        var activeSession =
            await verificationService.FindValidSessionAsync(
                successful.RefreshToken!);

        Assert.NotNull(activeSession);
    }

    [Fact]
    public async Task LogoutFamily_ShouldInvalidateCurrentToken()
    {
        await using var context = CreateContext();

        var user = await CreateUserAsync(context);
        var service = new RefreshTokenService(context);

        var token = await service.CreateSessionAsync(user);

        var loggedOut = await service.LogoutFamilyAsync(token);

        Assert.True(loggedOut);

        var result = await service.RotateSessionAsync(token);

        Assert.Equal(
            RefreshRotationStatus.LoggedOut,
            result.Status);
    }

    [Fact]
    public async Task LogoutWithOldToken_ShouldRevokeRotatedSuccessor()
    {
        await using var context = CreateContext();

        var user = await CreateUserAsync(context);
        var service = new RefreshTokenService(context);

        var originalToken =
            await service.CreateSessionAsync(user);

        var rotation =
            await service.RotateSessionAsync(originalToken);

        Assert.Equal(
            RefreshRotationStatus.Success,
            rotation.Status);

        // Simulamos una pestaña que conserva el token antiguo.
        var loggedOut =
            await service.LogoutFamilyAsync(originalToken);

        Assert.True(loggedOut);

        var activeSession =
            await service.FindValidSessionAsync(
                rotation.RefreshToken!);

        Assert.Null(activeSession);

        var result =
            await service.RotateSessionAsync(
                rotation.RefreshToken!);

        Assert.Equal(
            RefreshRotationStatus.LoggedOut,
            result.Status);
    }

    [Fact]
    public async Task LogoutFamily_ShouldNotAffectAnotherFamily()
    {
        await using var context = CreateContext();

        var user = await CreateUserAsync(context);
        var service = new RefreshTokenService(context);

        var firstToken =
            await service.CreateSessionAsync(user);

        var secondToken =
            await service.CreateSessionAsync(user);

        // Dos logins del mismo usuario generan familias distintas.
        var loggedOut =
            await service.LogoutFamilyAsync(firstToken);

        Assert.True(loggedOut);

        var firstSession =
            await service.FindValidSessionAsync(firstToken);

        var secondSession =
            await service.FindValidSessionAsync(secondToken);

        Assert.Null(firstSession);
        Assert.NotNull(secondSession);
    }

}
