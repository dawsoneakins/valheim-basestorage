namespace BaseStorage.Plugin.Deposit;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.Organize;
using BaseStorage.Plugin.Reservations;
using BaseStorage.Plugin.World;

/// <summary>
/// The game-backed <see cref="IContainerLayer"/> of one Deposit (contract deposit-button-contracts.md, rule DB13). The chest side
/// delegates to the Organize layer; the inventory side is the source only: it is read fresh from the player, never reserved, never a
/// destination, and removed from by value. A chest add clones the live inventory item so durability, variant and crafter survive.
/// It never claims ownership, opens or closes a chest, or sends a message. Main thread only; Unity null checks use <c>==</c> (W4).
/// </summary>
internal sealed class GameDepositContainerLayer : IContainerLayer
{
    private readonly DepositPlan plan;
    private readonly IReadOnlyDictionary<ContainerId, Container> containers;
    private readonly ReservationDriver<Container> reservations;
    private readonly GameSnapshotSource snapshots;
    private readonly Player player;
    private readonly GameOrganizeContainerLayer chests;
    private int selectedIndex = -1;

    /// <summary>Creates the layer of one Deposit.</summary>
    /// <param name="plan">The Deposit plan that runs.</param>
    /// <param name="containers">The loaded container per chest id of the base.</param>
    /// <param name="reservations">The reservation driver.</param>
    /// <param name="snapshots">The snapshot adapter that maps a live container.</param>
    /// <param name="player">The local player whose inventory is the source.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal GameDepositContainerLayer(
        DepositPlan plan,
        IReadOnlyDictionary<ContainerId, Container> containers,
        ReservationDriver<Container> reservations,
        GameSnapshotSource snapshots,
        Player player)
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

        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        this.plan = plan;
        this.containers = containers;
        this.reservations = reservations;
        this.snapshots = snapshots;
        this.player = player;
        chests = new GameOrganizeContainerLayer(plan.Plan, containers, reservations, snapshots);
    }

    /// <inheritdoc />
    ContainerId IContainerLayer.PlayerInventoryId => plan.Player.Id;

    /// <inheritdoc />
    IReadOnlyList<ContainerSnapshot> IContainerLayer.GetContainers() => chests.GetContainers();

    /// <inheritdoc />
    ContainerSnapshot? IContainerLayer.GetContainer(ContainerId id) =>
        IsInventory(id) ? PlayerInventoryCapture.Capture(player, id)?.Container : chests.GetContainer(id);

    /// <inheritdoc />
    void IContainerLayer.RequestReservation(ContainerId id)
    {
        if (!IsInventory(id))
        {
            chests.RequestReservation(id);
        }
    }

    /// <inheritdoc />
    ReservationStatus IContainerLayer.GetReservationStatus(ContainerId id) =>
        IsInventory(id) ? ReservationStatus.Granted : chests.GetReservationStatus(id);

    /// <inheritdoc />
    void IContainerLayer.ReleaseReservation(ContainerId id)
    {
        if (!IsInventory(id))
        {
            chests.ReleaseReservation(id);
        }
    }

    /// <inheritdoc />
    ContainerOperationResult IContainerLayer.AddItems(ContainerId id, ItemStackSnapshot items)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (IsInventory(id))
        {
            return ContainerOperationResult.DestinationFull;
        }

        Container? container = FindChest(id);
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

        ItemDrop.ItemData? item = CloneFor(items);
        if (item == null)
        {
            return ContainerOperationResult.DestinationFull;
        }

        return container.GetInventory().AddItem(item) ? ContainerOperationResult.Success : ContainerOperationResult.DestinationFull;
    }

    /// <inheritdoc />
    void IContainerLayer.SelectSourceStack(ContainerId source, int stackIndex) =>
        selectedIndex = IsInventory(source) ? stackIndex : -1;

    /// <inheritdoc />
    ContainerOperationResult IContainerLayer.RemoveFromStack(ContainerId id, int stackIndex, ItemStackSnapshot expected, int count)
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

        if (!IsInventory(id))
        {
            return chests.RemoveFromStack(id, stackIndex, expected, count);
        }

        PlayerInventorySnapshot? live = PlayerInventoryCapture.Capture(player, id);
        if (live == null)
        {
            return ContainerOperationResult.NotFound;
        }

        Inventory inventory = player.GetInventory();
        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        if (stackIndex >= live.Container.Contents.Count || stackIndex >= items.Count || live.Container.Contents[stackIndex] != expected)
        {
            return ContainerOperationResult.ContainerChanged;
        }

        ItemDrop.ItemData item = items[stackIndex];
        return inventory.RemoveItem(item, count) ? ContainerOperationResult.Success : ContainerOperationResult.ContainerChanged;
    }

    private bool IsInventory(ContainerId id) => id == plan.Player.Id;

    private Container? FindChest(ContainerId id)
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

    /// <summary>Clones the exact selected inventory stack (S-A-004); a prefab default would lose durability, crafter and custom data.</summary>
    private ItemDrop.ItemData? CloneFor(ItemStackSnapshot items)
    {
        int index = selectedIndex;
        selectedIndex = -1;
        List<ItemDrop.ItemData> all = player.GetInventory().GetAllItems();
        if (index < 0 || index >= all.Count)
        {
            return null;
        }

        ItemDrop.ItemData template = all[index];
        if (template.m_quality != items.Quality || template.m_stack < items.Count)
        {
            return null;
        }

        ItemDrop.ItemData clone = template.Clone();
        clone.m_stack = items.Count;
        clone.m_quality = items.Quality;
        clone.m_equipped = false;
        if (clone.m_dropPrefab == null)
        {
            clone.m_dropPrefab = template.m_dropPrefab;
        }

        return clone;
    }
}
