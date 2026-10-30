namespace BaseStorage.Core.ChestCategories;

using System;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Ui;

/// <summary>
/// The pure decisions of the chest category write (contract S-E004-003 section 2.4): what a pending write does for a
/// reservation state, how a reservation failure reads as an outcome, how long a write may wait, and which strings-table keys
/// tell the player the result. No clock, no I/O, no logging and no static mutable state.
/// </summary>
public static class CategoryWriteRules
{
    /// <summary>Gets the deadline of a pending write: twice the reservation timeout after its start, so the reservation's own timeout always fires first.</summary>
    /// <param name="startedAt">The clock reading when the write was asked for.</param>
    /// <param name="reserveTimeoutMs">The reservation timeout in milliseconds. Values below 1 count as 1.</param>
    /// <returns>The deadline. It never throws.</returns>
    public static TimeSpan Deadline(TimeSpan startedAt, int reserveTimeoutMs) =>
        startedAt + TimeSpan.FromTicks(2L * Math.Max(1, reserveTimeoutMs) * TimeSpan.TicksPerMillisecond);

    /// <summary>Gets what a pending write does for the state of its reservation.</summary>
    /// <param name="state">The reservation state, or null when no reservation is stored.</param>
    /// <returns>Wait for Requesting, Write for Granted, Fail for every other state, for null and for an undefined value.</returns>
    public static PendingWriteAction ActionFor(ReservationState? state)
    {
        switch (state)
        {
            case ReservationState.Requesting:
                return PendingWriteAction.Wait;
            case ReservationState.Granted:
                return PendingWriteAction.Write;
            default:
                return PendingWriteAction.Fail;
        }
    }

    /// <summary>Maps a reservation failure to the outcome the player is told.</summary>
    /// <param name="failure">The reservation failure.</param>
    /// <returns>Denied, InUse or Timeout for the matching failure; Unavailable for every other value, None included.</returns>
    public static CategoryWriteOutcome OutcomeFor(ReservationFailure failure)
    {
        switch (failure)
        {
            case ReservationFailure.Denied:
                return CategoryWriteOutcome.Denied;
            case ReservationFailure.InUseByOther:
                return CategoryWriteOutcome.InUse;
            case ReservationFailure.Timeout:
                return CategoryWriteOutcome.Timeout;
            default:
                return CategoryWriteOutcome.Unavailable;
        }
    }

    /// <summary>Gets the outcome of a pending write whose action is Fail.</summary>
    /// <param name="state">The reservation state, or null when no reservation is stored.</param>
    /// <param name="failure">The reservation failure.</param>
    /// <returns><see cref="OutcomeFor"/> of the failure when the state is Failed; otherwise Unavailable.</returns>
    public static CategoryWriteOutcome FailureOutcome(ReservationState? state, ReservationFailure failure) =>
        state == ReservationState.Failed ? OutcomeFor(failure) : CategoryWriteOutcome.Unavailable;

    /// <summary>Gets the status a caller sees for an outcome reached during its call.</summary>
    /// <param name="outcome">The outcome.</param>
    /// <returns>Applied for Applied; Refused for everything else.</returns>
    public static CategoryWriteStatus StatusOf(CategoryWriteOutcome outcome) =>
        outcome == CategoryWriteOutcome.Applied ? CategoryWriteStatus.Applied : CategoryWriteStatus.Refused;

    /// <summary>Gets the strings-table key of the notice for an outcome.</summary>
    /// <param name="outcome">The outcome.</param>
    /// <returns><see cref="StringKeys.MessageCategorySet"/> for Applied; <see cref="StringKeys.MessageCategoryRefused"/> otherwise.</returns>
    public static string NoticeKey(CategoryWriteOutcome outcome) =>
        outcome == CategoryWriteOutcome.Applied ? StringKeys.MessageCategorySet : StringKeys.MessageCategoryRefused;

    /// <summary>Gets the strings-table key of the reason fragment for a refusal.</summary>
    /// <param name="outcome">The outcome.</param>
    /// <returns>The reason key; null for Applied. An undefined value gives the Unavailable reason.</returns>
    public static string? ReasonKey(CategoryWriteOutcome outcome)
    {
        switch (outcome)
        {
            case CategoryWriteOutcome.Applied:
                return null;
            case CategoryWriteOutcome.Denied:
                return StringKeys.CategoryRefusalDenied;
            case CategoryWriteOutcome.InUse:
                return StringKeys.CategoryRefusalInUse;
            case CategoryWriteOutcome.Timeout:
                return StringKeys.CategoryRefusalTimeout;
            default:
                return StringKeys.CategoryRefusalUnavailable;
        }
    }
}
