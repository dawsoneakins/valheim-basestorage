namespace BaseStorage.Core.Reservations;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The per-chest registry of one requesting session (contract S-E005-001 section 6). It holds at most one non-terminal reservation
/// per chest. It has reference equality. It is not thread-safe; use it from one thread.
/// </summary>
/// <typeparam name="TOperation">The operation the reservations serve. It is carried unchanged and never inspected.</typeparam>
public sealed class ChestReservations<TOperation>
    where TOperation : notnull
{
    private readonly Dictionary<ContainerId, Reservation<TOperation>> byChest = new Dictionary<ContainerId, Reservation<TOperation>>();
    private readonly IClock clock;
    private readonly int reserveTimeoutMs;

    /// <summary>Creates an empty registry. It reads no clock.</summary>
    /// <param name="clock">The only time source, passed on to each reservation. It must not be null.</param>
    /// <param name="reserveTimeoutMs">The timeout in milliseconds, passed on to each reservation. It must be at least 1.</param>
    /// <exception cref="ArgumentNullException">Thrown when clock is null. ParamName is "clock".</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when reserveTimeoutMs is less than 1. ParamName is "reserveTimeoutMs".</exception>
    public ChestReservations(IClock clock, int reserveTimeoutMs)
    {
        if (clock is null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        if (reserveTimeoutMs < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(reserveTimeoutMs));
        }

        this.clock = clock;
        this.reserveTimeoutMs = reserveTimeoutMs;
    }

    /// <summary>Gets the non-terminal reservations, ordered by chest ascending. Each read returns a new snapshot list.</summary>
    public IReadOnlyList<Reservation<TOperation>> Active => Snapshot();

    /// <summary>
    /// Creates and stores a reservation for a chest that has no non-terminal reservation. It replaces a terminal one.
    /// </summary>
    /// <param name="chest">The chest to reserve. It must not be the none id.</param>
    /// <param name="operation">The operation the reservation serves. It must not be null.</param>
    /// <returns>The new reservation, or null when the chest already has a non-terminal reservation (nothing changes).</returns>
    /// <exception cref="ArgumentException">Thrown when chest is the none id. ParamName is "chest".</exception>
    /// <exception cref="ArgumentNullException">Thrown when operation is null. ParamName is "operation".</exception>
    public Reservation<TOperation>? Begin(ContainerId chest, TOperation operation)
    {
        if (chest.IsNone)
        {
            throw new ArgumentException("The chest must not be the none id.", nameof(chest));
        }

        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        if (byChest.TryGetValue(chest, out Reservation<TOperation>? existing) && !existing.IsTerminal)
        {
            return null;
        }

        var created = new Reservation<TOperation>(chest, operation, clock, reserveTimeoutMs);
        byChest[chest] = created;
        return created;
    }

    /// <summary>Gets the latest reservation stored for a chest, terminal or not. Never throws.</summary>
    /// <param name="chest">The chest to look up.</param>
    /// <returns>The stored reservation, or null when there is none.</returns>
    public Reservation<TOperation>? Find(ContainerId chest) =>
        byChest.TryGetValue(chest, out Reservation<TOperation>? found) ? found : null;

    /// <summary>Applies an event to the stored reservation for a chest. Never throws.</summary>
    /// <param name="chest">The chest whose reservation receives the event.</param>
    /// <param name="trigger">The event to apply.</param>
    /// <returns>False when the chest has no stored reservation; otherwise the result of that reservation's Apply.</returns>
    public bool Apply(ContainerId chest, ReservationEvent trigger) =>
        byChest.TryGetValue(chest, out Reservation<TOperation>? found) && found.Apply(trigger);

    /// <summary>Applies an event to every non-terminal reservation, in chest ascending order. Intended for session-wide events.</summary>
    /// <param name="trigger">The event to apply.</param>
    /// <returns>The number of reservations that accepted the event.</returns>
    public int ApplyToAll(ReservationEvent trigger)
    {
        int accepted = 0;
        foreach (Reservation<TOperation> reservation in Snapshot())
        {
            if (reservation.Apply(trigger))
            {
                accepted++;
            }
        }

        return accepted;
    }

    /// <summary>Calls CheckTimeout on every non-terminal reservation, in chest ascending order.</summary>
    /// <returns>The reservations that this call moved to Failed, in chest ascending order. The list is empty when there are none.</returns>
    public IReadOnlyList<Reservation<TOperation>> CheckTimeouts()
    {
        var failed = new List<Reservation<TOperation>>();
        foreach (Reservation<TOperation> reservation in Snapshot())
        {
            if (reservation.CheckTimeout())
            {
                failed.Add(reservation);
            }
        }

        return new ReadOnlyCollection<Reservation<TOperation>>(failed);
    }

    /// <summary>Forgets every terminal reservation. Non-terminal reservations are never removed.</summary>
    /// <returns>The number of reservations removed.</returns>
    public int RemoveTerminal()
    {
        var doomed = new List<ContainerId>();
        foreach (KeyValuePair<ContainerId, Reservation<TOperation>> pair in byChest)
        {
            if (pair.Value.IsTerminal)
            {
                doomed.Add(pair.Key);
            }
        }

        foreach (ContainerId id in doomed)
        {
            byChest.Remove(id);
        }

        return doomed.Count;
    }

    private IReadOnlyList<Reservation<TOperation>> Snapshot()
    {
        var list = new List<Reservation<TOperation>>();
        foreach (Reservation<TOperation> reservation in byChest.Values)
        {
            if (!reservation.IsTerminal)
            {
                list.Add(reservation);
            }
        }

        list.Sort((a, b) => a.Chest.CompareTo(b.Chest));
        return new ReadOnlyCollection<Reservation<TOperation>>(list);
    }
}
