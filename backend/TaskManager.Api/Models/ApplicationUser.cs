
using Microsoft.AspNetCore.Identity;

namespace TaskManager.Api.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
