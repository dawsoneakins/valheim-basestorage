namespace BaseStorage.Core.Organize.Execution;

using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The counts of one execution for the end-of-run summary (contract 3.2): status and stop reason, what moved, what the plan
/// skipped and why, and what had no room. Immutable; only <see cref="OrganizeExecutor.Result"/> creates instances.
/// </summary>
public sealed class OrganizeExecutionResult
{
    private readonly OrganizePlan plan;

    /// <summary>Creates a result from the executor's counters.</summary>
    /// <param name="plan">The plan executed.</param>
    /// <param name="status">The status at the time of the read.</param>
    /// <param name="stopReason">The stop reason; None unless the status is Stopped.</param>
    /// <param name="failedChest">The chest that caused the stop; default when none.</param>
    /// <param name="failedMove">The move being executed when a write failed; otherwise null.</param>
    /// <param name="movesCompleted">The planned moves whose add and remove both succeeded.</param>
    /// <param name="stacksMoved">The distinct source stacks with at least one completed move.</param>
    /// <param name="itemsMoved">The sum of Count over the completed moves.</param>
    /// <param name="chestsPlanned">The number of batches.</param>
    /// <param name="chestsCompleted">The batches that completed and released.</param>
    /// <param name="unrecoveredItems">The items a failed rollback left in the destination.</param>
    internal OrganizeExecutionResult(
        OrganizePlan plan,
        ExecutionStatus status,
        ExecutionStopReason stopReason,
        ContainerId failedChest,
        PlannedMove? failedMove,
        int movesCompleted,
        int stacksMoved,
        long itemsMoved,
        int chestsPlanned,
        int chestsCompleted,
        long unrecoveredItems)
    {
        this.plan = plan;
        Status = status;
        StopReason = stopReason;
        FailedChest = failedChest;
        FailedMove = failedMove;
        MovesCompleted = movesCompleted;
        StacksMoved = stacksMoved;
        ItemsMoved = itemsMoved;
        ChestsPlanned = chestsPlanned;
        ChestsCompleted = chestsCompleted;
        UnrecoveredItems = unrecoveredItems;

        long noRoomItems = 0;
        for (var i = 0; i < plan.NoRoom.Count; i++)
        {
            noRoomItems += plan.NoRoom[i].Count;
        }

        NoRoomItems = noRoomItems;
    }

    /// <summary>Gets the status at the time of the read.</summary>
    public ExecutionStatus Status { get; }

    /// <summary>Gets the stop reason; None unless <see cref="Status"/> is Stopped.</summary>
    public ExecutionStopReason StopReason { get; }

    /// <summary>Gets the chest whose reservation, verification or write caused the stop; the default id when none (not stopped, or aborted).</summary>
    public ContainerId FailedChest { get; }

    /// <summary>Gets the move that was being executed when a write failed; null for reservation, verification, abort and not-stopped cases.</summary>
    public PlannedMove? FailedMove { get; }

    /// <summary>Gets the number of planned moves whose add and remove both succeeded.</summary>
    public int MovesCompleted { get; }

    /// <summary>Gets the number of planned moves not completed: the plan's move count minus <see cref="MovesCompleted"/>.</summary>
    public int MovesRemaining => plan.Moves.Count - MovesCompleted;

    /// <summary>Gets the number of distinct source stacks (source chest and input index) with at least one completed move.</summary>
    public int StacksMoved { get; }

    /// <summary>Gets the sum of Count over the completed moves.</summary>
    public long ItemsMoved { get; }

    /// <summary>Gets the number of batches: distinct source chests with at least one move.</summary>
    public int ChestsPlanned { get; }

    /// <summary>Gets the number of batches whose every move completed and whose reservations were released.</summary>
    public int ChestsCompleted { get; }

    /// <summary>Gets the items a failed rollback left in the destination; 0 unless <see cref="StopReason"/> is RollbackFailed.</summary>
    public long UnrecoveredItems { get; }

    /// <summary>Gets the number of no-room entries of the plan.</summary>
    public int NoRoomStacks => plan.NoRoom.Count;

    /// <summary>Gets the sum of Count over the plan's no-room entries.</summary>
    public long NoRoomItems { get; }

    /// <summary>Gets the number of skipped chests of the plan.</summary>
    public int SkippedChests => plan.Skipped.Count;

    /// <summary>Gets the plan's skipped chests, the same instance as <c>Plan.Skipped</c>.</summary>
    public IReadOnlyList<SkippedChest> Skipped => plan.Skipped;

    /// <summary>Counts the skipped chests of one kind.</summary>
    /// <param name="kind">The skip reason to count.</param>
    /// <returns>The number of skipped chests with that kind; 0 for an unknown value.</returns>
    public int CountSkipped(SkipReason kind)
    {
        var count = 0;
        for (var i = 0; i < plan.Skipped.Count; i++)
        {
            if (plan.Skipped[i].Kind == kind)
            {
                count++;
            }
        }

        return count;
    }
}
