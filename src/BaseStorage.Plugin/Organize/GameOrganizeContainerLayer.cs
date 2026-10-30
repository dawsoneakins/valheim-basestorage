namespace BaseStorage.Plugin.Organize;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.Reservations;
using BaseStorage.Plugin.World;
using UnityEngine;

/// <summary>
/// The game-backed <see cref="IContainerLayer"/> of one Organize run (contract section 5, rules RN9 and RN10). It reserves
/// chests only through the reservation driver, reports the driver's state through <see cref="OrganizeLayerRules.StatusOf"/>,
/// writes only while <c>CanWrite</c> is true, adds whole stacks only when <see cref="OrganizeLayerRules.CanFit"/> allows, and
/// removes by value. Live snapshots come from the world's snapshot adapter. Main thread only; Unity null checks use <c>==</c> (W4).
/// </summary>
internal sealed class GameOrganizeContainerLayer : IContainerLayer
{
    private readonly OrganizePlan plan;
    private readonly IReadOnlyDictionary<ContainerId, Container> containers;
    private readonly ReservationDriver<Container> reservations;
    private readonly GameSnapshotSource snapshots;
    private readonly HashSet<ContainerId> busy = new HashSet<ContainerId>();
    private readonly List<ContainerSnapshot> listing = new List<ContainerSnapshot>();
    private int nextMove;
    private ContainerId selectedSource;
    private int selectedIndex = -1;

