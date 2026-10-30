namespace BaseStorage.Core.Tests.Bases;

using System;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.ResolutionFixtures;

/// <summary>AC-1 (ADR-GWIN-024, the argument order): the resolver rejects bad arguments before it does any work.</summary>
public sealed class ResolverArgumentTests
{
    // GW-U-238 (AC-1): a null station list is rejected for "stations", also when the container list is null too.
    [Fact]
    public void NullStationsIsRejectedForStations()
    {
        ArgumentNullException withContainers = Assert.Throws<ArgumentNullException>(() =>
        {
            BaseResolver.Resolve(null!, WorldContainers(), BaseStart.FromStation(StationKey(1)), 64);
        });
        ArgumentNullException withoutContainers = Assert.Throws<ArgumentNullException>(() =>
        {
            BaseResolver.Resolve(null!, null!, BaseStart.FromStation(StationKey(1)), 64);
        });

        Assert.Equal("stations", withContainers.ParamName);
        Assert.Equal("stations", withoutContainers.ParamName);
    }

    // GW-U-238 (AC-1): a null container list is rejected for "containers".
    [Fact]
    public void NullContainersIsRejectedForContainers()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
        {
            BaseResolver.Resolve(WorldStations(), null!, BaseStart.FromStation(StationKey(1)), 64);
        });

        Assert.Equal("containers", ex.ParamName);
    }

    // GW-U-238 (AC-1): default(BaseStart) has no defined kind, so it is rejected for "start".
    [Fact]
    public void DefaultStartIsRejectedForStart()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
        {
            BaseResolver.Resolve(WorldStations(), WorldContainers(), default(BaseStart), 64);
        });

        Assert.Equal("start", ex.ParamName);
    }

    // GW-U-238 (AC-1): a null station element is rejected with the existing Bases message, for "stations".
    [Fact]
    public void NullStationElementIsRejectedForStations()
    {
        StationSnapshot[] stations = { WorldStations()[0], null!, WorldStations()[2] };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
        {
            BaseResolver.Resolve(stations, WorldContainers(), BaseStart.FromStation(StationKey(1)), 64);
        });

        Assert.Equal("stations", ex.ParamName);
    }

    // GW-U-238 (AC-1): a null container element is rejected with the existing Bases message, for "containers".
    [Fact]
    public void NullContainerElementIsRejectedForContainers()
    {
        ContainerSnapshot[] containers = { WorldContainers()[0], null!, WorldContainers()[2] };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
        {
            BaseResolver.Resolve(WorldStations(), containers, BaseStart.FromStation(StationKey(1)), 64);
        });

        Assert.Equal("containers", ex.ParamName);
    }
}
