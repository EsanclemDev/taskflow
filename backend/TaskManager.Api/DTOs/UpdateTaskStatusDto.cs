
using System.ComponentModel.DataAnnotations;

namespace TaskManager.Api.DTOs;

public class UpdateTaskStatusDto
{
    [Required]
    [RegularExpression(
        @"^(pending|in_progress|completed)$",
        ErrorMessage = "El estado de la tarea no es válido."
    )]
    public string Status { get; set; } = string.Empty;
}
