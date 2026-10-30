namespace BaseStorage.Core.Organize.Execution;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Executes one Organize plan frame by frame (contract sections 3.1 and 4). The moves are processed chest by chest: each batch of
/// moves from one source reserves its source and destinations through <see cref="ChestReservations{TOperation}"/>, verifies the
/// live chests against the expected state, writes its moves (add to the destination, confirm, then remove from the source), and
/// releases. One <see cref="Step"/> completes at most <c>MovesPerFrame</c> moves. The first failure stops the whole plan, after taking
/// back any confirmed add whose remove failed, so items are neither lost nor duplicated. Not thread-safe; use it from one thread.
/// </summary>
public sealed class OrganizeExecutor
{
    private readonly IContainerLayer layer;
    private readonly IClock clock;
    private readonly BaseStorageSettings settings;
    private readonly Dictionary<ContainerId, ChestLedger> ledgers = new Dictionary<ContainerId, ChestLedger>();
    private List<MoveBatch>? batches;
    private ChestReservations<OrganizePlan>? reservations;
    private int batchIndex;
    private int moveIndex;
    private bool requested;
    private bool writing;
    private ExecutionStatus status;
    private ExecutionStopReason stopReason;
    private ContainerId failedChest;
    private PlannedMove? failedMove;
    private int movesCompleted;
    private int stacksMoved;
    private long itemsMoved;
    private int chestsCompleted;
    private long unrecoveredItems;
    private ContainerId lastMovedSource;
    private int lastMovedIndex = -1;

    /// <summary>Creates an executor for one plan over one layer. The constructor reads nothing from the layer, clock or settings.</summary>
    /// <param name="plan">The plan to execute.</param>
    /// <param name="layer">The polled game access.</param>
    /// <param name="clock">The only time source, used for reservation deadlines.</param>
    /// <param name="settings">The settings that give MovesPerFrame and ReserveTimeoutMs.</param>
    /// <exception cref="ArgumentNullException">An argument is null; ParamName names it.</exception>
    public OrganizeExecutor(OrganizePlan plan, IContainerLayer layer, IClock clock, BaseStorageSettings settings)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        this.layer = layer ?? throw new ArgumentNullException(nameof(layer));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>Gets the plan being executed.</summary>
    public OrganizePlan Plan { get; }

    /// <summary>Gets the current status.</summary>
    public ExecutionStatus Status => status;

    /// <summary>Gets a value indicating whether the status is Completed or Stopped.</summary>
    public bool IsComplete => status == ExecutionStatus.Completed || status == ExecutionStatus.Stopped;

    /// <summary>Gets a fresh result built from the current counters. It allocates on every read and is final once <see cref="IsComplete"/> is true.</summary>
    public OrganizeExecutionResult Result => new OrganizeExecutionResult(
        Plan,
        status,
        stopReason,
        failedChest,
        failedMove,
        movesCompleted,
        stacksMoved,
        itemsMoved,
        (batches ?? MoveBatch.Split(Plan)).Count,
        chestsCompleted,
        unrecoveredItems);

    /// <summary>
    /// Does one frame of work (contract 4.1): requests and polls the current batch's reservations, verifies the chests once all are
    /// granted, and completes at most <c>max(1, MovesPerFrame)</c> moves, read once per call. A step that waits for a pending reservation
    /// completes no move. A step may finish one batch and start the next.
    /// </summary>
    /// <returns>The number of moves completed in this call; 0 once the run is complete.</returns>
    public int Step()
    {
        if (IsComplete)
        {
            return 0;
        }

        if (status == ExecutionStatus.NotStarted)
        {
            Start();
        }

        var budget = Math.Max(1, settings.MovesPerFrame);
        var done = 0;
        while (true)
        {
            if (batchIndex >= batches!.Count)
            {
                status = ExecutionStatus.Completed;
                return done;
            }

            var batch = batches[batchIndex];
            if (!writing && !Reserve(batch))
            {
                return done;
            }

            while (moveIndex < batch.End && done < budget)
            {
                if (!Execute(Plan.Moves[moveIndex]))
                {
                    return done;
                }

                moveIndex++;
                done++;
            }

            if (moveIndex < batch.End)
            {
                return done;
            }

            foreach (var chest in batch.Chests)
            {
                reservations!.Apply(chest, ReservationEvent.BeginRelease);
                layer.ReleaseReservation(chest);
                reservations.Apply(chest, ReservationEvent.CompleteRelease);
            }

            reservations!.RemoveTerminal();
            chestsCompleted++;
            batchIndex++;
            requested = false;
            writing = false;
        }
    }

