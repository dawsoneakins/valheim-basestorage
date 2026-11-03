namespace BaseStorage.Plugin.Feeding;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Feeding;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.ChestCategories;
using BaseStorage.Plugin.ChestNames;
using BaseStorage.Plugin.ChestSort;
using BaseStorage.Plugin.Reservations;
using BaseStorage.Plugin.World;
using UnityEngine;

/// <summary>
/// Keeps stations fed from base chests. Only the station's network owner feeds it, so two players never fill the same station. One
/// transfer runs at a time: it reserves the chest through the same handshake as crafting and Organize, moves the items in one frame, and
/// releases the chest. Items leave the chest before they enter the station, and a station that was lost in between gets nothing taken.
/// Main thread only.
/// </summary>
internal static class StationFeeder
{
    private const string Operation = "feed";
    private const string SetSelectionRpc = "BaseStorage_SetFeed";
    private static readonly TimeSpan PassInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan NextOrderDelay = TimeSpan.FromSeconds(0.1);
    private static readonly TimeSpan BusyChestCooldown = TimeSpan.FromSeconds(15);
    private static readonly Dictionary<ContainerId, CategoryAssignment> Assignments = new Dictionary<ContainerId, CategoryAssignment>();

    private static readonly List<FeedTarget> Stations = new List<FeedTarget>();
    private static readonly List<Container> Scratch = new List<Container>();
    private static readonly List<ContainerSnapshot> Chests = new List<ContainerSnapshot>();
    private static readonly Dictionary<ContainerId, Container> ById = new Dictionary<ContainerId, Container>();
    private static readonly Dictionary<ContainerId, TimeSpan> Cooldowns = new Dictionary<ContainerId, TimeSpan>();
    private static readonly List<ContainerId> Expired = new List<ContainerId>();

    private static PluginWorld? world;
    private static ReservationDriver<Container>? driver;
    private static IClock? clock;
    private static BaseStorageSettings? settings;
    private static Job? job;
    private static TimeSpan nextPass = TimeSpan.Zero;
    private static int cursor;

    /// <summary>Stores the services the feeder uses.</summary>
    /// <param name="current">The plugin world.</param>
    /// <param name="reservations">The reservation driver.</param>
    /// <param name="time">The clock.</param>
    /// <param name="config">The settings.</param>
    internal static void Initialize(PluginWorld current, ReservationDriver<Container> reservations, IClock time, BaseStorageSettings config)
    {
        world = current;
        driver = reservations;
        clock = time;
        settings = config;
    }

    /// <summary>Registers a station's selection message, so a player who does not own the station can change its selection.</summary>
    /// <param name="station">The station.</param>
    internal static void Register(FeedTarget station)
    {
        ZNetView view = station.View;
        view.Register<string>(SetSelectionRpc, (sender, value) =>
        {
            if (view.IsValid() && view.IsOwner())
            {
                view.GetZDO().Set(PersistenceKeys.StationFeed, value);
            }
        });
    }

    /// <summary>Reads a station's selection.</summary>
    /// <param name="station">The station.</param>
    /// <returns>The selection; empty for an invalid view.</returns>
    internal static FeedSelection Read(FeedTarget station)
    {
        ZNetView view = station.View;
        return view != null && view.IsValid()
            ? FeedSelection.Decode(view.GetZDO().GetString(PersistenceKeys.StationFeed, string.Empty))
            : FeedSelection.Empty;
    }

    /// <summary>Stores a station's selection through its owner.</summary>
    /// <param name="station">The station.</param>
    /// <param name="selection">The selection.</param>
    internal static void Write(FeedTarget station, FeedSelection selection)
    {
        ZNetView view = station.View;
        if (view != null && view.IsValid())
        {
            view.InvokeRPC(SetSelectionRpc, selection.Encode());
        }
    }

    /// <summary>Reads what a station holds and accepts.</summary>
    /// <param name="station">The station.</param>
    /// <returns>The state.</returns>
    internal static FeedStationState StateOf(FeedTarget station) => station.State();

    /// <summary>Fills a list with the chests of the base around a position, with their contents as they are now.</summary>
    /// <param name="position">The position.</param>
    /// <param name="chests">The list to fill.</param>
    /// <returns>False when the position is not in a base.</returns>
    internal static bool LoadChests(Vector3 position, List<ContainerSnapshot> chests)
    {
        chests.Clear();
        ById.Clear();
        PluginWorld? current = world;
        BaseStorageSettings? config = settings;
        if (current == null || config == null)
        {
            return false;
        }

        var at = new WorldPosition(position.x, position.y, position.z);
        BaseResolution resolution = current.Cache.Resolve(BaseStart.FromPosition(at), new BaseCacheOptions(config.BaseCacheSeconds, config.MaxBaseStations));
        if (!resolution.IsInBase)
        {
            return false;
        }

        Scratch.Clear();
        current.Containers.CopyItemsTo(Scratch);
        for (int i = 0; i < Scratch.Count; i++)
        {
            Container chest = Scratch[i];
            ZDO? zdo = chest != null && chest.m_nview != null && chest.m_nview.IsValid() ? chest.m_nview.GetZDO() : null;
            if (zdo != null)
            {
                ById[new ContainerId(zdo.m_uid.UserID, zdo.m_uid.ID)] = chest!;
            }
        }

        Assignments.Clear();
        for (int i = 0; i < resolution.Containers.Count; i++)
        {
            ContainerId id = resolution.Containers[i].Id;
            if (ById.TryGetValue(id, out Container? member))
            {
                CategoryAssignment? assignment = ChestCategoryWriter.Read(member);
                if (assignment != null)
                {
                    Assignments[id] = assignment;
                }
            }
        }

        // The base cache may hold older contents, and the keep amount needs the real counts.
        EligibilityResult eligibility = ContainerEligibility.Evaluate(resolution.Containers, Assignments, ContainerPurpose.CraftAvailability, EligibilityOptions.FromSettings(config));
        for (int i = 0; i < eligibility.Eligible.Count; i++)
        {
            if (ById.TryGetValue(eligibility.Eligible[i].Id, out Container? chest))
            {
                ContainerSnapshot? fresh = current.Snapshots.Snapshot(chest);
                if (fresh != null)
                {
                    chests.Add(fresh);
                }
            }
        }

        return true;
    }

