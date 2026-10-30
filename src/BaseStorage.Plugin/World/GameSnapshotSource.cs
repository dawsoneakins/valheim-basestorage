namespace BaseStorage.Plugin.World;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Snapshots;
using UnityEngine;

/// <summary>
/// Fills the Core container and station snapshots from the loaded registries (contract 6, ADR-GWIN-030). It only reads game
/// state, runs only when the base cache rebuilds, and reuses its buffers and unchanged snapshot instances. A game object whose
/// values Core would reject is left out of the build instead of throwing (W7). Main thread only (W2).
/// </summary>
internal sealed class GameSnapshotSource : IBaseSnapshotSource
{
    private const int MaxDimension = 256;

    private static readonly IComparer<StationSnapshot> StationOrder = new StationIdComparer();
    private static readonly IComparer<ContainerSnapshot> ContainerOrder = new ContainerIdComparer();
    private static readonly Dictionary<GameObject, string> PrefabNames = new Dictionary<GameObject, string>(new PrefabIdentityComparer());

    private readonly LoadedRegistry<ZNetView, Container> containers;
    private readonly LoadedRegistry<CraftingStation, CraftingStation> stations;
    private readonly List<Container> containerScratch = new List<Container>();
    private readonly List<CraftingStation> stationScratch = new List<CraftingStation>();
    private readonly List<ContainerSnapshot> containerOutput = new List<ContainerSnapshot>();
    private readonly List<StationSnapshot> stationOutput = new List<StationSnapshot>();
    private readonly List<ItemStackSnapshot> contentsScratch = new List<ItemStackSnapshot>();
    private readonly SnapshotMemo<Container, ContainerSnapshot> containerMemo = new SnapshotMemo<Container, ContainerSnapshot>();
    private readonly SnapshotMemo<CraftingStation, StationSnapshot> stationMemo = new SnapshotMemo<CraftingStation, StationSnapshot>();
    private readonly SnapshotMemo<Container, ContainerTraits> traitsMemo = new SnapshotMemo<Container, ContainerTraits>();

    private bool hasPlayer;
    private long playerId;
    private long sessionId;
    private List<ZNet.PlayerInfo>? players;
    private Container? openContainer;

    /// <summary>Creates the adapter over the two registries.</summary>
    /// <param name="containers">The registry of loaded containers.</param>
    /// <param name="stations">The registry of loaded crafting stations.</param>
    /// <exception cref="ArgumentNullException"><paramref name="containers"/> or <paramref name="stations"/> is null.</exception>
    internal GameSnapshotSource(
        LoadedRegistry<ZNetView, Container> containers,
        LoadedRegistry<CraftingStation, CraftingStation> stations)
    {
        if (containers == null)
        {
            throw new ArgumentNullException(nameof(containers));
        }

        if (stations == null)
        {
            throw new ArgumentNullException(nameof(stations));
        }

        this.containers = containers;
        this.stations = stations;
    }

    /// <summary>Builds the station snapshots of the loaded stations, sorted by id. The same list instance is returned on every call.</summary>
    /// <returns>The station snapshots.</returns>
    public IReadOnlyList<StationSnapshot> GetStations()
    {
        stations.CopyItemsTo(stationScratch);
        stationMemo.Begin();
        stationOutput.Clear();
        for (int i = 0; i < stationScratch.Count; i++)
        {
            CraftingStation station = stationScratch[i];
            if (station == null || station.m_nview == null || !station.m_nview.IsValid())
            {
                continue;
            }

            ZDO zdo = station.m_nview.GetZDO();
            if (zdo == null)
            {
                continue;
            }

            ZDOID uid = zdo.m_uid;
            var id = new StationId(uid.UserID, uid.ID);
            Vector3 point = station.transform.position;
            var position = new WorldPosition(point.x, point.y, point.z);
            float range = station.GetStationBuildRange();
            if (id.IsNone || !IsFinite(point.x) || !IsFinite(point.y) || !IsFinite(point.z) || !IsFinite(range) || range < 0f)
            {
                continue;
            }

            StationSnapshot? snapshot = stationMemo.Previous(station);
            if (snapshot == null || snapshot.Id != id || snapshot.Position != position || snapshot.BuildRange != range)
            {
                snapshot = new StationSnapshot(id, position, range);
            }

            stationMemo.Keep(station, snapshot);
            stationOutput.Add(snapshot);
        }

        stationMemo.End();
        stationScratch.Clear();
        stationOutput.Sort(StationOrder);
        RemoveDuplicateStations();
        return stationOutput;
    }

