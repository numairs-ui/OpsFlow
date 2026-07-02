using FluentAssertions;
using OpsFlow.Domain.Authorization;
using Xunit;

namespace OpsFlow.Tests.Unit.Authorization;

public sealed class UserRegionScopeTests
{
    private static readonly Guid R1 = Guid.NewGuid();
    private static readonly Guid R2 = Guid.NewGuid();

    [Fact]
    public void Encode_no_regions_is_null_null() =>
        UserRegionScope.Encode([]).Should().Be(((Guid?)null, (string?)null));

    [Fact]
    public void Encode_single_region_fills_the_fk_and_csv() =>
        UserRegionScope.Encode([R1]).Should().Be(((Guid?)R1, R1.ToString()));

    [Fact]
    public void Encode_many_regions_clears_the_fk_and_joins_csv() =>
        UserRegionScope.Encode([R1, R2]).Should().Be(((Guid?)null, $"{R1},{R2}"));

    [Fact]
    public void Decode_prefers_csv() =>
        UserRegionScope.Decode($"{R1},{R2}", null).Should().BeEquivalentTo([R1.ToString(), R2.ToString()]);

    [Fact]
    public void Decode_falls_back_to_single_when_csv_empty() =>
        UserRegionScope.Decode(null, R1.ToString()).Should().BeEquivalentTo([R1.ToString()]);

    [Fact]
    public void Decode_empty_when_nothing() =>
        UserRegionScope.Decode(null, null).Should().BeEmpty();

    [Fact]
    public void Encode_then_decode_round_trips_the_set()
    {
        var (_, csv) = UserRegionScope.Encode([R1, R2]);
        UserRegionScope.Decode(csv, null).Should().BeEquivalentTo([R1.ToString(), R2.ToString()]);
    }
}
