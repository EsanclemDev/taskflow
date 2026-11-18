
using Microsoft.AspNetCore.Identity;

namespace TaskManager.Api.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Cuenta temporal de demostración (sin contraseña).
    public bool IsDemo { get; set; } = false;

    public DateTime? DemoExpiresAt { get; set; }
}
