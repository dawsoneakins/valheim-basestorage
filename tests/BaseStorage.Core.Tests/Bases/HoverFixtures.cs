namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Linq;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Snapshots;
using Xunit;

/// <summary>
/// The S-E002-005 hover worlds as literals (ADR-GWIN-033). Every bench has build range 20 and is 15 m from the next one on X, so a
/// chain of six is connected. Chests are 4 wide and 2 high with no contents. Expected lines in the tests are copied from
/// ui-strings-contract.md 3.2 and 5.3 and are never computed from the formatter.
/// </summary>
/// <remarks>
/// W1 is one bench (id 17 at 0,0,0). W2 adds id 18 at (15,0,0). W6 is ids 17..22 at x = 0, 15, 30, 45, 60, 75.
/// Chests c1 (42, at 5,0,0) and c2 (43, at 8,0,0) are PlayerBuilt. far (44, at 500,0,500) is outside every base. The contract 8 kinds
/// are placed within 20 m of bench 17: cart (45, PlayerBuilt|Vehicle), grave (46, Tombstone), erik (47, PlayerBuilt|AccessDenied) and a
/// dungeon chest (48, None). Methods return fresh arrays, so no test changes a shared fixture.
/// </remarks>
internal static class HoverFixtures
{
    private const long UserId = 3141592653L;
    private const float BenchRange = 20f;

    /// <summary>The options of the hover tests: a 10 s window and a cap of 64.</summary>
    /// <returns>The options.</returns>
    internal static BaseCacheOptions Options() => new BaseCacheOptions(10, 64);

    /// <summary>The station id for a serial number, under user id 3141592653.</summary>
    /// <param name="serial">The per-peer serial.</param>
    /// <returns>The station id.</returns>
    internal static StationId StationKey(uint serial) => new StationId(UserId, serial);

    /// <summary>The container id for a serial number, under user id 3141592653.</summary>
    /// <param name="serial">The per-peer serial.</param>
    /// <returns>The container id.</returns>
    internal static ContainerId ContainerKey(uint serial) => new ContainerId(UserId, serial);

    /// <summary>The bench at a chain index: id 17 + index, at (15 * index, 0, 0), build range 20.</summary>
    /// <param name="index">The zero-based index in the chain.</param>
    /// <returns>A fresh bench.</returns>
    internal static StationSnapshot Bench(int index) => Station(17u + (uint)index, 15f * index, 0f, BenchRange);

    /// <summary>The first <paramref name="count"/> benches of the chain: W1 is 1, W2 is 2, W6 is 6.</summary>
    /// <param name="count">How many benches to return.</param>
    /// <returns>A fresh array.</returns>
    internal static StationSnapshot[] Benches(int count)
    {
        var stations = new StationSnapshot[count];
        for (var i = 0; i < count; i++)
        {
            stations[i] = Bench(i);
        }

        return stations;
    }

    /// <summary>A station at a position with a build range.</summary>
    /// <param name="serial">The per-peer serial.</param>
    /// <param name="x">The X position.</param>
    /// <param name="z">The Z position.</param>
    /// <param name="buildRange">The build range; 0 makes the station ineligible.</param>
    /// <returns>A fresh station.</returns>
    internal static StationSnapshot Station(uint serial, float x, float z, float buildRange) =>
        new StationSnapshot(StationKey(serial), new WorldPosition(x, 0f, z), buildRange);

    /// <summary>A chest at a position with flags.</summary>
    /// <param name="serial">The per-peer serial.</param>
    /// <param name="x">The X position.</param>
    /// <param name="y">The Y position.</param>
    /// <param name="z">The Z position.</param>
    /// <param name="flags">The flags; PlayerBuilt by default.</param>
    /// <returns>A fresh container.</returns>
    internal static ContainerSnapshot Chest(uint serial, float x, float y, float z, ContainerFlags flags = ContainerFlags.PlayerBuilt) =>
        new ContainerSnapshot(ContainerKey(serial), new WorldPosition(x, y, z), 4, 2, Array.Empty<ItemStackSnapshot>(), flags);

    /// <summary>Chest c1 (42) at (5,0,0), PlayerBuilt.</summary>
    /// <returns>A fresh container.</returns>
    internal static ContainerSnapshot C1() => Chest(42u, 5f, 0f, 0f);

    /// <summary>Chest c2 (43) at (8,0,0), PlayerBuilt.</summary>
    /// <returns>A fresh container.</returns>
    internal static ContainerSnapshot C2() => Chest(43u, 8f, 0f, 0f);

    /// <summary>Chest far (44) at (500,0,500), PlayerBuilt; outside every base.</summary>
    /// <returns>A fresh container.</returns>
    internal static ContainerSnapshot Far() => Chest(44u, 500f, 0f, 500f);

