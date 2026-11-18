namespace TaskManager.Api.DTOs;

// Respuesta explícita: evita serializar entidades de ASP.NET Identity.
public class TaskResponseDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsCompleted { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public DateOnly? DueDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? UserId { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public string? CreatedByUserName { get; set; }

    public string AssignedToUserId { get; set; } = string.Empty;

    public string? AssignedToUserName { get; set; }
}
