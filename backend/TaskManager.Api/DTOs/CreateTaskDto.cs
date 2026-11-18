using System.ComponentModel.DataAnnotations;

namespace TaskManager.Api.DTOs;

public class CreateTaskDto
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [RegularExpression(
        @"^(low|medium|high|urgent)$",
        ErrorMessage = "La prioridad de la tarea no es válida.")]
    public string? Priority { get; set; }

    [RegularExpression(
        @"^(frontend|backend|database|documentation|other)$",
        ErrorMessage = "La categoría de la tarea no es válida.")]
    public string? Category { get; set; }

    public DateOnly? DueDate { get; set; }
}
