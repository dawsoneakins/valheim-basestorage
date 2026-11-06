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
using BaseStorage.Plugin.ChestCategories;
using BaseStorage.Plugin.ChestNames;
using BaseStorage.Plugin.Feeding;

/// <summary>
/// The Plugin's loaded-object world (contract 4.1, ADR-GWIN-030): the registries of loaded containers and crafting stations,
/// the snapshot adapter over them, and the single base cache. The patch classes forward here. Main thread only (W2).
/// </summary>
internal sealed class PluginWorld
{
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
        this.FeedStations = new LoadedRegistry<ZNetView, FeedTarget>(static () => { }, static () => { });
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
    internal LoadedRegistry<ZNetView, FeedTarget> FeedStations { get; private set; } = null!;

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
        CharacterMenu.WornItems.WidenIfGrave(container);
        if (container != null && container.m_nview != null && container.m_nview.IsValid() && Containers.Add(container.m_nview, container))
        {
            ChestNames.Register(container);
        }
    }

    /// <summary>Unregisters the container of a destroyed network view. Views of anything else change nothing.</summary>
    /// <param name="view">The destroyed view.</param>
    internal void OnNetViewDestroy(ZNetView view)
    {
        Containers.Remove(view);
        FeedStations.Remove(view);
    }

    /// <summary>Registers a station after its Awake. A station without a valid view (a placement ghost) is ignored.</summary>
    /// <param name="station">The station.</param>
    internal void OnFeedStationAwake(UnityEngine.Component station)
    {
        FeedTarget? target = FeedTargets.Of(station);
        if (target != null && FeedStations.Add(target.View, target))
        {
            StationFeeder.Register(target);
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
                        ReadAssignments(resolution.Containers),
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

    /// <summary>Reads the persisted category assignment of each snapshot's loaded chest (P8), so the base summary applies the real Keep marks.</summary>
    /// <param name="snapshots">The member chests of a base.</param>
    /// <returns>The assignment per chest id; chests without one are left out.</returns>
    private Dictionary<ContainerId, CategoryAssignment> ReadAssignments(IReadOnlyList<ContainerSnapshot> snapshots)
    {
        var assignments = new Dictionary<ContainerId, CategoryAssignment>();
        var loaded = new List<Container>();
        Containers.CopyItemsTo(loaded);
        var byId = new Dictionary<ContainerId, Container>(loaded.Count);
        for (int i = 0; i < loaded.Count; i++)
        {
            Container container = loaded[i];
            if (container == null || container.m_nview == null || !container.m_nview.IsValid())
            {
                continue;
            }

            ZDO zdo = container.m_nview.GetZDO();
            if (zdo != null)
            {
                byId[new ContainerId(zdo.m_uid.UserID, zdo.m_uid.ID)] = container;
            }
        }

        for (int i = 0; i < snapshots.Count; i++)
        {
            if (byId.TryGetValue(snapshots[i].Id, out Container? chest))
            {
                CategoryAssignment? assignment = ChestCategoryWriter.Read(chest);
                if (assignment != null)
                {
                    assignments[snapshots[i].Id] = assignment;
                }
            }
        }

        return assignments;
    }
}