    /// <summary>Ends the run early: releases every requested or held reservation of the current batch and sets Stopped with reason Aborted.</summary>
    /// <returns>True when this call ended the run; false when it was already complete.</returns>
    public bool Abort()
    {
        if (IsComplete)
        {
            return false;
        }

        Stop(ExecutionStopReason.Aborted, default, null);
        return true;
    }

    private static ExecutionStopReason ReasonOf(ContainerOperationResult result, ExecutionStopReason fallback)
    {
        switch (result)
        {
            case ContainerOperationResult.NotFound:
                return ExecutionStopReason.ChestUnavailable;
            case ContainerOperationResult.NotReserved:
                return ExecutionStopReason.ReservationLost;
            case ContainerOperationResult.ContainerChanged:
                return ExecutionStopReason.ChestChanged;
            case ContainerOperationResult.DestinationFull:
                return ExecutionStopReason.DestinationFull;
            default:
                return fallback;
        }
    }

    private static bool SameItem(ItemStackSnapshot stack, ItemStackSnapshot items) =>
        string.Equals(stack.PrefabName, items.PrefabName, StringComparison.Ordinal)
        && stack.Quality == items.Quality
        && stack.MaxStack == items.MaxStack;

    private static int IndexOf(IReadOnlyList<ItemStackSnapshot> contents, ItemStackSnapshot expected)
    {
        for (var i = 0; i < contents.Count; i++)
        {
            if (contents[i] == expected)
            {
                return i;
            }
        }

        return -1;
    }

    private void Start()
    {
        status = ExecutionStatus.Running;
        reservations = new ChestReservations<OrganizePlan>(clock, Math.Max(1, settings.ReserveTimeoutMs));
        batches = MoveBatch.Split(Plan);
    }

    /// <summary>Requests, polls and verifies the batch's chests. True when the batch may write; false when the step must return.</summary>
    private bool Reserve(MoveBatch batch)
    {
        if (!requested)
        {
            foreach (var chest in batch.Chests)
            {
                layer.RequestReservation(chest);
                reservations!.Begin(chest, Plan);
            }

            requested = true;
        }

        var pending = false;
        foreach (var chest in batch.Chests)
        {
            switch (layer.GetReservationStatus(chest))
            {
                case ReservationStatus.Granted:
                    var reservation = reservations!.Find(chest);
                    if (reservation is not null && reservation.State == ReservationState.Requesting)
                    {
                        reservation.Apply(ReservationEvent.Grant);
                    }

                    break;
                case ReservationStatus.Pending:
                    pending = true;
                    break;
                case ReservationStatus.Denied:
                    reservations!.Apply(chest, ReservationEvent.Deny);
                    Stop(ExecutionStopReason.ReservationRefused, chest, null);
                    return false;
                case ReservationStatus.Lost:
                    reservations!.Apply(chest, ReservationEvent.OwnerLost);
                    Stop(ExecutionStopReason.ReservationLost, chest, null);
                    return false;
                default:
                    Stop(ExecutionStopReason.ChestUnavailable, chest, null);
                    return false;
            }
        }

        if (pending)
        {
            var timedOut = reservations!.CheckTimeouts();
            if (timedOut.Count > 0)
            {
                Stop(ExecutionStopReason.ReservationTimedOut, timedOut[0].Chest, null);
            }

            return false;
        }

        foreach (var chest in batch.Chests)
        {
            var live = layer.GetContainer(chest);
            if (live is null)
            {
                Stop(ExecutionStopReason.ChestUnavailable, chest, null);
                return false;
            }

            if (!Ledger(chest).Matches(live.Contents))
            {
                Stop(ExecutionStopReason.ChestChanged, chest, null);
                return false;
            }
        }

        foreach (var chest in batch.Chests)
        {
            reservations!.Apply(chest, ReservationEvent.BeginWrite);
        }

        writing = true;
        return true;
    }

