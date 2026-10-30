namespace BaseStorage.Core.Tests.Simulation;

using System;
using System.Collections.Generic;
using System.Globalization;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Organize.Execution;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;

/// <summary>
/// A simulated Organize or Deposit of one peer (AC-4.8, AC-4.9, AC-5.4): acquire the base lock, plan from the peer's view, step an
/// <see cref="OrganizeExecutor"/> once per frame while renewing the lock, and release the lock when the run ends. A refused lock ends
/// the operation before any plan is made; a failed renewal aborts the executor. Not a test row.
/// </summary>
internal sealed class ExecutorOperation : PeerOperation
{
    private readonly OrganizeLock baseLock;
    private readonly IReadOnlyList<StationSnapshot> stations;
    private readonly long holderId;
    private readonly string holderName;
    private readonly Func<OrganizePlan> planner;
    private readonly IContainerLayer layer;
    private readonly IClock clock;
    private readonly BaseStorageSettings settings;
    private readonly int renewEveryTicks;
    private int ticksSinceRenew;

    /// <summary>Creates the operation; nothing happens until the first <see cref="Step"/>.</summary>
    /// <param name="kind">Organize or Deposit.</param>
    /// <param name="baseLock">The peer's lock service over the shared store.</param>
    /// <param name="stations">The base's stations.</param>
    /// <param name="holderId">The peer's player id.</param>
    /// <param name="holderName">The peer's name.</param>
    /// <param name="planner">Builds the plan from the peer's view once the lock is held.</param>
    /// <param name="layer">The peer's layer.</param>
    /// <param name="clock">The shared clock.</param>
    /// <param name="settings">The shared settings.</param>
    /// <param name="renewEveryTicks">How many frames pass between lock renewals.</param>
    internal ExecutorOperation(
        OperationKind kind,
        OrganizeLock baseLock,
        IReadOnlyList<StationSnapshot> stations,
        long holderId,
        string holderName,
        Func<OrganizePlan> planner,
        IContainerLayer layer,
        IClock clock,
        BaseStorageSettings settings,
        int renewEveryTicks)
    {
        Kind = kind;
        this.baseLock = baseLock;
        this.stations = stations;
        this.holderId = holderId;
        this.holderName = holderName;
        this.planner = planner;
        this.layer = layer;
        this.clock = clock;
        this.settings = settings;
        this.renewEveryTicks = Math.Max(1, renewEveryTicks);
    }

    /// <inheritdoc />
    internal override OperationKind Kind { get; }

    /// <summary>Gets the plan, once the lock was acquired.</summary>
    internal OrganizePlan? Plan { get; private set; }

    /// <summary>Gets the executor, once a plan with moves exists.</summary>
    internal OrganizeExecutor? Executor { get; private set; }

    /// <summary>Gets the result of the lock acquisition.</summary>
    internal OrganizeLockResult? LockResult { get; private set; }

    /// <summary>Gets the number of lock renewals that were granted.</summary>
    internal int Renewals { get; private set; }

    /// <inheritdoc />
    internal override void Step()
    {
        if (IsDone)
        {
            return;
        }

        if (Executor is null)
        {
            LockResult = baseLock.Acquire(stations, holderId, holderName);
            if (!LockResult.IsGranted)
            {
                Outcome = OperationOutcome.LockRefused;
                return;
            }

            HoldsLock = true;
            Plan = planner();
            if (Plan.Moves.Count == 0)
            {
                Finish(OperationOutcome.Nothing);
                return;
            }

            Executor = new OrganizeExecutor(Plan, layer, clock, settings);
        }

        if (++ticksSinceRenew >= renewEveryTicks)
        {
            ticksSinceRenew = 0;
            OrganizeLockResult renewed = baseLock.Renew(stations, holderId, holderName);
            if (!renewed.IsGranted)
            {
                Executor.Abort();
                Finish(OperationOutcome.LockLost);
                return;
            }

            Renewals++;
        }

        Executor.Step();
        if (Executor.IsComplete)
        {
            Finish(OutcomeOf(Executor.Result));
        }
    }

    /// <inheritdoc />
    internal override string Describe()
    {
        string what = Kind == OperationKind.Organize ? "organize" : "deposit";
        if (Plan is null)
        {
            return what + " (no lock)";
        }

        string run = Executor is null
            ? string.Empty
            : " " + Executor.Result.MovesCompleted.ToString(CultureInfo.InvariantCulture) + "/" + Plan.Moves.Count.ToString(CultureInfo.InvariantCulture) + " moves";
        return what + " " + Plan.Moves.Count.ToString(CultureInfo.InvariantCulture) + " moves planned" + run;
    }

    private static OperationOutcome OutcomeOf(OrganizeExecutionResult result)
    {
        if (result.Status == ExecutionStatus.Completed)
        {
            return OperationOutcome.Completed;
        }

        switch (result.StopReason)
        {
            case ExecutionStopReason.ReservationRefused:
                return OperationOutcome.Denied;
            case ExecutionStopReason.ReservationTimedOut:
                return OperationOutcome.TimedOut;
            case ExecutionStopReason.ReservationLost:
                return OperationOutcome.Lost;
            case ExecutionStopReason.ChestChanged:
                return OperationOutcome.Changed;
            default:
                return OperationOutcome.Stopped;
        }
    }

    private void Finish(OperationOutcome outcome)
    {
        baseLock.Release(stations, holderId);
        HoldsLock = false;
        Outcome = outcome;
    }
}
