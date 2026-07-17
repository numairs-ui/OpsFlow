using Microsoft.Extensions.Configuration;
using OpsFlow.Domain.Authorization;
using OpsFlow.Domain.Interfaces;
using Supabase.Gotrue;

namespace OpsFlow.Infrastructure.Supabase;

internal sealed class SupabaseAuthProvider(
    global::Supabase.Client supabase,
    IConfiguration configuration) : IAuthProvider
{
    private readonly string _serviceKey = configuration["SUPABASE_SERVICE_ROLE_KEY"]!;

    public async Task<AuthResult?> AuthenticateAsync(string email, string password, string tenantId, CancellationToken ct = default)
    {
        var session = await supabase.Auth.SignIn(email, password);
        if (session?.User is null) return null;

        var meta = session.User.UserMetadata;
        var userTenantId = meta?.GetValueOrDefault("tenant_id")?.ToString();
        if (userTenantId != tenantId) return null;

        return new AuthResult(
            UserId: session.User.Id!,
            TenantId: tenantId,
            Role: meta?.GetValueOrDefault("role")?.ToString() ?? "store_employee",
            StoreId: meta?.GetValueOrDefault("store_id")?.ToString(),
            RegionIds: ParseRegionIds(meta)
        );
    }

    // Region scope is stored as a comma-separated "region_ids" string (admin: many, supervisor: one),
    // falling back to the legacy single "region_id" key for pre-multi-region accounts.
    private static IReadOnlyList<string> ParseRegionIds(IReadOnlyDictionary<string, object>? meta) =>
        meta is null
            ? []
            : UserRegionScope.Decode(
                meta.GetValueOrDefault("region_ids")?.ToString(),
                meta.GetValueOrDefault("region_id")?.ToString());

    public async Task<string> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var admin = supabase.AdminAuth(_serviceKey);
        var attrs = new AdminUserAttributes
        {
            Email = request.Email,
            Password = request.Password,
            // Accounts are created directly by an admin (not via self-serve signup), so there is
            // no confirmation email to click — without this, Supabase leaves the user unconfirmed
            // and every login attempt fails with "Email not confirmed".
            EmailConfirm = true,
            UserMetadata = new Dictionary<string, object>
            {
                ["tenant_id"] = request.TenantId,
                ["role"] = request.Role,
            },
        };

        if (request.StoreId is not null) attrs.UserMetadata["store_id"] = request.StoreId;
        var csv = UserRegionScope.ToCsv(request.RegionIds);
        if (csv is not null) attrs.UserMetadata["region_ids"] = csv;

        var user = await admin.CreateUser(attrs);
        return user?.Id ?? throw new InvalidOperationException("Supabase returned null user after CreateUser.");
    }

    public async Task UpdateUserAsync(UpdateUserRequest request, CancellationToken ct = default)
    {
        var admin = supabase.AdminAuth(_serviceKey);

        // Send every scope key (null clears) so the result is correct whether Gotrue merges or
        // replaces user_metadata — e.g. switching a manager to a supervisor must drop store_id.
        var metadata = new Dictionary<string, object?>
        {
            ["tenant_id"] = request.TenantId,
            ["role"] = request.Role,
            ["store_id"] = request.StoreId,
            ["region_ids"] = UserRegionScope.ToCsv(request.RegionIds),
            ["region_id"] = null, // retire the legacy single-region key
        };

        await admin.UpdateUserById(request.UserId, new AdminUserAttributes
        {
            // Defensive: an admin editing a user should never leave them stuck unconfirmed.
            EmailConfirm = true,
            UserMetadata = metadata.ToDictionary(kv => kv.Key, kv => kv.Value!),
        });
    }

    public async Task ResetPasswordAsync(string userId, string newPassword, CancellationToken ct = default)
    {
        var admin = supabase.AdminAuth(_serviceKey);
        await admin.UpdateUserById(userId, new AdminUserAttributes { Password = newPassword });
    }

    public async Task<string?> GetEmailAsync(string userId, CancellationToken ct = default)
    {
        var admin = supabase.AdminAuth(_serviceKey);
        var user = await admin.GetUserById(userId);
        return user?.Email;
    }
}
