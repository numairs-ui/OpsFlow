using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OpsFlow.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OpsFlow.Tests.Integration.Health;

public sealed class HealthEndpointTests : IClassFixture<OpsFlowWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(OpsFlowWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_Returns200WithOkStatus()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body!.Status.Should().Be("ok");
    }

    private sealed record HealthResponse(string Status, string Environment, DateTimeOffset Timestamp);
}
