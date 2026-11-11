
namespace TaskManager.Api.Models;

public class RefreshSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Identifica la cadena de renovaciones de una sesión
    public Guid FamilyId { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? RevocationReason { get; set; }

    public DateTime? LastUsedAt { get; set; }
}