    /// <summary>Executes one move: add, confirm, remove; rollback on a failed remove. True when the move completed.</summary>
    private bool Execute(PlannedMove move)
    {
        var source = move.Source;
        var destination = move.Destination;
        var live = layer.GetContainer(source);
        if (live is null)
        {
            Stop(ExecutionStopReason.ChestUnavailable, source, move);
            return false;
        }

        var ledger = Ledger(source);
        var expected = ledger.Expected(move.SourceStackIndex);
        var index = expected is null || move.Count > expected.Count ? -1 : IndexOf(live.Contents, expected);
        if (index < 0)
        {
            Stop(ExecutionStopReason.ChestChanged, source, move);
            return false;
        }

        var items = new ItemStackSnapshot(move.PrefabName, move.Count, move.Quality, move.MaxStack);
        layer.SelectSourceStack(source, index);
        var add = layer.AddItems(destination, items);
        if (add != ContainerOperationResult.Success)
        {
            NoteLost(destination, add);
            Stop(ReasonOf(add, ExecutionStopReason.DestinationFull), destination, move);
            return false;
        }

        var remove = layer.RemoveFromStack(source, index, expected!, move.Count);
        if (remove == ContainerOperationResult.Success)
        {
            var key = ChestLedger.KeyOf(move);
            ledger.Take(move.SourceStackIndex, key, move.Count);
            Ledger(destination).Give(key, move.Count);
            movesCompleted++;
            itemsMoved += move.Count;
            if (lastMovedIndex < 0 || lastMovedSource != source || lastMovedIndex != move.SourceStackIndex)
            {
                stacksMoved++;
                lastMovedSource = source;
                lastMovedIndex = move.SourceStackIndex;
            }

            return true;
        }

        NoteLost(source, remove);
        var left = Rollback(destination, items);
        if (left > 0)
        {
            unrecoveredItems = left;
            Stop(ExecutionStopReason.RollbackFailed, destination, move);
        }
        else
        {
            Stop(ReasonOf(remove, ExecutionStopReason.ChestChanged), source, move);
        }

        return false;
    }

    /// <summary>Takes a confirmed add back out of the destination (contract 4.3). Returns the count that could not be taken back.</summary>
    private int Rollback(ContainerId destination, ItemStackSnapshot items)
    {
        var remaining = items.Count;
        while (remaining > 0)
        {
            var live = layer.GetContainer(destination);
            if (live is null)
            {
                break;
            }

            var index = -1;
            for (var i = live.Contents.Count - 1; i >= 0; i--)
            {
                if (SameItem(live.Contents[i], items))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                break;
            }

            var stack = live.Contents[index];
            var take = Math.Min(remaining, stack.Count);
            if (layer.RemoveFromStack(destination, index, stack, take) != ContainerOperationResult.Success)
            {
                break;
            }

            remaining -= take;
        }

        return remaining;
    }

    /// <summary>Reports a lost reservation to the state machine when a write came back NotReserved.</summary>
    private void NoteLost(ContainerId chest, ContainerOperationResult result)
    {
        if (result == ContainerOperationResult.NotReserved)
        {
            reservations!.Apply(chest, ReservationEvent.OwnerLost);
        }
    }

    private void Stop(ExecutionStopReason reason, ContainerId chest, PlannedMove? move)
    {
        if (batches is not null && batchIndex < batches.Count && requested)
        {
            foreach (var held in batches[batchIndex].Chests)
            {
                Release(held);
            }
        }

        status = ExecutionStatus.Stopped;
        stopReason = reason;
        failedChest = chest;
        failedMove = move;
        writing = false;
    }

    /// <summary>Drives one reservation to a terminal state along the legal path for its current state, then releases it in the layer.</summary>
    private void Release(ContainerId chest)
    {
        var reservation = reservations!.Find(chest);
        if (reservation is not null)
        {
            switch (reservation.State)
            {
                case ReservationState.Requesting:
                    reservation.Apply(ReservationEvent.Cancel);
                    break;
                case ReservationState.Granted:
                case ReservationState.Writing:
                    reservation.Apply(ReservationEvent.BeginRelease);
                    reservation.Apply(ReservationEvent.CompleteRelease);
                    break;
                case ReservationState.Releasing:
                    reservation.Apply(ReservationEvent.CompleteRelease);
                    break;
                default:
                    break;
            }
        }

        layer.ReleaseReservation(chest);
    }

    private ChestLedger Ledger(ContainerId chest)
    {
        if (!ledgers.TryGetValue(chest, out var ledger))
        {
            var layout = Plan.FindLayout(chest);
            ledger = new ChestLedger(layout is null ? Array.Empty<ItemStackSnapshot>() : layout.Container.Contents);
            ledgers[chest] = ledger;
        }

        return ledger;
    }
}