    /// <summary>The cart (45): PlayerBuilt and Vehicle, within 20 m of bench 17.</summary>
    /// <returns>A fresh container.</returns>
    internal static ContainerSnapshot Cart() => Chest(45u, -4f, 0f, 0f, ContainerFlags.PlayerBuilt | ContainerFlags.Vehicle);

    /// <summary>The grave (46): Tombstone, within 20 m of bench 17.</summary>
    /// <returns>A fresh container.</returns>
    internal static ContainerSnapshot Grave() => Chest(46u, -6f, 0f, 0f, ContainerFlags.Tombstone);

    /// <summary>The locked chest of another player (47): PlayerBuilt and AccessDenied, within 20 m of bench 17.</summary>
    /// <returns>A fresh container.</returns>
    internal static ContainerSnapshot Erik() => Chest(47u, 10f, 0f, 0f, ContainerFlags.PlayerBuilt | ContainerFlags.AccessDenied);

    /// <summary>The dungeon chest (48): no flags, within 20 m of bench 17.</summary>
    /// <returns>A fresh container.</returns>
    internal static ContainerSnapshot Dungeon() => Chest(48u, 0f, 0f, 8f, ContainerFlags.None);

    /// <summary>The chests that a station-count row needs: c1 and c2 for one and two, and generated chests 100.. for more.</summary>
    /// <param name="count">The number of chests.</param>
    /// <returns>A fresh array.</returns>
    internal static ContainerSnapshot[] ChestsForCount(int count)
    {
        if (count <= 2)
        {
            return new[] { C1(), C2() }.Take(count).ToArray();
        }

        var chests = new ContainerSnapshot[count];
        for (var i = 0; i < count; i++)
        {
            chests[i] = Chest(100u + (uint)i, 2f, 0f, 2f);
        }

        return chests;
    }

    /// <summary>A snapshot source that holds the given stations and containers, with its counters at zero.</summary>
    /// <param name="stations">The stations.</param>
    /// <param name="containers">The containers.</param>
    /// <returns>A new counting source.</returns>
    internal static CountingSnapshotSource Source(StationSnapshot[] stations, params ContainerSnapshot[] containers)
    {
        var source = new CountingSnapshotSource();
        source.Stations.AddRange(stations);
        source.Containers.AddRange(containers);
        return source;
    }

    /// <summary>The base of a chest, resolved from the chest's position.</summary>
    /// <param name="stations">The stations of the world.</param>
    /// <param name="containers">The containers of the world.</param>
    /// <param name="chest">The chest whose position starts the lookup.</param>
    /// <param name="maxBaseStations">The station cap.</param>
    /// <returns>The resolution.</returns>
    internal static BaseResolution FromChest(StationSnapshot[] stations, ContainerSnapshot[] containers, ContainerSnapshot chest, int maxBaseStations = 64) =>
        BaseResolver.Resolve(stations, containers, BaseStart.FromPosition(chest.Position), maxBaseStations);

    /// <summary>The base of a station, resolved from the station's id.</summary>
    /// <param name="stations">The stations of the world.</param>
    /// <param name="containers">The containers of the world.</param>
    /// <param name="serial">The serial of the station to start from.</param>
    /// <param name="maxBaseStations">The station cap.</param>
    /// <returns>The resolution.</returns>
    internal static BaseResolution FromStation(StationSnapshot[] stations, ContainerSnapshot[] containers, uint serial, int maxBaseStations = 64) =>
        BaseResolver.Resolve(stations, containers, BaseStart.FromStation(StationKey(serial)), maxBaseStations);

    /// <summary>Asserts one named row of a theory: the expected text first, then the actual text, with the row name in the failure message.</summary>
    /// <param name="row">The row name from the theory data.</param>
    /// <param name="expected">The literal expected from the contract.</param>
    /// <param name="actual">The text under test.</param>
    internal static void AssertRow(string row, string expected, string actual)
    {
        Assert.True(string.Equals(expected, actual, StringComparison.Ordinal), $"[{row}] expected '{expected}' but was '{actual}'.");
    }

    /// <summary>Asserts that a text is one line: no CR, LF, U+0085, U+2028 or U+2029, and no leading or trailing whitespace.</summary>
    /// <param name="line">The text to check.</param>
    internal static void AssertSingleLine(string line)
    {
        char[] breaks = { '\r', '\n', (char)0x85, (char)0x2028, (char)0x2029 };
        Assert.Equal(-1, line.IndexOfAny(breaks));
        Assert.Equal(line.Trim(), line);
    }
}
