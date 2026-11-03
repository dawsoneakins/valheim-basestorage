namespace BaseStorage.Core.Restock.Run;

using BaseStorage.Core.Organize.Execution;

/// <summary>
/// The counters of one Restock run (S-QR-002 contract section 3.4). There is no public constructor; every counter saturates at int.MaxValue.
/// </summary>
public sealed class RestockRunResult
{
    private readonly ExecutionStatus status;
    private readonly RestockStopReason stopReason;
    private readonly int movedItems;
    private readonly int toppedUpStacks;
    private readonly int leftDuringRun;
    private readonly int leftBehind;
    private readonly int staleStacks;
    private readonly int movesCompleted;
    private readonly int movesSkipped;
    private readonly int unrecoveredItems;

    internal RestockRunResult(
        ExecutionStatus status,
        RestockStopReason stopReason,
        long movedItems,
        int toppedUpStacks,
        long leftDuringRun,
        long planLeftBehind,
        int staleStacks,
        long movesCompleted,
        long movesSkipped,
        long unrecoveredItems)
    {
        this.status = status;
        this.stopReason = stopReason;
        this.movedItems = Saturate(movedItems);
        this.toppedUpStacks = toppedUpStacks;
        this.leftDuringRun = Saturate(leftDuringRun);
        leftBehind = Saturate(planLeftBehind + leftDuringRun);
        this.staleStacks = staleStacks;
        this.movesCompleted = Saturate(movesCompleted);
        this.movesSkipped = Saturate(movesSkipped);
        this.unrecoveredItems = Saturate(unrecoveredItems);
    }

    /// <summary>Gets the run status.</summary>
    public ExecutionStatus Status => status;

    /// <summary>Gets the stop reason; None unless the run stopped.</summary>
    public RestockStopReason StopReason => stopReason;

    /// <summary>Gets the confirmed and removed items, summed over moves.</summary>
    public int MovedItems => movedItems;

    /// <summary>Gets the distinct targets with at least one confirmed, removed delta above 0.</summary>
    public int ToppedUpStacks => toppedUpStacks;

    /// <summary>Gets the planned items that did not move during the run (RR10).</summary>
    public int LeftDuringRun => leftDuringRun;

    /// <summary>Gets the planned items left behind: the saturated sum of plan.LeftBehind and LeftDuringRun (RR13).</summary>
    public int LeftBehind => leftBehind;

    /// <summary>Gets the distinct targets found stale (RR7).</summary>
    public int StaleStacks => staleStacks;

    /// <summary>Gets the moves with a removed delta above 0.</summary>
    public int MovesCompleted => movesCompleted;

    /// <summary>Gets the moves that moved nothing.</summary>
    public int MovesSkipped => movesSkipped;

    /// <summary>Gets the items left duplicated by a failed rollback (RR9).</summary>
    public int UnrecoveredItems => unrecoveredItems;

    private static int Saturate(long value) => value > int.MaxValue ? int.MaxValue : (int)value;
}
