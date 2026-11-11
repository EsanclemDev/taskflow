
using System.ComponentModel.DataAnnotations;

namespace TaskManager.Api.DTOs;

public class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
