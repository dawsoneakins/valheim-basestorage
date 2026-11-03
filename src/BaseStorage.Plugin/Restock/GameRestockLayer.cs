namespace BaseStorage.Plugin.Restock;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Restock.Run;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.Reservations;

/// <summary>
/// The game-backed <see cref="IRestockLayer"/> of one Restock (contract restock-run-contracts.md, P5). The chest members drive the
/// reservation exactly as the Organize layer does; the player side works on the captured inventory slots only. A top-up clones the live
/// stack and adds the clone to that exact slot, so it never lands in another stack (constraint 9), and a chest is only touched after the
/// core has re-read the slot. Every comparison is by value, identity included. Main thread only; Unity null checks use <c>==</c> (W4).
/// </summary>
internal sealed class GameRestockLayer : IRestockLayer
{
    private readonly IReadOnlyDictionary<ContainerId, Container> containers;
    private readonly IReadOnlyDictionary<ContainerId, ContainerSnapshot> listed;
    private readonly ReservationDriver<Container> reservations;
    private readonly Player? player;
    private readonly IReadOnlyList<Vector2i> slots;
    private readonly HashSet<ContainerId> busy = new HashSet<ContainerId>();

    /// <summary>Creates the layer of one Restock.</summary>
    /// <param name="containers">The loaded container per chest id of the base.</param>
    /// <param name="listed">The chests as the last chest re-read listed them, which supply each chest's id, position and flags.</param>
    /// <param name="reservations">The reservation driver.</param>
    /// <param name="player">The local player whose inventory is topped up.</param>
    /// <param name="slots">The inventory slot of each carried stack of the plan, by carried index.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument other than <paramref name="player"/> is null.</exception>
    internal GameRestockLayer(
        IReadOnlyDictionary<ContainerId, Container> containers,
        IReadOnlyDictionary<ContainerId, ContainerSnapshot> listed,
        ReservationDriver<Container> reservations,
        Player? player,
        IReadOnlyList<Vector2i> slots)
    {
        this.containers = containers ?? throw new ArgumentNullException(nameof(containers));
        this.listed = listed ?? throw new ArgumentNullException(nameof(listed));
        this.reservations = reservations ?? throw new ArgumentNullException(nameof(reservations));
        this.player = player;
        this.slots = slots ?? throw new ArgumentNullException(nameof(slots));
    }

    /// <inheritdoc />
    public void RequestReservation(ContainerId chest)
    {
        Container? container = Find(chest);
        if (container == null)
        {
            return;
        }

        Reservation<string>? existing = reservations.Find(chest);
        if (existing != null && !existing.IsTerminal)
        {
            return;
        }

        busy.Remove(chest);
        if (reservations.Begin(container, OrganizeLayerRules.Operation) == null)
        {
            busy.Add(chest);
        }
    }

    /// <inheritdoc />
    public ReservationStatus GetReservationStatus(ContainerId chest)
    {
        Reservation<string>? reservation = reservations.Find(chest);
        if (reservation != null && reservation.State == ReservationState.Granted)
        {
            reservations.BeginWrite(chest);
        }

        return OrganizeLayerRules.StatusOf(reservation?.State, reservation == null ? ReservationFailure.None : reservation.Failure, busy.Contains(chest));
    }

    /// <inheritdoc />
    public void ReleaseReservation(ContainerId chest)
    {
        busy.Remove(chest);
        reservations.Release(chest);
    }

    /// <inheritdoc />
    public ContainerSnapshot? GetContainer(ContainerId chest)
    {
        return listed.TryGetValue(chest, out ContainerSnapshot? meta) ? RestockChestCapture.Read(meta, Find(chest)) : null;
    }

