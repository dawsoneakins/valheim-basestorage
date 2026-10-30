namespace BaseStorage.Core.Reservations;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// The reservation transition table (contract S-E005-001 sections 3.1 and 3.4) and the state predicates. The table is the only
/// source of legality. It is static and read-only, and not thread-safe; use it from one thread.
/// </summary>
public static class ReservationTransitions
{
    private static readonly ReadOnlyCollection<ReservationTransition> AllRows = new ReadOnlyCollection<ReservationTransition>(
        new[]
        {
            new ReservationTransition(ReservationState.Requesting, ReservationEvent.Grant, ReservationState.Granted, ReservationFailure.None),
            new ReservationTransition(ReservationState.Requesting, ReservationEvent.Deny, ReservationState.Failed, ReservationFailure.Denied),
            new ReservationTransition(ReservationState.Requesting, ReservationEvent.Cancel, ReservationState.Failed, ReservationFailure.Cancelled),
            new ReservationTransition(ReservationState.Requesting, ReservationEvent.RequesterDisconnected, ReservationState.Failed, ReservationFailure.RequesterDisconnected),
            new ReservationTransition(ReservationState.Requesting, ReservationEvent.OwnerLost, ReservationState.Failed, ReservationFailure.OwnerLost),
            new ReservationTransition(ReservationState.Requesting, ReservationEvent.InUseByOther, ReservationState.Failed, ReservationFailure.InUseByOther),
            new ReservationTransition(ReservationState.Granted, ReservationEvent.BeginWrite, ReservationState.Writing, ReservationFailure.None),
            new ReservationTransition(ReservationState.Granted, ReservationEvent.BeginRelease, ReservationState.Releasing, ReservationFailure.None),
            new ReservationTransition(ReservationState.Granted, ReservationEvent.RequesterDisconnected, ReservationState.Failed, ReservationFailure.RequesterDisconnected),
            new ReservationTransition(ReservationState.Granted, ReservationEvent.OwnerLost, ReservationState.Failed, ReservationFailure.OwnerLost),
            new ReservationTransition(ReservationState.Granted, ReservationEvent.InUseByOther, ReservationState.Failed, ReservationFailure.InUseByOther),
            new ReservationTransition(ReservationState.Writing, ReservationEvent.BeginRelease, ReservationState.Releasing, ReservationFailure.None),
            new ReservationTransition(ReservationState.Writing, ReservationEvent.RequesterDisconnected, ReservationState.Failed, ReservationFailure.RequesterDisconnected),
            new ReservationTransition(ReservationState.Writing, ReservationEvent.OwnerLost, ReservationState.Failed, ReservationFailure.OwnerLost),
            new ReservationTransition(ReservationState.Writing, ReservationEvent.InUseByOther, ReservationState.Failed, ReservationFailure.InUseByOther),
            new ReservationTransition(ReservationState.Releasing, ReservationEvent.CompleteRelease, ReservationState.Completed, ReservationFailure.None),
            new ReservationTransition(ReservationState.Releasing, ReservationEvent.RequesterDisconnected, ReservationState.Failed, ReservationFailure.RequesterDisconnected),
            new ReservationTransition(ReservationState.Releasing, ReservationEvent.OwnerLost, ReservationState.Failed, ReservationFailure.OwnerLost),
            new ReservationTransition(ReservationState.Releasing, ReservationEvent.InUseByOther, ReservationState.Failed, ReservationFailure.InUseByOther),
        });

    /// <summary>Gets the 19 legal rows in contracted order: From ascending, then Trigger ascending. The same read-only instance is returned on every read.</summary>
    public static IReadOnlyList<ReservationTransition> All => AllRows;

    /// <summary>Looks up the legal row for a from state and a trigger event. An undefined value or an illegal pair gives false. Never throws.</summary>
    /// <param name="from">The current state.</param>
    /// <param name="trigger">The event to apply.</param>
    /// <param name="transition">When the method returns true, the row found. Otherwise default(ReservationTransition).</param>
    /// <returns>True when the pair is a legal row; otherwise false.</returns>
    public static bool TryGet(ReservationState from, ReservationEvent trigger, out ReservationTransition transition)
    {
        for (int i = 0; i < AllRows.Count; i++)
        {
            ReservationTransition row = AllRows[i];
            if (row.From == from && row.Trigger == trigger)
            {
                transition = row;
                return true;
            }
        }

        transition = default;
        return false;
    }

    /// <summary>Determines whether a state is terminal. Only Completed and Failed are. An undefined value gives false.</summary>
    /// <param name="state">The state to test.</param>
    /// <returns>True for Completed and Failed; otherwise false.</returns>
    public static bool IsTerminal(ReservationState state) =>
        state == ReservationState.Completed || state == ReservationState.Failed;

    /// <summary>Determines whether a state holds the chest. Only Granted, Writing and Releasing do. An undefined value gives false.</summary>
    /// <param name="state">The state to test.</param>
    /// <returns>True for Granted, Writing and Releasing; otherwise false.</returns>
    public static bool HoldsChest(ReservationState state) =>
        state == ReservationState.Granted || state == ReservationState.Writing || state == ReservationState.Releasing;

    internal static bool IsDefined(ReservationState value) =>
        value >= ReservationState.Requesting && value <= ReservationState.Failed;

    internal static bool IsDefined(ReservationEvent value) =>
        value >= ReservationEvent.Grant && value <= ReservationEvent.InUseByOther;

    internal static bool IsDefined(ReservationFailure value) =>
        value >= ReservationFailure.None && value <= ReservationFailure.InUseByOther;
}
