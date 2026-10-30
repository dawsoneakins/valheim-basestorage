namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Snapshots;

/// <summary>
/// A clock that the test moves by hand (ADR-GWIN-025). It starts at 3600 s, as in the contract 3.1 example, so a test can move it
/// backwards without going below zero.
/// </summary>
internal sealed class ManualClock : IClock
{
    /// <summary>Gets or sets the reading. A test sets it lower to model a clock that goes backwards.</summary>
    public TimeSpan Elapsed { get; set; } = TimeSpan.FromSeconds(3600);

    /// <summary>Moves the clock forward by <paramref name="by"/>.</summary>
    /// <param name="by">The time to add.</param>
    public void Advance(TimeSpan by) => Elapsed += by;
}

/// <summary>
/// A snapshot source that counts its reads and records their order (ADR-GWIN-025). A rebuild is seen as one <see cref="StationReads"/>
/// and one <see cref="ContainerReads"/>. The counters rise before <see cref="ThrowOnRead"/> is thrown, so a failed read is counted too.
/// </summary>
internal sealed class CountingSnapshotSource : IBaseSnapshotSource
{
    /// <summary>Gets the mutable list of stations that the next read returns (a copy of it is what the cache keeps).</summary>
    public List<StationSnapshot> Stations { get; } = new List<StationSnapshot>();

    /// <summary>Gets the mutable list of containers that the next read returns.</summary>
    public List<ContainerSnapshot> Containers { get; } = new List<ContainerSnapshot>();

    /// <summary>Gets the number of calls to <see cref="GetStations"/>.</summary>
    public int StationReads { get; private set; }

    /// <summary>Gets the number of calls to <see cref="GetContainers"/>.</summary>
    public int ContainerReads { get; private set; }

    /// <summary>Gets the names of the read methods, in call order.</summary>
    public List<string> CallLog { get; } = new List<string>();

    /// <summary>Gets or sets an exception that both reads throw, after they are counted and logged.</summary>
    public Exception? ThrowOnRead { get; set; }

    /// <summary>Gets or sets a value that makes <see cref="GetStations"/> return null.</summary>
    public bool ReturnNullStations { get; set; }

    /// <summary>Gets or sets a value that makes <see cref="GetContainers"/> return null.</summary>
    public bool ReturnNullContainers { get; set; }

    /// <summary>Counts and logs a station read, then returns the stations or throws <see cref="ThrowOnRead"/>.</summary>
    /// <returns>The current stations, or null when <see cref="ReturnNullStations"/> is set.</returns>
    public IReadOnlyList<StationSnapshot> GetStations()
    {
        StationReads++;
        CallLog.Add("GetStations");
        ThrowIfRequested();
        if (ReturnNullStations)
        {
            return null!;
        }

        return Stations;
    }

    /// <summary>Counts and logs a container read, then returns the containers or throws <see cref="ThrowOnRead"/>.</summary>
    /// <returns>The current containers, or null when <see cref="ReturnNullContainers"/> is set.</returns>
    public IReadOnlyList<ContainerSnapshot> GetContainers()
    {
        ContainerReads++;
        CallLog.Add("GetContainers");
        ThrowIfRequested();
        if (ReturnNullContainers)
        {
            return null!;
        }

        return Containers;
    }

    private void ThrowIfRequested()
    {
        Exception? error = ThrowOnRead;
        if (error is not null)
        {
            throw error;
        }
    }
}

