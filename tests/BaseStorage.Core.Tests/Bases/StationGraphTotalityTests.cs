namespace BaseStorage.Core.Tests.Bases;

using System;
using BaseStorage.Core.Bases;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.StationGraphFixtures;

/// <summary>AC-1: totality, duplicate ids, unknown or none starts, and argument validation (contract 7.3, fixtures G12-G14).</summary>
public sealed class StationGraphTotalityTests
{
    // GW-U-121 (AC-1)
    [Theory]
    [InlineData("G12a")]
    [InlineData("G12d")]
    public void DuplicatePositionsTerminateAndDuplicateIdsCountOnce(string id)
    {
        AssertRow(id);
    }

    // GW-U-121 (AC-1): first occurrence wins, in input order.
    [Fact]
    public void FirstOccurrenceWinsWhenItIsTheNearStation()
    {
        AssertRow("G12b");

        Assert.Equal(S(1, 0f), Run("G12b").Stations[0]);
    }

    // GW-U-121 (AC-1): first occurrence wins, other input order.
    [Fact]
    public void FirstOccurrenceWinsWhenItIsTheFarStation()
    {
        AssertRow("G12c");

        Assert.Equal(S(1, 100f), Run("G12c").Stations[0]);
    }

    // GW-U-121 (AC-1): start id not present, or the none id, or an empty list.
    [Theory]
    [InlineData("G13a")]
    [InlineData("G13b")]
    [InlineData("G13c")]
    public void UnknownNoneOrEmptyStartGivesAnEmptyUncappedResult(string id)
    {
        StationGraphResult result = Run(id);

        Assert.Empty(result.Stations);
        Assert.False(result.IsCapped);
    }

    // GW-U-121 (AC-1): a null list is rejected with the exact exception type and parameter name.
    [Fact]
    public void NullListThrowsArgumentNullExceptionForStations()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            StationGraph.FindBase(null!, Id(1), DefaultMax));

        Assert.Equal("stations", ex.ParamName);
    }

    // GW-U-121 (AC-1): a null element is rejected before start is looked at (start 99 is not in the list).
    [Fact]
    public void NullElementThrowsArgumentExceptionForStationsWhateverTheStart()
    {
        StationSnapshot[] stations = [S(1, 0f), null!];

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            StationGraph.FindBase(stations, Id(99), DefaultMax));

        Assert.Equal("stations", ex.ParamName);
        Assert.IsNotType<ArgumentNullException>(ex);
    }
}
