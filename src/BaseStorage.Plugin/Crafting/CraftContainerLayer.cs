namespace BaseStorage.Plugin.Crafting;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.Reservations;

/// <summary>
/// The container layer the consumption executor drives for a craft (contract craft-from-base 4). It reserves chests through the
/// reservation driver under the "craft" operation and writes through the world port only while the reservation allows it (RA2).
/// A reservation of another operation is never used, released or written. Game-free.
/// </summary>
/// <typeparam name="TChest">The chest type of the game.</typeparam>
internal sealed class CraftContainerLayer<TChest> : IContainerLayer
    where TChest : class
{
    /// <summary>The operation label of craft reservations.</summary>
    internal const string Operation = "craft";

    private static readonly IReadOnlyList<ContainerSnapshot> NoContainers = new EmptyContainers();

    private readonly ICraftWorld<TChest> world;
    private readonly ReservationDriver<TChest> driver;
    private readonly HashSet<ContainerId> released = new HashSet<ContainerId>();

    /// <summary>Creates the layer.</summary>
    /// <param name="world">The world port.</param>
    /// <param name="driver">The reservation driver.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    internal CraftContainerLayer(ICraftWorld<TChest> world, ReservationDriver<TChest> driver)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    /// <inheritdoc />
    public ContainerId PlayerInventoryId => world.InventoryId;

    /// <inheritdoc />
    public IReadOnlyList<ContainerSnapshot> GetContainers() => NoContainers;

    /// <inheritdoc />
    public ContainerSnapshot? GetContainer(ContainerId id) => world.Read(id);

    /// <inheritdoc />
    public void RequestReservation(ContainerId id)
    {
        if (id == PlayerInventoryId)
        {
            return;
        }

        TChest? chest = world.FindChest(id);
        if (chest is null)
        {
            return;
        }

        released.Remove(id);
        driver.Begin(chest, Operation);
    }

    /// <inheritdoc />
    public ReservationStatus GetReservationStatus(ContainerId id)
    {
        if (id == PlayerInventoryId)
        {
            return ReservationStatus.Granted;
        }

        Reservation<string>? reservation = driver.Find(id);
        if (reservation is null)
        {
            return ReservationStatus.None;
        }

        if (!IsOurs(reservation))
        {
            return reservation.IsTerminal ? ReservationStatus.None : ReservationStatus.Denied;
        }

        ReservationState state = reservation.State;
        if (reservation.IsTerminal && released.Contains(id))
        {
            return ReservationStatus.None;
        }

        if (state == ReservationState.Granted)
        {
            driver.BeginWrite(id);
            state = reservation.State;
        }

        switch (state)
        {
            case ReservationState.Requesting:
                return ReservationStatus.Pending;
            case ReservationState.Granted:
            case ReservationState.Writing:
                return driver.CanWrite(id) ? ReservationStatus.Granted : ReservationStatus.Lost;
            case ReservationState.Failed:
                return reservation.Failure == ReservationFailure.Denied || reservation.Failure == ReservationFailure.InUseByOther
                    ? ReservationStatus.Denied
                    : ReservationStatus.Lost;
            default:
                return ReservationStatus.None;
        }
    }

    /// <inheritdoc />
    public void ReleaseReservation(ContainerId id)
    {
        if (id == PlayerInventoryId)
        {
            return;
        }

        Reservation<string>? reservation = driver.Find(id);
        if (reservation is null || !IsOurs(reservation))
        {
            return;
        }

        driver.Release(id);
        released.Add(id);
    }

    /// <inheritdoc />
    public ContainerOperationResult AddItems(ContainerId id, ItemStackSnapshot items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        ContainerOperationResult guard = Guard(id);
        return guard != ContainerOperationResult.Success ? guard : world.PutBack(id, items);
    }

    /// <inheritdoc/>
    public void SelectSourceStack(ContainerId source, int stackIndex)
    {
    }

    /// <inheritdoc />
    public ContainerOperationResult RemoveFromStack(ContainerId id, int stackIndex, ItemStackSnapshot expected, int count)
    {
        if (expected is null)
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

        ContainerOperationResult guard = Guard(id);
        return guard != ContainerOperationResult.Success ? guard : world.Take(id, stackIndex, expected, count);
    }

    private static bool IsOurs(Reservation<string> reservation) =>
        string.Equals(reservation.Operation, Operation, StringComparison.Ordinal);

    private ContainerOperationResult Guard(ContainerId id)
    {
        if (id == PlayerInventoryId)
        {
            return ContainerOperationResult.Success;
        }

        Reservation<string>? reservation = driver.Find(id);
        if (reservation is not null && IsOurs(reservation) && reservation.State == ReservationState.Granted)
        {
            driver.BeginWrite(id);
        }

        if (reservation is null || !IsOurs(reservation) || !driver.CanWrite(id))
        {
            return ContainerOperationResult.NotReserved;
        }

        return world.FindChest(id) is null ? ContainerOperationResult.NotFound : ContainerOperationResult.Success;
    }

    private sealed class EmptyContainers : IReadOnlyList<ContainerSnapshot>
    {
        public int Count => 0;

        public ContainerSnapshot this[int index] => throw new ArgumentOutOfRangeException(nameof(index));

        public IEnumerator<ContainerSnapshot> GetEnumerator()
        {
            yield break;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
