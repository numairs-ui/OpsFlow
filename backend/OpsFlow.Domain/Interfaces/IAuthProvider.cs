namespace OpsFlow.Domain.Interfaces;

public sealed record AuthResult(string UserId, string TenantId, string Role, string? StoreId, string? RegionId);
public sealed record CreateUserRequest(string Email, string Password, string Role, string TenantId, string? StoreId, string? RegionId);

public interface IAuthProvider
{
    Task<AuthResult?> AuthenticateAsync(string email, string password, string tenantId, CancellationToken ct = default);
    Task<string> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(string userId, string newPassword, CancellationToken ct = default);
}
