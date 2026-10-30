namespace BaseStorage.Core.Tests.Simulation;

using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;

/// <summary>
/// One peer's <see cref="IContainerLayer"/> over the shared <see cref="SimulatedWorld"/>: the peer's own inventory, the chests with
/// the in-use flag as the peer observes it, reservations routed through the <see cref="ReservationAuthority"/>, and writes gated by
/// the authority. It is what the Core planners and <c>OrganizeExecutor</c> talk to, exactly as the Plugin layer would. Not a test row.
/// </summary>
internal sealed class PeerContainerLayer : IContainerLayer
{
    private readonly SimulatedWorld world;
    private readonly int peer;
    private bool retired;

    /// <summary>Creates the layer of one peer.</summary>
    /// <param name="world">The shared world.</param>
    /// <param name="peer">The peer index.</param>
    internal PeerContainerLayer(SimulatedWorld world, int peer)
    {
        this.world = world;
        this.peer = peer;
    }

    /// <summary>Gets the number of reservation and write calls made after <see cref="Retire"/>: a dead peer must make none.</summary>
    internal int CallsAfterRetire { get; private set; }

    /// <summary>Marks the layer as belonging to a peer that disconnected. Calls still pass through, but are counted.</summary>
    internal void Retire() => retired = true;

    /// <inheritdoc />
    public ContainerId PlayerInventoryId => world.InventoryIdOf(peer);

    /// <inheritdoc />
    public IReadOnlyList<ContainerSnapshot> GetContainers() => world.ChestsFor(peer);

    /// <inheritdoc />
    public ContainerSnapshot? GetContainer(ContainerId id) => world.GetFor(peer, id);

    /// <inheritdoc />
    public void RequestReservation(ContainerId id)
    {
        Touch();
        if (id != PlayerInventoryId && world.HasChest(id))
        {
            world.Authority.Request(peer, id);
        }
    }

    /// <inheritdoc />
    public ReservationStatus GetReservationStatus(ContainerId id)
    {
        if (id == PlayerInventoryId)
        {
            return ReservationStatus.Granted;
        }

        return world.HasChest(id) ? world.Authority.Status(peer, id) : ReservationStatus.None;
    }

    /// <inheritdoc />
    public void ReleaseReservation(ContainerId id)
    {
        Touch();
        if (id != PlayerInventoryId && world.HasChest(id))
        {
            world.Authority.Release(peer, id);
        }
    }

    /// <inheritdoc />
    public ContainerOperationResult AddItems(ContainerId id, ItemStackSnapshot items)
    {
        Touch();
        return world.Add(peer, id, items);
    }

    /// <inheritdoc/>
    public void SelectSourceStack(ContainerId source, int stackIndex)
    {
    }

    /// <inheritdoc />
    public ContainerOperationResult RemoveFromStack(ContainerId id, int stackIndex, ItemStackSnapshot expected, int count)
    {
        Touch();
        return world.Remove(peer, id, stackIndex, expected, count);
    }

    private void Touch()
    {
        if (retired)
        {
            CallsAfterRetire++;
        }
    }
}
