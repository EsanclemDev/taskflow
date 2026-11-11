
namespace TaskManager.Api.Models;

public record RefreshSessionResult(
    string UserId,
    string RefreshToken
);
