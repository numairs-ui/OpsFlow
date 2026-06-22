using FluentAssertions;
using System.Net;
using Xunit;

namespace OpsFlow.Tests.Integration.Auth;

/// <summary>
/// Tests the A1 fix: POST /auth/refresh must return 401 (not 500)
/// when the refresh cookie is missing or invalid.
/// </summary>
public sealed class AuthRefreshTests : IClassFixture<OpsFlowWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthRefreshTests(OpsFlowWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Refresh_WithNoCookie_Returns401NotServerError()
    {
        // Before the A1 fix this threw UnhandledException → 500.
        // The global exception handler now catches UnauthorizedAccessException → 401.
        var response = await _client.PostAsync("/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithMalformedCookie_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        request.Headers.Add("Cookie", "refresh_token=not-a-valid-token-format");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
