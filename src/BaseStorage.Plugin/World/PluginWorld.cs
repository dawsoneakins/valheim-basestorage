namespace BaseStorage.Plugin.World;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Logging;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.Feeding;

/// <summary>
/// The Plugin's loaded-object world (contract 4.1, ADR-GWIN-030): the registries of loaded containers and crafting stations,
/// the snapshot adapter over them, and the single base cache. The patch classes forward here. Main thread only (W2).
/// </summary>
internal sealed class PluginWorld
{
    private static readonly Dictionary<ContainerId, CategoryAssignment> NoAssignments = new Dictionary<ContainerId, CategoryAssignment>();

    private static PluginWorld? current;

    private PluginWorld(IClock clock)
    {
        this.Containers = new LoadedRegistry<ZNetView, Container>(
            () =>
            {
                Cache.OnContainerPlaced();
                if (LogPolicy.IsEnabled(BaseStoragePlugin.LogSink, LogCategory.Detail))
                {
                    LogPolicy.Write(BaseStoragePlugin.LogSink, LogCategory.Detail, RegistryMessages.Changed(RegistryKind.Container, true, Containers.Count));
                }
            },
            () =>
            {
                Cache.OnContainerRemoved();
                if (LogPolicy.IsEnabled(BaseStoragePlugin.LogSink, LogCategory.Detail))
                {
                    LogPolicy.Write(BaseStoragePlugin.LogSink, LogCategory.Detail, RegistryMessages.Changed(RegistryKind.Container, false, Containers.Count));
                }
            });
        this.Stations = new LoadedRegistry<CraftingStation, CraftingStation>(
            () =>
            {
                Cache.OnStationPlaced();
                if (LogPolicy.IsEnabled(BaseStoragePlugin.LogSink, LogCategory.Detail))
                {
                    LogPolicy.Write(BaseStoragePlugin.LogSink, LogCategory.Detail, RegistryMessages.Changed(RegistryKind.Station, true, Stations.Count));
                }
            },
            () =>
            {
                Cache.OnStationRemoved();
                if (LogPolicy.IsEnabled(BaseStoragePlugin.LogSink, LogCategory.Detail))
                {
                    LogPolicy.Write(BaseStoragePlugin.LogSink, LogCategory.Detail, RegistryMessages.Changed(RegistryKind.Station, false, Stations.Count));
                }
            });
        this.Smelters = new LoadedRegistry<ZNetView, Smelter>(static () => { }, static () => { });
        this.Snapshots = new GameSnapshotSource(Containers, Stations);
        this.Cache = new BaseCache(Snapshots, clock);
    }

    /// <summary>Gets the world, or null until <see cref="Initialize"/> has run. Patch bodies treat null as "do nothing".</summary>
    internal static PluginWorld? Current => current;

    /// <summary>Gets the registry of loaded containers, keyed by the container's network view.</summary>
    internal LoadedRegistry<ZNetView, Container> Containers { get; private set; } = null!;

    /// <summary>Gets the registry of loaded crafting stations that have a build range.</summary>
    internal LoadedRegistry<CraftingStation, CraftingStation> Stations { get; private set; } = null!;

    /// <summary>Gets the registry of loaded stations that can be fed, keyed by the station's network view.</summary>
    internal LoadedRegistry<ZNetView, Smelter> Smelters { get; private set; } = null!;

    /// <summary>Gets the snapshot adapter that fills the Core snapshots from the registries.</summary>
    internal GameSnapshotSource Snapshots { get; }

    /// <summary>Gets the single base cache of the Plugin.</summary>
    internal BaseCache Cache { get; private set; } = null!;

    /// <summary>Creates the world on the first call and returns it; later calls return the existing instance unchanged.</summary>
    /// <param name="clock">The clock the base cache uses.</param>
    /// <returns>The world.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="clock"/> is null.</exception>
    internal static PluginWorld Initialize(IClock clock)
    {
        if (clock == null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        if (current == null)
        {
            current = new PluginWorld(clock);
        }

        return current;
    }

    /// <summary>Registers a container after its Awake. A container without a valid view (a placement ghost) is ignored.</summary>
    /// <param name="container">The container.</param>
    internal void OnContainerAwake(Container container)
    {
        if (container != null && container.m_nview != null && container.m_nview.IsValid())
        {
            Containers.Add(container.m_nview, container);
        }
    }

    /// <summary>Unregisters the container of a destroyed network view. Views of anything else change nothing.</summary>
    /// <param name="view">The destroyed view.</param>
    internal void OnNetViewDestroy(ZNetView view)
    {
        Containers.Remove(view);
        Smelters.Remove(view);
    }

    /// <summary>Registers a station after its Awake. A station without a valid view (a placement ghost) is ignored.</summary>
    /// <param name="smelter">The station.</param>
    internal void OnSmelterAwake(Smelter smelter)
    {
        if (smelter != null && smelter.m_nview != null && smelter.m_nview.IsValid() && Smelters.Add(smelter.m_nview, smelter))
        {
            StationFeeder.Register(smelter);
        }
    }

    /// <summary>Registers a crafting station after its Start when it has a valid view and a build range, and writes the Debug base summary.</summary>
    /// <param name="station">The station.</param>
    internal void OnStationStart(CraftingStation station)
    {
        if (station != null && station.m_nview != null && station.m_nview.IsValid() && station.m_rangeBuild > 0f)
        {
            if (Stations.Add(station, station))
            {
                BaseStorageSettings? settings = BaseStoragePlugin.Settings;
                if (settings != null && LogPolicy.IsEnabled(BaseStoragePlugin.LogSink, LogCategory.Detail))
                {
                    ZDOID uid = station.m_nview.GetZDO().m_uid;
                    var id = new StationId(uid.UserID, uid.ID);
                    BaseResolution resolution = Cache.Resolve(
                        BaseStart.FromStation(id),
                        new BaseCacheOptions(settings.BaseCacheSeconds, settings.MaxBaseStations));
                    EligibilityResult eligibility = ContainerEligibility.Evaluate(
                        resolution.Containers,
                        NoAssignments,
                        ContainerPurpose.CraftAvailability,
                        EligibilityOptions.FromSettings(settings));
                    LogPolicy.Write(BaseStoragePlugin.LogSink, LogCategory.Detail, RegistryMessages.BaseSummary(id, resolution, eligibility));
                }
            }
        }
    }

    /// <summary>Unregisters a crafting station that is being destroyed.</summary>
    /// <param name="station">The station.</param>
    internal void OnStationDestroy(CraftingStation station)
    {
        Stations.Remove(station);
    }
}
