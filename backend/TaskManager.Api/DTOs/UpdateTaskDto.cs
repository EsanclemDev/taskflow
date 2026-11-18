using System.ComponentModel.DataAnnotations;

namespace TaskManager.Api.DTOs;

public class UpdateTaskDto
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    // Compatibilidad con el frontend anterior.
    public bool? IsCompleted { get; set; }

    [RegularExpression(
        @"^(pending|in_progress|completed)$",
        ErrorMessage = "El estado de la tarea no es válido.")]
    public string? Status { get; set; }

    [RegularExpression(
        @"^(low|medium|high|urgent)$",
        ErrorMessage = "La prioridad de la tarea no es válida.")]
    public string? Priority { get; set; }

    [RegularExpression(
        @"^(frontend|backend|database|documentation|other)$",
        ErrorMessage = "La categoría de la tarea no es válida.")]
    public string? Category { get; set; }

    // Si se omite, conserva la fecha existente.
    public DateOnly? DueDate { get; set; }

    // Enviar true para borrar expresamente la fecha límite.
    public bool ClearDueDate { get; set; }
}
