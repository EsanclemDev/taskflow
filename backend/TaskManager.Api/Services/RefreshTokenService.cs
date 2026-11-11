
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
            SHA256.HashData(
                Encoding.UTF8.GetBytes(token)));
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
            return new RefreshRotationResult(
                RefreshRotationStatus.InvalidToken);
        }

        var tokenHash = HashToken(refreshToken);

        // Primera lectura para localizar la familia.
        // El estado se comprobará de nuevo después
        // de adquirir el bloqueo transaccional.
        var session = await _context.RefreshSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.TokenHash == tokenHash);

        if (session == null)
        {
            return new RefreshRotationResult(
                RefreshRotationStatus.InvalidToken);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        await LockFamilyAsync(session.FamilyId);

        // Volvemos a consultar el token dentro de la
        // transacción y después de adquirir el bloqueo.
        session = await _context.RefreshSessions
            .AsNoTracking()
            .FirstAsync(s => s.Id == session.Id);

        var now = DateTime.UtcNow;

        // Una revocación de la familia tiene prioridad
        // sobre cualquier intento de renovación.
        var familyRevoked = await _context.RefreshSessions
            .AsNoTracking()
            .AnyAsync(s =>
                s.FamilyId == session.FamilyId &&
                (s.RevocationReason == "Logout" ||
                 s.RevocationReason == "ReuseDetected"));

        if (familyRevoked)
        {
            return new RefreshRotationResult(
                RefreshRotationStatus.LoggedOut);
        }

        if (session.ExpiresAt <= now)
        {
            return new RefreshRotationResult(
                RefreshRotationStatus.ExpiredToken);
        }

        if (session.RevocationReason == "Rotated")
        {
            return HandleRotatedToken(session);
        }

        if (session.RevokedAt != null)
        {
            return new RefreshRotationResult(
                RefreshRotationStatus.InvalidToken);
        }

        // Solo se permite consumir el token una vez.
        var affectedRows = await _context.RefreshSessions
            .Where(s =>
                s.Id == session.Id &&
                s.RevokedAt == null &&
                s.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.RevokedAt, now)
                .SetProperty(
                    s => s.RevocationReason,
                    "Rotated")
                .SetProperty(s => s.LastUsedAt, now));

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync();

            var updatedSession =
                await _context.RefreshSessions
                    .AsNoTracking()
                    .FirstAsync(s => s.Id == session.Id);

            if (updatedSession.RevocationReason == "Rotated")
            {
                return HandleRotatedToken(updatedSession);
            }

            if (updatedSession.RevocationReason == "Logout" ||
                updatedSession.RevocationReason == "ReuseDetected")
            {
                return new RefreshRotationResult(
                    RefreshRotationStatus.LoggedOut);
            }

            return new RefreshRotationResult(
                RefreshRotationStatus.InvalidToken);
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

    private static RefreshRotationResult HandleRotatedToken(
        RefreshSession session)
    {
        // Un token rotado no puede volver a utilizarse.
        // No revocamos automáticamente toda la familia,
        // porque podría tratarse de una petición
        // concurrente o de una pestaña suspendida.
        return new RefreshRotationResult(
            RefreshRotationStatus.AlreadyRotated,
            FamilyId: session.FamilyId);
    }

    public async Task<int> RevokeFamilyAsync(Guid familyId)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        await LockFamilyAsync(familyId);

        var now = DateTime.UtcNow;

        var affectedRows = await _context.RefreshSessions
            .Where(s =>
                s.FamilyId == familyId &&
                s.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.RevokedAt, now)
                .SetProperty(
                    s => s.RevocationReason,
                    "ReuseDetected"));

        await transaction.CommitAsync();

        return affectedRows;
    }

    public async Task<bool> LogoutFamilyAsync(
        string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        var tokenHash = HashToken(refreshToken);

        // También permite cerrar una familia utilizando
        // un token que ya fue rotado.
        var session = await _context.RefreshSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.TokenHash == tokenHash);

        if (session == null)
            return false;

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        await LockFamilyAsync(session.FamilyId);

        var now = DateTime.UtcNow;

        var affectedRows = await _context.RefreshSessions
            .Where(s =>
                s.FamilyId == session.FamilyId &&
                s.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.RevokedAt, now)
                .SetProperty(
                    s => s.RevocationReason,
                    "Logout"));

        await transaction.CommitAsync();

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
                .SetProperty(
                    s => s.RevocationReason,
                    "Logout"));

        return affectedRows == 1;
    }

    private async Task LockFamilyAsync(Guid familyId)
    {
        // Los bloqueos transaccionales se utilizan
        // únicamente con PostgreSQL.
        if (!_context.Database.IsNpgsql())
            return;

        var transaction =
            _context.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "El bloqueo requiere una transacción activa.");

        var connection =
            _context.Database.GetDbConnection();

        await using var command =
            connection.CreateCommand();

        command.Transaction =
            transaction.GetDbTransaction();

        // PostgreSQL libera automáticamente este bloqueo
        // cuando finaliza la transacción.
        command.CommandText =
            "SELECT pg_advisory_xact_lock(" +
            "hashtextextended(@familyId, 0))";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "familyId";
        parameter.Value = familyId.ToString();

        command.Parameters.Add(parameter);

        await command.ExecuteNonQueryAsync();
    }
}
