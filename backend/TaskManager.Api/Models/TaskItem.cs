
namespace TaskManager.Api.Models;

public class TaskItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Compatibilidad con Dashboard y Estadísticas.
    // Se mantiene sincronizado con Status.
    public bool IsCompleted { get; set; } = false;

    // Estado real del Kanban.
    public string Status { get; set; } =
        TaskWorkflowStatus.Pending;

    public DateTime CreatedAt { get; set; } =
        DateTime.UtcNow;

    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }
}
