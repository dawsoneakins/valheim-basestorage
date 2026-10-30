namespace BaseStorage.Core.Ui;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Organize;

/// <summary>
/// What one Organize run did, as the end-of-run summary line needs it (ui-strings contract 10.2): the stacks moved, the chests
/// skipped with their reasons, the stacks that had no room, and whether the run stopped early. The executor fills it in; it
/// is immutable and holds no plan or game state.
/// </summary>
public sealed class OrganizeRunReport
{
    /// <summary>Creates a report.</summary>
    /// <param name="movedStacks">The number of planned moves that ran, at least 0.</param>
    /// <param name="skipped">The skipped chests, one entry each; not null. The list is copied.</param>
    /// <param name="noRoomStacks">The number of stacks that had no room, at least 0.</param>
    /// <param name="stoppedEarly">True when the run stopped before the plan was complete.</param>
    /// <param name="stopReason">The already-resolved display text of the stop reason, or null or blank when there is none. Ignored unless <paramref name="stoppedEarly"/> is true.</param>
    /// <exception cref="ArgumentNullException"><paramref name="skipped"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="movedStacks"/> or <paramref name="noRoomStacks"/> is negative.</exception>
    public OrganizeRunReport(int movedStacks, IReadOnlyList<OrganizeSkipEntry> skipped, int noRoomStacks, bool stoppedEarly, string? stopReason)
    {
        if (movedStacks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(movedStacks), movedStacks, "The count must not be negative.");
        }

        if (skipped is null)
        {
            throw new ArgumentNullException(nameof(skipped));
        }

        if (noRoomStacks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(noRoomStacks), noRoomStacks, "The count must not be negative.");
        }

        var copy = new OrganizeSkipEntry[skipped.Count];
        for (int i = 0; i < copy.Length; i++)
        {
            copy[i] = skipped[i];
        }

        MovedStacks = movedStacks;
        Skipped = new ReadOnlyCollection<OrganizeSkipEntry>(copy);
        NoRoomStacks = noRoomStacks;
        StoppedEarly = stoppedEarly;
        StopReason = stoppedEarly && !string.IsNullOrWhiteSpace(stopReason) ? stopReason : null;
    }

    /// <summary>Gets the number of planned moves that ran.</summary>
    public int MovedStacks { get; }

    /// <summary>Gets the skipped chests, one entry each, read-only.</summary>
    public IReadOnlyList<OrganizeSkipEntry> Skipped { get; }

    /// <summary>Gets the number of stacks that had no room.</summary>
    public int NoRoomStacks { get; }

    /// <summary>Gets a value indicating whether the run stopped before the plan was complete.</summary>
    public bool StoppedEarly { get; }

    /// <summary>Gets the display text of the stop reason; null when the run did not stop early or gave no reason.</summary>
    public string? StopReason { get; }

    /// <summary>Builds a report from a plan: the plan's skipped chests and no-room count, with the run's own moved count and stop state.</summary>
    /// <param name="plan">The plan that ran; not null.</param>
    /// <param name="movedStacks">The number of planned moves that ran, at least 0.</param>
    /// <param name="stoppedEarly">True when the run stopped before the plan was complete.</param>
    /// <param name="stopReason">The already-resolved display text of the stop reason, or null when there is none.</param>
    /// <returns>A report whose <see cref="Skipped"/> mirrors <see cref="OrganizePlan.Skipped"/> and whose <see cref="NoRoomStacks"/> is the plan's no-room entry count.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plan"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="movedStacks"/> is negative.</exception>
    public static OrganizeRunReport FromPlan(OrganizePlan plan, int movedStacks, bool stoppedEarly, string? stopReason)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        var entries = new OrganizeSkipEntry[plan.Skipped.Count];
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i] = OrganizeSkipEntry.From(plan.Skipped[i]);
        }

        return new OrganizeRunReport(movedStacks, entries, plan.NoRoom.Count, stoppedEarly, stopReason);
    }
}
