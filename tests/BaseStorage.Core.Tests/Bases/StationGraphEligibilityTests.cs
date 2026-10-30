namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Linq;

/// <summary>AC-1: eligibility (build range above 0) and the either-range rule, from contract 7.3 and fixtures G6 and G7.</summary>
public sealed class StationGraphEligibilityTests
{
    // GW-U-118 (AC-1)
    [Theory]
    [InlineData("G6a")]
    [InlineData("G6b")]
    [InlineData("G6c")]
    [InlineData("G7a")]
    [InlineData("G7b")]
    [InlineData("G7c")]
    public void EligibilityAndEitherRangeRuleMatchTheContractRows(string id)
    {
        StationGraphFixtures.AssertRow(id);
    }

    // GW-U-118 (AC-1)
    [Fact]
    public void RangeZeroStationIsNeverIncludedAndNeverBridges()
    {
        // G7a: station 2 has range 0 and sits between 1 and 3, so it must not connect them.
        var ids = StationGraphFixtures.Run("G7a").Stations.Select(s => s.Id.Id).ToArray();

        Assert.DoesNotContain(2u, ids);
        Assert.DoesNotContain(3u, ids);
    }
}
