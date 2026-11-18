using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services;

public class DemoAccountService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RefreshTokenService _refreshTokenService;

    public DemoAccountService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RefreshTokenService refreshTokenService)
    {
        _context = context;
        _userManager = userManager;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<(ApplicationUser User, string RefreshToken, Guid FamilyId)>
        CreateAsync()
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        // Dominio reservado: no se envían correos ni se conocen contraseñas.
        var email = $"demo-{Guid.NewGuid():N}@taskflow.invalid";
        var now = DateTime.UtcNow;
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            IsDemo = true,
            DemoExpiresAt = now.AddHours(24),
            CreatedAt = now
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                "No se ha podido crear la cuenta de demostración.");

        _context.Tasks.AddRange(BuildSampleTasks(user.Id, now));
        await _context.SaveChangesAsync();

        var session = await _refreshTokenService
            .CreateSessionWithFamilyAsync(user);

        await transaction.CommitAsync();
        return (user, session.RefreshToken, session.FamilyId);
    }

    public async Task<bool> ResetAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsDemo ||
            user.DemoExpiresAt == null ||
            user.DemoExpiresAt <= DateTime.UtcNow)
            return false;

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        // Solo las tareas del usuario demo actual; nunca las de otros usuarios.
        await _context.Tasks
            .Where(t => t.UserId == userId)
            .ExecuteDeleteAsync();

        _context.Tasks.AddRange(BuildSampleTasks(userId, DateTime.UtcNow));
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expiredIds = await _context.Users.AsNoTracking()
            .Where(u => u.IsDemo && u.DemoExpiresAt != null &&
                u.DemoExpiresAt <= now)
            .OrderBy(u => u.DemoExpiresAt)
            .Select(u => u.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        var deleted = 0;
        foreach (var userId in expiredIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            // Las relaciones de creador y asignado son NoAction.
            // Eliminamos primero las tareas y sesiones propias.
            await _context.Tasks.Where(t => t.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
            await _context.RefreshSessions.Where(s => s.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && user.IsDemo &&
                user.DemoExpiresAt <= DateTime.UtcNow)
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                    throw new InvalidOperationException(
                        "No se ha podido eliminar una cuenta demo caducada.");
                deleted++;
            }

            await transaction.CommitAsync(cancellationToken);
        }
        return deleted;
    }

    private static IEnumerable<TaskItem> BuildSampleTasks(string userId, DateTime now)
    {
        // Estado, prioridad y categoría compatibles con el Kanban existente.
        var samples = new (string Title, string Description, string Status,
            string Priority, string Category, int? DueInDays, int CreatedDaysAgo)[]
        {
            ("Diseñar página de inicio", "Definir estructura y componentes del dashboard.",
                TaskWorkflowStatus.Pending, TaskPriority.High, TaskCategory.Frontend, 3, 2),
            ("Preparar pruebas de API", "Cubrir creación y edición de tareas.",
                TaskWorkflowStatus.Pending, TaskPriority.Medium, TaskCategory.Backend, 5, 3),
            ("Revisar índices PostgreSQL", "Analizar las consultas de tareas por usuario.",
                TaskWorkflowStatus.Pending, TaskPriority.Low, TaskCategory.Database, 10, 1),
            ("Actualizar README", "Documentar instalación y variables de entorno.",
                TaskWorkflowStatus.Pending, TaskPriority.Low, TaskCategory.Documentation, null, 4),
            ("Corregir validación de formularios", "Mostrar errores accesibles en los modales.",
                "in_progress", TaskPriority.Urgent, TaskCategory.Frontend, -1, 6),
            ("Optimizar consultas de tareas", "Reducir lecturas innecesarias del servidor.",
                "in_progress", TaskPriority.High, TaskCategory.Backend, 2, 7),
            ("Mejorar versión móvil", "Revisar el Kanban en pantallas pequeñas.",
                "in_progress", TaskPriority.Medium, TaskCategory.Frontend, 4, 5),
            ("Escribir guía de uso", "Explicar filtros, prioridades y fechas límite.",
                "in_progress", TaskPriority.Low, TaskCategory.Documentation, 7, 3),
            ("Crear autenticación", "Configurar inicio de sesión y registro.",
                TaskWorkflowStatus.Completed, TaskPriority.High, TaskCategory.Backend, -8, 14),
            ("Configurar base de datos", "Crear esquema y migraciones iniciales.",
                TaskWorkflowStatus.Completed, TaskPriority.Medium, TaskCategory.Database, -10, 16),
            ("Implementar tablero Kanban", "Añadir columnas y arrastrar tareas.",
                TaskWorkflowStatus.Completed, TaskPriority.High, TaskCategory.Frontend, -3, 11),
            ("Añadir estadísticas", "Mostrar métricas y evolución del trabajo.",
                TaskWorkflowStatus.Completed, TaskPriority.Medium, TaskCategory.Other, null, 9)
        };

        return samples.Select(s =>
        {
            var createdAt = now.AddDays(-s.CreatedDaysAgo);
            return new TaskItem
            {
                Title = s.Title,
                Description = s.Description,
                Status = s.Status,
                IsCompleted = s.Status == TaskWorkflowStatus.Completed,
                Priority = s.Priority,
                Category = s.Category,
                DueDate = s.DueInDays.HasValue
                    ? DateOnly.FromDateTime(now.AddDays(s.DueInDays.Value))
                    : null,
                CreatedAt = createdAt,
                UpdatedAt = createdAt.AddHours(2),
                UserId = userId,
                CreatedByUserId = userId,
                AssignedToUserId = userId
            };
        });
    }
}