    /// <summary>Creates the layer of one run.</summary>
    /// <param name="plan">The plan that runs; its moves identify the source stack of each add.</param>
    /// <param name="containers">The loaded container per chest id of the base.</param>
    /// <param name="reservations">The reservation driver.</param>
    /// <param name="snapshots">The snapshot adapter that maps a live container.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal GameOrganizeContainerLayer(
        OrganizePlan plan,
        IReadOnlyDictionary<ContainerId, Container> containers,
        ReservationDriver<Container> reservations,
        GameSnapshotSource snapshots)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (containers == null)
        {
            throw new ArgumentNullException(nameof(containers));
        }

        if (reservations == null)
        {
            throw new ArgumentNullException(nameof(reservations));
        }

        if (snapshots == null)
        {
            throw new ArgumentNullException(nameof(snapshots));
        }

        this.plan = plan;
        this.containers = containers;
        this.reservations = reservations;
        this.snapshots = snapshots;
    }

    /// <summary>Gets the none id: an Organize has no player inventory source.</summary>
    public ContainerId PlayerInventoryId => default;

    /// <summary>Gets a fresh snapshot of every mapped container that is still loaded, ordered by id.</summary>
    /// <returns>The snapshots.</returns>
    public IReadOnlyList<ContainerSnapshot> GetContainers()
    {
        listing.Clear();
        foreach (KeyValuePair<ContainerId, Container> pair in containers)
        {
            ContainerSnapshot? snapshot = snapshots.Snapshot(pair.Value);
            if (snapshot != null)
            {
                listing.Add(snapshot);
            }
        }

        listing.Sort(static (a, b) => a.Id.CompareTo(b.Id));
        return listing.ToArray();
    }

    /// <summary>Gets a fresh snapshot of one mapped container.</summary>
    /// <param name="id">The container id.</param>
    /// <returns>The snapshot, or null when the id is unknown or the container is no longer loaded.</returns>
    public ContainerSnapshot? GetContainer(ContainerId id)
    {
        Container? container = Find(id);
        return container == null ? null : snapshots.Snapshot(container);
    }

    /// <summary>Begins a reservation through the driver; a chest another operation holds is marked busy.</summary>
    /// <param name="id">The container id.</param>
    public void RequestReservation(ContainerId id)
    {
        Container? container = Find(id);
        if (container == null)
        {
            return;
        }

        Reservation<string>? existing = reservations.Find(id);
        if (existing != null && !existing.IsTerminal)
        {
            return;
        }

        busy.Remove(id);
        if (reservations.Begin(container, OrganizeLayerRules.Operation) == null)
        {
            busy.Add(id);
        }
    }

    /// <summary>Gets the layer status of the driver's reservation; a granted reservation is moved to Writing first.</summary>
    /// <param name="id">The container id.</param>
    /// <returns>The status.</returns>
    public ReservationStatus GetReservationStatus(ContainerId id)
    {
        Reservation<string>? reservation = reservations.Find(id);
        if (reservation != null && reservation.State == ReservationState.Granted)
        {
            reservations.BeginWrite(id);
        }

        return OrganizeLayerRules.StatusOf(reservation?.State, reservation == null ? ReservationFailure.None : reservation.Failure, busy.Contains(id));
    }

    /// <summary>Releases the driver's reservation and clears the busy mark.</summary>
    /// <param name="id">The container id.</param>
    public void ReleaseReservation(ContainerId id)
    {
        busy.Remove(id);
        reservations.Release(id);
    }

    /// <summary>Adds one stack to a chest, all or nothing (RN10).</summary>
    /// <param name="id">The destination id.</param>
    /// <param name="items">The stack to add.</param>
    /// <returns>The operation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null.</exception>
    public ContainerOperationResult AddItems(ContainerId id, ItemStackSnapshot items)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        Container? container = Find(id);
        ContainerSnapshot? live = container == null ? null : snapshots.Snapshot(container);
        if (container == null || live == null)
        {
            return ContainerOperationResult.NotFound;
        }

        if (!reservations.CanWrite(id))
        {
            return ContainerOperationResult.NotReserved;
        }

        if (!OrganizeLayerRules.CanFit(live.Contents, live.SlotCount, items))
        {
            return ContainerOperationResult.DestinationFull;
        }

        ItemDrop.ItemData? item = CloneForMove(id, items);
        if (item == null)
        {
            return ContainerOperationResult.DestinationFull;
        }

        return container.GetInventory().AddItem(item) ? ContainerOperationResult.Success : ContainerOperationResult.DestinationFull;
    }

    /// <summary>Remembers the exact live stack the next add copies from (S-A-004).</summary>
    /// <param name="source">The source container.</param>
    /// <param name="stackIndex">The index in the source's live contents.</param>
    public void SelectSourceStack(ContainerId source, int stackIndex)
    {
        selectedSource = source;
        selectedIndex = stackIndex;
    }

    /// <summary>Removes items from the live stack at an index when it still equals the expected stack (RN10).</summary>
    /// <param name="id">The source id.</param>
    /// <param name="stackIndex">The index of the stack in the container's contents.</param>
    /// <param name="expected">The stack the executor expects there.</param>
    /// <param name="count">How many items to remove.</param>
    /// <returns>The operation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expected"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="stackIndex"/> or <paramref name="count"/> is out of range.</exception>
    public ContainerOperationResult RemoveFromStack(ContainerId id, int stackIndex, ItemStackSnapshot expected, int count)
    {
        if (expected == null)
        {
            throw new ArgumentNullException(nameof(expected));
        }

        if (stackIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stackIndex));
        }

        if (count < 1 || count > expected.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        Container? container = Find(id);
        ContainerSnapshot? live = container == null ? null : snapshots.Snapshot(container);
        if (container == null || live == null)
        {
            return ContainerOperationResult.NotFound;
        }

        if (!reservations.CanWrite(id))
        {
            return ContainerOperationResult.NotReserved;
        }

        List<ItemDrop.ItemData> items = container.GetInventory().GetAllItems();
        if (stackIndex >= live.Contents.Count || stackIndex >= items.Count || live.Contents[stackIndex] != expected)
        {
            return ContainerOperationResult.ContainerChanged;
        }

        ItemDrop.ItemData item = items[stackIndex];
        if (!container.GetInventory().RemoveItem(item, count))
        {
            return ContainerOperationResult.ContainerChanged;
        }

        if (nextMove < plan.Moves.Count && plan.Moves[nextMove].Source == id)
        {
            nextMove++;
        }

        return ContainerOperationResult.Success;
    }

    private Container? Find(ContainerId id)
    {
        if (!containers.TryGetValue(id, out Container? container))
        {
            return null;
        }

        if (container == null || container.m_nview == null || !container.m_nview.IsValid() || container.m_inventory == null)
        {
            return null;
        }

        return container;
    }

    /// <summary>Clones the live source stack of the plan's next move when it matches the add; otherwise clones the item prefab.</summary>
    private ItemDrop.ItemData? CloneForMove(ContainerId destination, ItemStackSnapshot items)
    {
        // Only the selected stack is copied: a prefab default would lose durability, crafter and custom data (S-A-004).
        ItemDrop.ItemData? template = selectedIndex < 0 ? null : SelectedStack(items);
        selectedIndex = -1;
        if (template == null)
        {
            return null;
        }

        ItemDrop.ItemData clone = template.Clone();
        clone.m_stack = items.Count;
        clone.m_quality = items.Quality;
        if (clone.m_dropPrefab == null)
        {
            clone.m_dropPrefab = template.m_dropPrefab;
        }

        return clone;
    }

    private ItemDrop.ItemData? SelectedStack(ItemStackSnapshot items)
    {
        Container? container = Find(selectedSource);
        if (container == null)
        {
            return null;
        }

        List<ItemDrop.ItemData> all = container.GetInventory().GetAllItems();
        if (selectedIndex >= all.Count)
        {
            return null;
        }

        ItemDrop.ItemData item = all[selectedIndex];
        return item.m_quality == items.Quality && item.m_stack >= items.Count ? item : null;
    }

    internal static ItemDrop.ItemData? PrefabTemplate(string prefabName)
    {
        ObjectDB db = ObjectDB.instance;
        if (db == null)
        {
            return null;
        }

        GameObject prefab = db.GetItemPrefab(prefabName);
        if (prefab == null)
        {
            return null;
        }

        ItemDrop drop = prefab.GetComponent<ItemDrop>();
        if (drop == null || drop.m_itemData == null)
        {
            return null;
        }

        ItemDrop.ItemData template = drop.m_itemData;
        if (template.m_dropPrefab == null)
        {
            template.m_dropPrefab = prefab;
        }

        return template;
    }
}
