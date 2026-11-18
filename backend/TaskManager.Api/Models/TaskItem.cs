namespace TaskManager.Api.Models;

public class TaskItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsCompleted { get; set; } = false;

    public string Status { get; set; } = TaskWorkflowStatus.Pending;

    public string Priority { get; set; } = TaskPriority.Medium;

    public string Category { get; set; } = TaskCategory.Other;

    // Solo fecha: PostgreSQL date, JSON "YYYY-MM-DD".
    public DateOnly? DueDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Propietario: se conserva por compatibilidad.
    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public ApplicationUser? CreatedByUser { get; set; }

    public string AssignedToUserId { get; set; } = string.Empty;

    public ApplicationUser? AssignedToUser { get; set; }
}
