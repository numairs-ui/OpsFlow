namespace OpsFlow.Domain.Interfaces;

public sealed record AuthResult(string UserId, string TenantId, string Role, string? StoreId, IReadOnlyList<string> RegionIds);
public sealed record CreateUserRequest(string Email, string Password, string Role, string TenantId, string? StoreId, IReadOnlyList<string> RegionIds);
public sealed record UpdateUserRequest(string UserId, string TenantId, string Role, string? StoreId, IReadOnlyList<string> RegionIds);

public interface IAuthProvider
{
    Task<AuthResult?> AuthenticateAsync(string email, string password, string tenantId, CancellationToken ct = default);
    Task<string> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
    /// <summary>Sync a user's authorization attributes (role, store, region set) into the auth store.</summary>
    Task UpdateUserAsync(UpdateUserRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(string userId, string newPassword, CancellationToken ct = default);
}
