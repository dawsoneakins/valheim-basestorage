namespace BaseStorage.Core.Tests.Bases;

using System;
using BaseStorage.Core.Bases.Inspection;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.HoverFixtures;

/// <summary>AC-1 (ADR-GWIN-032, ADR-GWIN-033): the chest base segment comes from the resolution, and ChestLine answers over the cache.</summary>
public sealed class HoverChestLineTests
{
    // GW-U-534 (AC-1): the base segment for a chest in one, two, six and capped bases.
    [Theory]
    [InlineData("W1 gives one station", 1, 64, "[In base: 1 station]")]
    [InlineData("W2 gives two stations", 2, 64, "[In base: 2 stations]")]
    [InlineData("W6 gives six stations", 6, 64, "[In base: 6 stations]")]
    [InlineData("W6 with cap 2 gives the capped count", 6, 2, "[In base: 2 stations]")]
    public void AChestInABaseGivesTheStationCount(string row, int benches, int cap, string expected)
    {
        ContainerSnapshot c1 = C1();
        BaseResolution resolution = FromChest(Benches(benches), new[] { c1 }, c1, cap);

        string actual = HoverInspector.FormatChest(resolution, null);

        AssertRow(row, expected, actual);
        AssertSingleLine(actual);
    }

    // GW-U-534 (AC-1): a no-base result and a chest far from the stations.
    [Fact]
    public void TheNoBaseResultGivesNotInABase()
    {
        string actual = HoverInspector.FormatChest(BaseResolution.NotInBase, null);

        Assert.Equal("[Not in a base]", actual);
        AssertSingleLine(actual);
    }

    // GW-U-534 (AC-1): a chest far from every station of W2.
    [Fact]
    public void AFarChestOverW2GivesNotInABase()
    {
        ContainerSnapshot far = Far();
        BaseResolution resolution = FromChest(Benches(2), new[] { C1(), far }, far);

        string actual = HoverInspector.FormatChest(resolution, null);

        Assert.Equal("[Not in a base]", actual);
        AssertSingleLine(actual);
    }

    // GW-U-535 (AC-1): ChestLine over a BaseCache with the clock at 100 s.
    [Fact]
    public void ChestLineGivesTheBaseLineForAChestInTheBase()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        ContainerSnapshot c1 = C1();
        CountingSnapshotSource source = Source(Benches(2), c1, Far());
        var inspector = new HoverInspector(new BaseCache(source, clock));

        Assert.Equal("[In base: 2 stations]", inspector.ChestLine(c1.Id, c1.Position, null, Options()));
    }

    // GW-U-535 (AC-1): a far chest over the same cache.
    [Fact]
    public void ChestLineGivesNotInABaseForAFarChest()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        ContainerSnapshot far = Far();
        CountingSnapshotSource source = Source(Benches(2), C1(), far);
        var inspector = new HoverInspector(new BaseCache(source, clock));

        Assert.Equal("[Not in a base]", inspector.ChestLine(far.Id, far.Position, null, Options()));
    }

    // GW-U-535 (AC-1): an empty world has no base for any chest.
    [Fact]
    public void ChestLineOverAnEmptyWorldGivesNotInABase()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        ContainerSnapshot c1 = C1();
        CountingSnapshotSource source = Source(Array.Empty<StationSnapshot>(), c1);
        var inspector = new HoverInspector(new BaseCache(source, clock));

        Assert.Equal("[Not in a base]", inspector.ChestLine(c1.Id, c1.Position, null, Options()));
    }

    // GW-U-535 (AC-1): a container id that is none still resolves by position and does not throw.
    [Fact]
    public void ChestLineWithANoneContainerIdStillResolvesByPosition()
    {
        var clock = new ManualClock { Elapsed = TimeSpan.FromSeconds(100) };
        ContainerSnapshot c1 = C1();
        CountingSnapshotSource source = Source(Benches(2), c1);
        var inspector = new HoverInspector(new BaseCache(source, clock));

        string line = null!;
        Exception? error = Record.Exception(() => line = inspector.ChestLine(default, c1.Position, null, Options()));

        Assert.Null(error);
        Assert.Equal("[In base: 2 stations]", line);
    }
}
