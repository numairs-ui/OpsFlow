using Microsoft.Extensions.Configuration;
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
            RegionId: meta?.GetValueOrDefault("region_id")?.ToString()
        );
    }

    public async Task<string> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var admin = supabase.AdminAuth(_serviceKey);
        var attrs = new AdminUserAttributes
        {
            Email = request.Email,
            Password = request.Password,
            UserMetadata = new Dictionary<string, object>
            {
                ["tenant_id"] = request.TenantId,
                ["role"] = request.Role,
            },
        };

        if (request.StoreId is not null) attrs.UserMetadata["store_id"] = request.StoreId;
        if (request.RegionId is not null) attrs.UserMetadata["region_id"] = request.RegionId;

        var user = await admin.CreateUser(attrs);
        return user?.Id ?? throw new InvalidOperationException("Supabase returned null user after CreateUser.");
    }

    public async Task ResetPasswordAsync(string userId, string newPassword, CancellationToken ct = default)
    {
        var admin = supabase.AdminAuth(_serviceKey);
        await admin.UpdateUserById(userId, new AdminUserAttributes { Password = newPassword });
    }
}