    /// <summary>Builds the container snapshots of the loaded containers, sorted by id. The same list instance is returned on every call.</summary>
    /// <returns>The container snapshots.</returns>
    public IReadOnlyList<ContainerSnapshot> GetContainers()
    {
        containers.CopyItemsTo(containerScratch);
        ReadLocalContext();
        containerMemo.Begin();
        traitsMemo.Begin();
        containerOutput.Clear();
        for (int i = 0; i < containerScratch.Count; i++)
        {
            Container container = containerScratch[i];
            ContainerSnapshot? snapshot = Build(container, containerMemo.Previous(container));
            if (snapshot == null)
            {
                continue;
            }

            containerMemo.Keep(container, snapshot);
            containerOutput.Add(snapshot);
        }

        containerMemo.End();
        traitsMemo.End();
        containerScratch.Clear();
        containerOutput.Sort(ContainerOrder);
        RemoveDuplicateContainers();
        return containerOutput;
    }

    /// <summary>
    /// Builds a fresh snapshot of one loaded container for a live read (S-E004-011): the same mapping and omission rules as
    /// <see cref="GetContainers"/>, without the memo. The local context (player, session, open chest) is read on every call.
    /// </summary>
    /// <param name="container">The container, which may be a destroyed Unity object.</param>
    /// <returns>The snapshot, or null when the container has no valid view or a value Core would reject.</returns>
    public ContainerSnapshot? Snapshot(Container container)
    {
        ReadLocalContext();
        return Build(container, null);
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    /// <summary>Maps one container to its snapshot, reusing <paramref name="previous"/> when nothing changed; null when the container is omitted (W7).</summary>
    /// <param name="container">The container.</param>
    /// <param name="previous">The snapshot kept for the container by the last build, or null.</param>
    /// <returns>The snapshot, or null.</returns>
    private ContainerSnapshot? Build(Container container, ContainerSnapshot? previous)
    {
        if (container == null || container.m_nview == null || !container.m_nview.IsValid())
        {
            return null;
        }

        ZDO zdo = container.m_nview.GetZDO();
        Inventory inventory = container.m_inventory;
        if (zdo == null || inventory == null)
        {
            return null;
        }

        ZDOID uid = zdo.m_uid;
        var id = new ContainerId(uid.UserID, uid.ID);
        Vector3 point = container.transform.position;
        int width = inventory.GetWidth();
        int height = inventory.GetHeight();
        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        if (id.IsNone
            || !IsFinite(point.x) || !IsFinite(point.y) || !IsFinite(point.z)
            || width < 1 || width > MaxDimension
            || height < 1 || height > MaxDimension
            || items == null
            || !ItemsAreValid(items))
        {
            return null;
        }

        var position = new WorldPosition(point.x, point.y, point.z);
        ContainerFlags flags = ReadFlags(container, zdo, point);
        if (previous != null && Matches(previous, id, position, width, height, flags, items))
        {
            return previous;
        }

        contentsScratch.Clear();
        for (int k = 0; k < items.Count; k++)
        {
            ItemDrop.ItemData item = items[k];
            contentsScratch.Add(new ItemStackSnapshot(PrefabName(item.m_dropPrefab), item.m_stack, item.m_quality, item.m_shared.m_maxStackSize));
        }

        var snapshot = new ContainerSnapshot(id, position, width, height, contentsScratch, flags);
        contentsScratch.Clear();
        return snapshot;
    }

    private static string PrefabName(GameObject prefab)
    {
        if (!PrefabNames.TryGetValue(prefab, out string? name))
        {
            name = prefab.name;
            PrefabNames[prefab] = name;
        }

        return name;
    }

    private static bool ItemsAreValid(List<ItemDrop.ItemData> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            ItemDrop.ItemData item = items[i];
            if (item == null
                || item.m_dropPrefab == null
                || item.m_stack < 1
                || item.m_quality < 1
                || item.m_shared == null
                || item.m_shared.m_maxStackSize < 1
                || string.IsNullOrWhiteSpace(PrefabName(item.m_dropPrefab)))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Matches(
        ContainerSnapshot snapshot,
        ContainerId id,
        WorldPosition position,
        int width,
        int height,
        ContainerFlags flags,
        List<ItemDrop.ItemData> items)
    {
        if (snapshot.Id != id || snapshot.Position != position || snapshot.Width != width || snapshot.Height != height
            || snapshot.Flags != flags || snapshot.Contents.Count != items.Count)
        {
            return false;
        }

        for (int i = 0; i < items.Count; i++)
        {
            ItemDrop.ItemData item = items[i];
            ItemStackSnapshot kept = snapshot.Contents[i];
            if (kept.Count != item.m_stack
                || kept.Quality != item.m_quality
                || kept.MaxStack != item.m_shared.m_maxStackSize
                || !string.Equals(kept.PrefabName, PrefabName(item.m_dropPrefab), StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private void ReadLocalContext()
    {
        hasPlayer = false;
        playerId = 0L;
        if (Game.instance != null)
        {
            PlayerProfile profile = Game.instance.GetPlayerProfile();
            if (profile != null)
            {
                hasPlayer = true;
                playerId = profile.GetPlayerID();
            }
        }

        sessionId = ZDOMan.GetSessionID();
        players = ZNet.instance != null ? ZNet.instance.GetPlayerList() : null;
        openContainer = InventoryGui.instance != null ? InventoryGui.instance.m_currentContainer : null;
    }

    private ContainerFlags ReadFlags(Container container, ZDO zdo, Vector3 point)
    {
        ContainerFlags flags = ContainerFlags.None;

        // Snapshot() reads one chest outside a GetContainers build, when the memo cannot be used.
        bool building = traitsMemo.IsBuilding;
        ContainerTraits? traits = building ? traitsMemo.Previous(container) : null;
        if (traits == null)
        {
            traits = new ContainerTraits(container);
        }

        if (building)
        {
            traitsMemo.Keep(container, traits);
        }

        if (traits.Piece != null && traits.Piece.GetCreator() != 0L)
        {
            flags |= ContainerFlags.PlayerBuilt;
        }

        if (traits.IsVehicle)
        {
            flags |= ContainerFlags.Vehicle;
        }

        if (traits.IsTombstone)
        {
            flags |= ContainerFlags.Tombstone;
        }

        if (!hasPlayer
            || !container.CheckAccess(playerId)
            || (container.m_checkGuardStone && !PrivateArea.CheckAccess(point, 0f, false, false)))
        {
            flags |= ContainerFlags.AccessDenied;
        }

        if (container != openContainer && zdo.GetInt(ZDOVars.s_inUse, 0) == 1)
        {
            long owner = zdo.GetOwner();
            if (owner != sessionId && IsConnectedPlayer(owner))
            {
                flags |= ContainerFlags.InUseByOther;
            }
        }

        return flags;
    }

    private bool IsConnectedPlayer(long owner)
    {
        if (players == null)
        {
            return false;
        }

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].m_characterID.UserID == owner)
            {
                return true;
            }
        }

        return false;
    }

    // The two duplicate removers and comparers stay per type on purpose: a generic helper would need a delegate or an
    // interface call per element, which the rebuild path avoids (AC-6, no per-rebuild allocation).
    private void RemoveDuplicateStations()
    {
        for (int i = stationOutput.Count - 1; i > 0; i--)
        {
            if (stationOutput[i].Id == stationOutput[i - 1].Id)
            {
                stationOutput.RemoveAt(i);
            }
        }
    }

    private void RemoveDuplicateContainers()
    {
        for (int i = containerOutput.Count - 1; i > 0; i--)
        {
            if (containerOutput[i].Id == containerOutput[i - 1].Id)
            {
                containerOutput.RemoveAt(i);
            }
        }
    }

    private static int ComparePosition(WorldPosition x, WorldPosition y)
    {
        int result = x.X.CompareTo(y.X);
        if (result == 0)
        {
            result = x.Y.CompareTo(y.Y);
        }

        return result != 0 ? result : x.Z.CompareTo(y.Z);
    }

    /// <summary>The per-container traits that do not change while the container lives (ADR-GWIN-030): the parent piece, vehicle and tombstone lookups.</summary>
    private sealed class ContainerTraits
    {
        /// <summary>Reads the traits of a container.</summary>
        /// <param name="container">The container.</param>
        internal ContainerTraits(Container container)
        {
            Piece = container.m_piece != null ? container.m_piece : container.GetComponentInParent<Piece>();
            IsVehicle = container.m_wagon != null || container.GetComponentInParent<Vagon>() != null || container.GetComponentInParent<Ship>() != null;
            IsTombstone = container.GetComponent<TombStone>() != null;
        }

        /// <summary>Gets the piece the container belongs to, or null.</summary>
        internal Piece? Piece { get; }

        /// <summary>Gets a value that tells whether the container is part of a vehicle.</summary>
        internal bool IsVehicle { get; }

        /// <summary>Gets a value that tells whether the container is a tombstone.</summary>
        internal bool IsTombstone { get; }
    }

    private sealed class StationIdComparer : IComparer<StationSnapshot>
    {
        public int Compare(StationSnapshot? x, StationSnapshot? y)
        {
            int result = x!.Id.CompareTo(y!.Id);
            if (result == 0)
            {
                result = ComparePosition(x.Position, y.Position);
            }

            return result != 0 ? result : x.BuildRange.CompareTo(y.BuildRange);
        }
    }

    private sealed class ContainerIdComparer : IComparer<ContainerSnapshot>
    {
        public int Compare(ContainerSnapshot? x, ContainerSnapshot? y)
        {
            int result = x!.Id.CompareTo(y!.Id);
            if (result == 0)
            {
                result = ComparePosition(x.Position, y.Position);
            }

            return result != 0 ? result : ((int)x.Flags).CompareTo((int)y.Flags);
        }
    }

    private sealed class PrefabIdentityComparer : IEqualityComparer<GameObject>
    {
        public bool Equals(GameObject? x, GameObject? y) => ReferenceEquals(x, y);

        public int GetHashCode(GameObject obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