    /// <summary>Runs the current transfer, or looks for the next one. Called once per frame.</summary>
    internal static void Tick()
    {
        FeedPanel.Tick();
        ChestSorter.Tick();
        ChestLabels.Tick();
        if (clock == null || driver == null || world == null)
        {
            return;
        }

        if (job != null)
        {
            Advance(job);
            return;
        }

        TimeSpan now = clock.Elapsed;
        if (now < nextPass || Player.m_localPlayer == null)
        {
            return;
        }

        nextPass = now + PassInterval;
        ForgetCooldowns(now);
        Stations.Clear();
        world.FeedStations.CopyItemsTo(Stations);
        for (int i = 0; i < Stations.Count; i++)
        {
            FeedTarget station = Stations[(cursor + i) % Stations.Count];
            if (TryStart(station))
            {
                cursor = (cursor + i + 1) % Stations.Count;
                nextPass = now + NextOrderDelay;
                return;
            }
        }
    }

    private static bool TryStart(FeedTarget station)
    {
        ZNetView? view = station.IsAlive ? station.View : null;
        if (view == null || !view.IsValid() || !view.IsOwner())
        {
            return false;
        }

        FeedSelection selection = Read(station);
        if (selection.Entries.Count == 0 || !station.IsReady || !LoadChests(station.Station.transform.position, Chests))
        {
            return false;
        }

        Vector3 at = station.Station.transform.position;
        FeedOrder? order = FeedPlanner.Next(StateOf(station), selection, Chests, Assignments, EligibilityOptions.FromSettings(settings!), new WorldPosition(at.x, at.y, at.z), Cooldowns.Keys);
        if (order == null || !ById.TryGetValue(order.Chest, out Container? chest))
        {
            return false;
        }

        ReservationRoute? route = driver!.Begin(chest, Operation);
        if (route == null)
        {
            Cool(order.Chest);
            return false;
        }

        job = new Job(station, chest, order);
        return true;
    }

    private static void Advance(Job current)
    {
        ContainerId id = current.Order.Chest;
        Reservation<string>? reservation = driver!.Find(id);
        if (reservation == null || !string.Equals(reservation.Operation, Operation, StringComparison.Ordinal))
        {
            job = null;
            return;
        }

        if (reservation.State == ReservationState.Requesting)
        {
            return;
        }

        if (reservation.State == ReservationState.Granted)
        {
            driver.BeginWrite(id);
        }

        if (driver.CanWrite(id))
        {
            Move(current);
        }
        else
        {
            Cool(id);
        }

        driver.Release(id);
        job = null;
    }

    /// <summary>Takes the items out of the chest and puts them into the station, in one frame, after checking the station is still ours.</summary>
    private static void Move(Job current)
    {
        FeedTarget station = current.Station;
        Inventory? inventory = current.Chest == null ? null : current.Chest.GetInventory();
        if (!station.IsAlive || inventory == null || !station.View.IsValid() || !station.View.IsOwner() || !station.IsReady)
        {
            return;
        }

        FeedStationState state = StateOf(station);
        FeedOrder order = current.Order;
        int left = Math.Min(order.Amount, order.IsFuel ? state.FuelRoom : state.QueueRoom);
        int moved = 0;
        foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
        {
            if (left == 0)
            {
                break;
            }

            if (item.m_dropPrefab == null || !string.Equals(item.m_dropPrefab.name, order.PrefabName, StringComparison.Ordinal))
            {
                continue;
            }

            bool cheated = item.m_cheated;
            int take = Math.Min(left, item.m_stack);
            if (!inventory.RemoveItem(item, take))
            {
                continue;
            }

            left -= take;
            moved += take;
            if (!order.IsFuel)
            {
                for (int i = 0; i < take; i++)
                {
                    station.AddInput(order.PrefabName, cheated);
                }
            }
        }

        if (moved == 0)
        {
            return;
        }

        if (order.IsFuel)
        {
            station.AddFuel(moved);
        }
        else
        {
            station.InputsAdded();
        }
    }

    private static void Cool(ContainerId id) => Cooldowns[id] = clock!.Elapsed + BusyChestCooldown;

    private static void ForgetCooldowns(TimeSpan now)
    {
        Expired.Clear();
        foreach (KeyValuePair<ContainerId, TimeSpan> pair in Cooldowns)
        {
            if (pair.Value <= now)
            {
                Expired.Add(pair.Key);
            }
        }

        for (int i = 0; i < Expired.Count; i++)
        {
            Cooldowns.Remove(Expired[i]);
        }
    }

    private sealed class Job
    {
        internal Job(FeedTarget station, Container chest, FeedOrder order)
        {
            Station = station;
            Chest = chest;
            Order = order;
        }

        internal FeedTarget Station { get; }

        internal Container Chest { get; }

        internal FeedOrder Order { get; }
    }
}