/// <summary>
/// The S-E002-003 world as literals (ADR-GWIN-025, the fixture world of the test plan). Build range is 20 unless stated. Every container is
/// 4 wide, 2 high, with no contents. Expected ids and results in the tests are copied from these literals and never computed by Core.
/// </summary>
/// <remarks>
/// Base A is stations 1 (0,0,0) and 2 (15,0,0). Base B is station 3 (100,0,0). Station 4 (50,0,50) has range 0, so it is ineligible.
/// Stations 8 (300,0,0) and 9 (337,0,0) are 37 m apart, so they are two separate bases. Containers 10 (5,0,0) and 13 (30,0,0) are in A,
/// 11 (100,0,5) is in B, and 12 (60,0,60) is in no base. Container 14 (8,0,0) and station 5 (115,0,0) are used only where a row adds them.
/// Methods return fresh arrays and lists, so no test changes a shared fixture.
/// </remarks>
internal static class ResolutionFixtures
{
    /// <summary>The station id for a serial number, under user id 1.</summary>
    /// <param name="serial">The per-peer serial.</param>
    /// <returns>The station id.</returns>
    internal static StationId StationKey(int serial) => new StationId(1, (uint)serial);

    /// <summary>The container id for a serial number, under user id 1.</summary>
    /// <param name="serial">The per-peer serial.</param>
    /// <returns>The container id.</returns>
    internal static ContainerId ContainerKey(int serial) => new ContainerId(1, (uint)serial);

    /// <summary>The serial numbers of stations, in the order given.</summary>
    /// <param name="stations">The stations to read.</param>
    /// <returns>The serials.</returns>
    internal static int[] Serials(IEnumerable<StationSnapshot> stations) => stations.Select(station => (int)station.Id.Id).ToArray();

    /// <summary>The serial numbers of containers, in the order given.</summary>
    /// <param name="containers">The containers to read.</param>
    /// <returns>The serials.</returns>
    internal static int[] Serials(IEnumerable<ContainerSnapshot> containers) => containers.Select(container => (int)container.Id.Id).ToArray();

    /// <summary>The default options of the window tests: a 2 s window and a cap of 64.</summary>
    /// <returns>The options.</returns>
    internal static BaseCacheOptions DefaultOptions() => new BaseCacheOptions(2, 64);

    /// <summary>The stations of the fixture world, in serial order 1, 2, 3, 4, 8, 9.</summary>
    /// <returns>A fresh array.</returns>
    internal static StationSnapshot[] WorldStations() => new[]
    {
        Station(1, 0f, 0f),
        Station(2, 15f, 0f),
        Station(3, 100f, 0f),
        Station(4, 50f, 50f, 0f),
        Station(8, 300f, 0f),
        Station(9, 337f, 0f),
    };

    /// <summary>The containers of the fixture world, in serial order 10, 13, 11, 12.</summary>
    /// <returns>A fresh array.</returns>
    internal static ContainerSnapshot[] WorldContainers() => new[]
    {
        Container(10, 5f, 0f, 0f),
        Container(13, 30f, 0f, 0f),
        Container(11, 100f, 0f, 5f),
        Container(12, 60f, 0f, 60f),
    };

    /// <summary>A station that the rows add to the world: station 5 at (115,0,0), 15 m from station 3, so it joins base B.</summary>
    /// <returns>The station.</returns>
    internal static StationSnapshot Station5() => Station(5, 115f, 0f);

    /// <summary>A container that the rows add: container 14 at (8,0,0), in base A.</summary>
    /// <returns>The container.</returns>
    internal static ContainerSnapshot Container14() => Container(14, 8f, 0f, 0f);

    /// <summary>A source that already holds the fixture world, with its counters at zero.</summary>
    /// <returns>The source.</returns>
    internal static CountingSnapshotSource LoadedSource()
    {
        var source = new CountingSnapshotSource();
        source.Stations.AddRange(WorldStations());
        source.Containers.AddRange(WorldContainers());
        return source;
    }

    private static StationSnapshot Station(int serial, float x, float z, float buildRange = 20f) =>
        new StationSnapshot(StationKey(serial), new WorldPosition(x, 0f, z), buildRange);

    private static ContainerSnapshot Container(int serial, float x, float y, float z) =>
        new ContainerSnapshot(
            ContainerKey(serial),
            new WorldPosition(x, y, z),
            4,
            2,
            Array.Empty<ItemStackSnapshot>(),
            ContainerFlags.PlayerBuilt);
}