    /// <inheritdoc />
    public ContainerOperationResult RemoveFromStack(ContainerId chest, int stackIndex, ItemStackSnapshot expected, int count)
    {
        Container? container = Find(chest);
        if (container == null)
        {
            return ContainerOperationResult.NotFound;
        }

        if (!reservations.CanWrite(chest))
        {
            return ContainerOperationResult.NotReserved;
        }

        if (expected == null || count < 1 || count > expected.Count)
        {
            return ContainerOperationResult.ContainerChanged;
        }

        // The game list shifts when a stack empties, so the planned index is tried first and the first stack equal by value is the fallback.
        Inventory inventory = container.GetInventory();
        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        ItemDrop.ItemData? found = null;
        if (items != null && stackIndex >= 0 && stackIndex < items.Count && Same(items[stackIndex], expected))
        {
            found = items[stackIndex];
        }

        for (int i = 0; found == null && items != null && i < items.Count; i++)
        {
            if (Same(items[i], expected))
            {
                found = items[i];
            }
        }

        if (found == null)
        {
            return ContainerOperationResult.ContainerChanged;
        }

        return inventory.RemoveItem(found, count) ? ContainerOperationResult.Success : ContainerOperationResult.ContainerChanged;
    }

    /// <inheritdoc />
    public ItemStackSnapshot? ReadCarried(int carriedIndex)
    {
        ItemDrop.ItemData? item = ItemAt(carriedIndex);
        return Snapshot(item);
    }

    /// <inheritdoc />
    public bool AddToCarried(int carriedIndex, ItemStackSnapshot expected, int count)
    {
        ItemDrop.ItemData? target = ItemAt(carriedIndex);
        if (target == null || expected == null || count < 1 || !Same(target, expected))
        {
            return false;
        }

        // The game's AddItem(item, pos) fills the first stack in list order that matches name, quality and world level and has room,
        // not the stack at pos. Clamp to the target's room and refuse unless the target is that first stack, so only the target is written.
        count = Math.Min(count, target.m_shared.m_maxStackSize - target.m_stack);
        if (count < 1)
        {
            return false;
        }

        foreach (ItemDrop.ItemData other in player!.GetInventory().GetAllItems())
        {
            if (other.m_shared.m_name == target.m_shared.m_name
                && other.m_quality == target.m_quality
                && other.m_worldLevel == target.m_worldLevel
                && other.m_stack < other.m_shared.m_maxStackSize)
            {
                if (!ReferenceEquals(other, target))
                {
                    return false;
                }

                break;
            }
        }

        ItemDrop.ItemData clone = target.Clone();
        clone.m_stack = count;
        return player!.GetInventory().AddItem(clone, target.m_gridPos);
    }

    /// <inheritdoc />
    public bool RemoveFromCarried(int carriedIndex, ItemStackSnapshot expected, int count)
    {
        ItemDrop.ItemData? target = ItemAt(carriedIndex);
        if (target == null || expected == null || count < 1 || count > expected.Count || !Same(target, expected))
        {
            return false;
        }

        return player!.GetInventory().RemoveItem(target, count);
    }

    private static ItemStackSnapshot? Snapshot(ItemDrop.ItemData? item)
    {
        if (!RestockChestCapture.IsMappable(item))
        {
            return null;
        }

        try
        {
            return new ItemStackSnapshot(item!.m_dropPrefab.name, item.m_stack, item.m_quality, item.m_shared.m_maxStackSize, GameItemIdentity.Of(item));
        }
        catch (ArgumentException)
        {
            // A value the Core rejects (for example a non-finite durability): the slot reads as unreadable, so the core treats it as doubt.
            return null;
        }
    }

    private static bool Same(ItemDrop.ItemData? item, ItemStackSnapshot expected)
    {
        ItemStackSnapshot? live = Snapshot(item);
        return live != null && live == expected;
    }

    private ItemDrop.ItemData? ItemAt(int carriedIndex)
    {
        if (player == null || carriedIndex < 0 || carriedIndex >= slots.Count)
        {
            return null;
        }

        Inventory inventory = player.GetInventory();
        if (inventory == null)
        {
            return null;
        }

        Vector2i slot = slots[carriedIndex];
        return inventory.GetItemAt(slot.x, slot.y);
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
}
