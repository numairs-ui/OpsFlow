using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OpsFlow.Domain.Interfaces;

namespace OpsFlow.Api.Services;

internal sealed class TokenService(IConfiguration configuration, IWebHostEnvironment env)
{
    internal const string RefreshCookieName = "refresh_token";
    private const int AccessTokenMinutes = 15;
    private const int RefreshTokenDays = 30;

    public string MintAccessToken(AuthResult auth)
    {
        var secret = configuration["JWT_SECRET"]!;
        var issuer = configuration["JWT_ISSUER"] ?? "OpsFlow";
        var audience = configuration["JWT_AUDIENCE"] ?? "OpsFlow.API";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, auth.UserId),
            new("tenantId", auth.TenantId),
            new("role", auth.Role),
        };
        if (auth.StoreId is not null) claims.Add(new("storeId", auth.StoreId));
        if (auth.RegionId is not null) claims.Add(new("regionId", auth.RegionId));

        var token = new JwtSecurityToken(
            issuer, audience, claims,
            expires: DateTime.UtcNow.AddMinutes(AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string Raw, string Hash, DateTimeOffset ExpiresAt) GenerateRefreshToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var hash = HashToken(raw);
        return (raw, hash, DateTimeOffset.UtcNow.AddDays(RefreshTokenDays));
    }

    public string HashToken(string raw) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    public void SetRefreshCookie(HttpResponse response, string tenantId, string rawToken, DateTimeOffset expiresAt)
    {
        // Cookie value = "{tenantId}:{rawToken}" so we can resolve the tenant DB on refresh
        var isSecure = !env.IsDevelopment();
        response.Cookies.Append(RefreshCookieName, $"{tenantId}:{rawToken}", new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = isSecure ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = expiresAt,
        });
    }

    public (string TenantId, string RawToken)? ParseRefreshCookie(HttpRequest request)
    {
        var value = request.Cookies[RefreshCookieName];
        if (value is null) return null;
        var idx = value.IndexOf(':');
        if (idx < 0) return null;
        return (value[..idx], value[(idx + 1)..]);
    }

    public void ClearRefreshCookie(HttpResponse response) =>
        response.Cookies.Delete(RefreshCookieName);
}
