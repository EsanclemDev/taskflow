
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services;

public class RefreshTokenService
{
    private readonly ApplicationDbContext _context;

    public RefreshTokenService(ApplicationDbContext context)
    {
        _context = context;
    }

    public string GenerateToken()
    {
        return Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string HashToken(string token)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public async Task<string> CreateSessionAsync(
        ApplicationUser user)
    {
        var now = DateTime.UtcNow;
        var refreshToken = GenerateToken();

        _context.RefreshSessions.Add(new RefreshSession
        {
            FamilyId = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(7)
        });

        await _context.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<RefreshSession?> FindValidSessionAsync(
        string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var hash = HashToken(refreshToken);
        var now = DateTime.UtcNow;

        return await _context.RefreshSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.TokenHash == hash &&
                s.RevokedAt == null &&
                s.ExpiresAt > now);
    }

    public async Task<RefreshRotationResult> RotateSessionAsync(
        string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return new(RefreshRotationStatus.InvalidToken);
        }

        var tokenHash = HashToken(refreshToken);
        var now = DateTime.UtcNow;

        // Consultamos también tokens revocados para
        // poder detectar intentos de reutilización.
        var session = await _context.RefreshSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TokenHash == tokenHash);

        if (session == null)
            return new(RefreshRotationStatus.InvalidToken);

        if (session.ExpiresAt <= now)
            return new(RefreshRotationStatus.ExpiredToken);

        if (session.RevocationReason == "Logout" ||
            session.RevocationReason == "ReuseDetected")
        {
            return new(RefreshRotationStatus.LoggedOut);
        }

        if (session.RevocationReason == "Rotated")
        {
            return await HandleRotatedTokenAsync(session, now);
        }

        if (session.RevokedAt != null)
            return new(RefreshRotationStatus.InvalidToken);

        // Impide renovar una familia ya invalidada.
        var familyRevoked = await _context.RefreshSessions
            .AnyAsync(s =>
                s.FamilyId == session.FamilyId &&
                s.RevocationReason == "ReuseDetected");

        if (familyRevoked)
            return new(RefreshRotationStatus.LoggedOut);

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        // Actualización condicional: solo una petición
        // puede consumir el refresh token.
        var affectedRows = await _context.RefreshSessions
            .Where(s =>
                s.Id == session.Id &&
                s.RevokedAt == null &&
                s.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.RevokedAt, now)
                .SetProperty(s => s.RevocationReason, "Rotated")
                .SetProperty(s => s.LastUsedAt, now));

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync();

            // Otra petición puede haberlo rotado.
            var updatedSession = await _context.RefreshSessions
                .AsNoTracking()
                .FirstAsync(s => s.Id == session.Id);

            if (updatedSession.RevocationReason == "Rotated")
            {
                return await HandleRotatedTokenAsync(
                    updatedSession,
                    DateTime.UtcNow);
            }

            return new(RefreshRotationStatus.InvalidToken);
        }

        var newToken = GenerateToken();

        _context.RefreshSessions.Add(new RefreshSession
        {
            FamilyId = session.FamilyId,
            UserId = session.UserId,
            TokenHash = HashToken(newToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(7)
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new RefreshRotationResult(
            RefreshRotationStatus.Success,
            session.UserId,
            newToken,
            session.FamilyId);
    }

    
private Task<RefreshRotationResult> HandleRotatedTokenAsync(
    RefreshSession session,
    DateTime now)
{
    // Un token rotado nunca puede volver a utilizarse.
    //
    // No revocamos automáticamente toda la familia:
    // una petición concurrente o una pestaña suspendida
    // también puede presentar un token antiguo.
    //
    // Registraremos estos intentos para poder aplicar
    // una política de detección más precisa.

    return Task.FromResult(
        new RefreshRotationResult(
            RefreshRotationStatus.AlreadyRotated,
            FamilyId: session.FamilyId
        )
    );
}


    public async Task<int> RevokeFamilyAsync(Guid familyId)
    {
        var now = DateTime.UtcNow;

        return await _context.RefreshSessions
            .Where(s =>
                s.FamilyId == familyId &&
                s.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.RevokedAt, now)
                .SetProperty(s => s.RevocationReason, "ReuseDetected"));
    }

    public async Task<bool> LogoutFamilyAsync(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        var tokenHash = HashToken(refreshToken);

        // Buscamos también tokens previamente rotados.
        // El navegador puede enviar un token antiguo si
        // otra pestaña acaba de renovar la sesión.
        var session = await _context.RefreshSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TokenHash == tokenHash);

        if (session == null)
            return false;

        var now = DateTime.UtcNow;

        var affectedRows = await _context.RefreshSessions
            .Where(s =>
                s.FamilyId == session.FamilyId &&
                s.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.RevokedAt, now)
                .SetProperty(s => s.RevocationReason, "Logout"));

        return affectedRows > 0;
    }

    public async Task<bool> RevokeSessionAsync(
        string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        var hash = HashToken(refreshToken);
        var now = DateTime.UtcNow;

        var affectedRows = await _context.RefreshSessions
            .Where(s =>
                s.TokenHash == hash &&
                s.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.RevokedAt, now)
                .SetProperty(s => s.RevocationReason, "Logout"));

        return affectedRows == 1;
    }
}
