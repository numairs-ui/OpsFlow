using FluentAssertions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using OpsFlow.Api.Features.Health.GetStatus;
using Xunit;

namespace OpsFlow.Tests.Unit.Health;

public sealed class GetStatusHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOkStatus()
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns("Development");

        var handler = new GetStatusHandler(env);
        var result = await handler.Handle(new GetStatusQuery(), CancellationToken.None);

        result.Status.Should().Be("ok");
        result.Environment.Should().Be("Development");
        result.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
