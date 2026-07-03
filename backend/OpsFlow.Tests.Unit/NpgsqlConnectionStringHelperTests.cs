using FluentAssertions;
using OpsFlow.Infrastructure;
using Xunit;

namespace OpsFlow.Tests.Unit;

public sealed class NpgsqlConnectionStringHelperTests
{
    [Fact]
    public void Host_already_an_IP_literal_is_left_unchanged()
    {
        const string connectionString = "Host=127.0.0.1;Database=postgres;Username=postgres;Password=x;Port=5432";

        var result = NpgsqlConnectionStringHelper.PreferIPv4(connectionString);

        result.Should().Contain("Host=127.0.0.1");
    }

    [Fact]
    public void Empty_host_is_left_unchanged()
    {
        const string connectionString = "Database=postgres;Username=postgres;Password=x;Port=5432";

        var result = NpgsqlConnectionStringHelper.PreferIPv4(connectionString);

        result.Should().NotContain("Host=");
    }
}
