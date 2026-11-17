
namespace TaskManager.Api.Models;

public enum RefreshRotationStatus
{
    Success,
    InvalidToken,
    ExpiredToken,
    LoggedOut,
    AlreadyRotated,
    ReuseDetected
}

public record RefreshRotationResult(
    RefreshRotationStatus Status,
    string? UserId = null,
    string? RefreshToken = null,
    Guid? FamilyId = null
);
