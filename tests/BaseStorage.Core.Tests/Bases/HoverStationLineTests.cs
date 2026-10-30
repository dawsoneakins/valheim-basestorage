namespace BaseStorage.Core.Tests.Bases;

using System;
using BaseStorage.Core.Bases.Inspection;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.HoverFixtures;

/// <summary>AC-3 (ADR-GWIN-032, ADR-GWIN-033): the station line counts the chests of its base, and reads only through the cache.</summary>
public sealed class HoverStationLineTests
{
    // GW-U-542 (AC-3): the chest count with singular and plural, and nothing for a station that is in no base.
    [Theory]
    [InlineData("no chests", 0, "[Base: 0 chests]")]
    [InlineData("one chest", 1, "[Base: 1 chest]")]
    [InlineData("two chests", 2, "[Base: 2 chests]")]
    [InlineData("41 generated chests", 41, "[Base: 41 chests]")]
    public void FormatStationGivesTheChestCountWithSingularAndPlural(string row, int chests, string expected)
    {
        BaseResolution resolution = FromStation(Benches(1), ChestsForCount(chests), 17u);

        string actual = HoverInspector.FormatStation(resolution);

        AssertRow(row, expected, actual);
        AssertSingleLine(actual);
    }

    // GW-U-542 (AC-3): a station in no base gets no line.
    [Fact]
    public void FormatStationOfANoBaseResultIsTheEmptyString()
    {
        Assert.Equal(string.Empty, HoverInspector.FormatStation(BaseResolution.NotInBase));
    }

    // GW-U-543 (AC-3): only tombstones and vehicles are excluded from the count.
    [Fact]
    public void CountChestsExcludesOnlyTombstonesAndVehicles()
    {
        ContainerSnapshot c1 = C1();
        ContainerSnapshot[] containers = { c1, Cart(), Grave(), Erik(), Dungeon() };
        BaseResolution resolution = FromStation(Benches(1), containers, 17u);

        Assert.Equal(3, HoverInspector.CountChests(resolution));
        Assert.Equal("[Base: 3 chests]", HoverInspector.FormatStation(resolution));
    }

    // GW-U-543 (AC-3): a vehicle without PlayerBuilt is still a vehicle, so it is still excluded.
    [Fact]
    public void AVehicleWithoutPlayerBuiltIsStillExcluded()
    {
        ContainerSnapshot vehicle = Chest(60u, 3f, 0f, 0f, ContainerFlags.Vehicle);
        BaseResolution resolution = FromStation(Benches(1), new[] { C1(), vehicle }, 17u);

        Assert.Equal(1, HoverInspector.CountChests(resolution));
    }

    // GW-U-543 (AC-3): a no-base result has no chests.
    [Fact]
    public void CountChestsOfANoBaseResultIsZero()
    {
        Assert.Equal(0, HoverInspector.CountChests(BaseResolution.NotInBase));
    }

    // GW-U-544 (AC-3): StationLine over a cache. Two stations of one base give the same line.
    [Fact]
    public void StationLineOverTheCacheCountsTheChestsOfTheBase()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        CountingSnapshotSource source = Source(Benches(2), C1(), C2());
        var inspector = new HoverInspector(new BaseCache(source, clock));

        string first = inspector.StationLine(StationKey(17), Options());
        string second = inspector.StationLine(StationKey(18), Options());

        Assert.Equal("[Base: 2 chests]", first);
        Assert.Equal("[Base: 2 chests]", second);
    }

    // GW-U-544 (AC-3): an unknown station gets no line.
    [Fact]
    public void AnUnknownStationGivesTheEmptyString()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        CountingSnapshotSource source = Source(Benches(2), C1(), C2());
        var inspector = new HoverInspector(new BaseCache(source, clock));

        Assert.Equal(string.Empty, inspector.StationLine(StationKey(99), Options()));
    }

    // GW-U-544 (AC-3): the none id returns at once, without reading the world.
    [Fact]
    public void TheNoneStationGivesTheEmptyStringWithoutReadingTheWorld()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        CountingSnapshotSource source = Source(Benches(2), C1(), C2());
        var inspector = new HoverInspector(new BaseCache(source, clock));

        string line = null!;
        Exception? error = Record.Exception(() => line = inspector.StationLine(default, Options()));

        Assert.Null(error);
        Assert.Equal(string.Empty, line);
        Assert.Equal(0, source.StationReads);
    }

    // GW-U-544 (AC-3): a station with build range 0 is not a base seed, so it gets no line.
    [Fact]
    public void AStationWithZeroBuildRangeGivesTheEmptyString()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        CountingSnapshotSource source = Source(new[] { Station(17u, 0f, 0f, 0f) }, C1());
        var inspector = new HoverInspector(new BaseCache(source, clock));

        Assert.Equal(string.Empty, inspector.StationLine(StationKey(17), Options()));
    }
}
