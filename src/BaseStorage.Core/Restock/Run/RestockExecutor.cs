namespace BaseStorage.Core.Restock.Run;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Organize.Execution;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;

/// <summary>
/// Runs one Restock plan through an <see cref="IRestockLayer"/>, one add at a time (S-QR-002 contract sections 3.3 and 5).
/// Every move is add, re-read, then remove: the chest is only touched for items the re-read proves arrived (RR6).
/// Single-threaded; the Core owns the order and the accounting, the layer owns the game calls.
/// </summary>
public sealed class RestockExecutor
{
    private readonly IRestockLayer layer;
    private readonly BaseStorageSettings settings;
    private readonly ChestReservations<RestockPlan> reservations;
    private readonly List<ChestWork> chests = new List<ChestWork>();
    private readonly Dictionary<int, ItemStackSnapshot> expected = new Dictionary<int, ItemStackSnapshot>();
    private readonly Dictionary<ContainerId, Dictionary<int, ItemStackSnapshot?>> sources = new Dictionary<ContainerId, Dictionary<int, ItemStackSnapshot?>>();
    private readonly HashSet<int> stale = new HashSet<int>();
    private readonly HashSet<int> topped = new HashSet<int>();
    private ExecutionStatus status;
    private RestockStopReason stopReason;
    private int chestIndex;
    private int cursor;
    private bool requested;
    private bool writing;
    private long movedItems;
    private long leftDuringRun;
    private long movesCompleted;
    private long movesSkipped;
    private long unrecoveredItems;

    /// <summary>Creates the executor over one plan. Nothing is read from the layer.</summary>
    /// <param name="plan">The plan; not null.</param>
    /// <param name="layer">The game layer; not null.</param>
    /// <param name="clock">The clock; not null.</param>
    /// <param name="settings">The settings; not null.</param>
    /// <exception cref="ArgumentNullException">An argument is null; ParamName names the first one.</exception>
    public RestockExecutor(RestockPlan plan, IRestockLayer layer, IClock clock, BaseStorageSettings settings)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        this.layer = layer ?? throw new ArgumentNullException(nameof(layer));
        if (clock is null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        reservations = new ChestReservations<RestockPlan>(clock, Math.Max(1, settings.ReserveTimeoutMs));
        var byChest = new Dictionary<ContainerId, ChestWork>();
        for (int i = 0; i < plan.Moves.Count; i++)
        {
            RestockMove move = plan.Moves[i];
            if (!byChest.TryGetValue(move.Source, out ChestWork? work))
            {
                work = new ChestWork(move.Source);
                byChest[move.Source] = work;
                chests.Add(work);
                sources[move.Source] = new Dictionary<int, ItemStackSnapshot?>();
            }

            work.Moves.Add(i);
            if (!expected.ContainsKey(move.TargetIndex))
            {
                expected[move.TargetIndex] = move.TargetStack;
            }

            if (!sources[move.Source].ContainsKey(move.SourceStackIndex))
            {
                sources[move.Source][move.SourceStackIndex] = move.SourceStack;
            }
        }
    }

    private enum Flow
    {
        Next,
        Yield,
        ChestEnded,
        Stopped,
    }

    /// <summary>Gets the plan.</summary>
    public RestockPlan Plan { get; }

    /// <summary>Gets the status.</summary>
    public ExecutionStatus Status => status;

    /// <summary>Gets a value that is true when the status is Completed or Stopped.</summary>
    public bool IsComplete => status == ExecutionStatus.Completed || status == ExecutionStatus.Stopped;

    /// <summary>Gets a fresh snapshot of the counters.</summary>
    public RestockRunResult Result => new RestockRunResult(
        status,
        stopReason,
        movedItems,
        topped.Count,
        leftDuringRun,
        Plan.LeftBehind,
        stale.Count,
        movesCompleted,
        movesSkipped,
        unrecoveredItems);

    /// <summary>Does up to max(1, MovesPerFrame) add attempts (RR14).</summary>
    /// <returns>The number of add attempts made.</returns>
    public int Step()
    {
        if (IsComplete)
        {
            return 0;
        }

        status = ExecutionStatus.Running;
        int budget = Math.Max(1, settings.MovesPerFrame);
        int done = 0;
        while (true)
        {
            if (chestIndex >= chests.Count)
            {
                status = ExecutionStatus.Completed;
                return done;
            }

            ChestWork chest = chests[chestIndex];
            if (!writing)
            {
                if (!requested && AllRemainingStale(chest))
                {
                    LeaveRest(chest);
                    NextChest();
                    continue;
                }

                Flow gate = Reserve(chest);
                if (gate == Flow.Yield)
                {
                    return done;
                }

                if (gate == Flow.ChestEnded)
                {
                    continue;
                }
            }

            bool ended = false;
            while (cursor < chest.Moves.Count)
            {
                Flow flow = Execute(chest, Plan.Moves[chest.Moves[cursor]], budget, ref done);
                if (flow == Flow.Yield || flow == Flow.Stopped)
                {
                    return done;
                }

                if (flow == Flow.ChestEnded)
                {
                    ended = true;
                    break;
                }
            }

            if (!ended)
            {
                Release(chest.Id);
                NextChest();
            }
        }
    }

    /// <summary>Stops the run with reason Aborted.</summary>
    /// <returns>False when already complete.</returns>
    public bool Abort() => Abort(RestockStopReason.Aborted);

    /// <summary>Stops the run with the given reason (LockLost or Aborted).</summary>
    /// <param name="reason">The reason.</param>
    /// <returns>False when already complete.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="reason"/> is not LockLost or Aborted.</exception>
    public bool Abort(RestockStopReason reason)
    {
        if (reason != RestockStopReason.LockLost && reason != RestockStopReason.Aborted)
        {
            throw new ArgumentOutOfRangeException(nameof(reason), "Only LockLost and Aborted can be given.");
        }

        if (IsComplete)
        {
            return false;
        }

        Stop(reason);
        return true;
    }

    private static bool SameKey(ItemStackSnapshot a, ItemStackSnapshot b) =>
        string.Equals(a.PrefabName, b.PrefabName, StringComparison.Ordinal)
        && a.Quality == b.Quality
        && a.MaxStack == b.MaxStack
        && a.Identity.Equals(b.Identity);

    private static int IndexOf(IReadOnlyList<ItemStackSnapshot> contents, ItemStackSnapshot value)
    {
        for (int i = 0; i < contents.Count; i++)
        {
            if (contents[i] == value)
            {
                return i;
            }
        }

        return -1;
    }

    private bool AllRemainingStale(ChestWork chest)
    {
        for (int i = cursor; i < chest.Moves.Count; i++)
        {
            if (!stale.Contains(Plan.Moves[chest.Moves[i]].TargetIndex))
            {
                return false;
            }
        }

        return true;
    }

    private Flow Reserve(ChestWork chest)
    {
        if (!requested)
        {
            layer.RequestReservation(chest.Id);
            reservations.Begin(chest.Id, Plan);
            requested = true;
        }

        switch (layer.GetReservationStatus(chest.Id))
        {
            case ReservationStatus.Granted:
                break;
            case ReservationStatus.Pending:
                if (reservations.CheckTimeouts().Count > 0)
                {
                    SkipChest(chest);
                    return Flow.ChestEnded;
                }

                return Flow.Yield;
            case ReservationStatus.Denied:
                reservations.Apply(chest.Id, ReservationEvent.Deny);
                SkipChest(chest);
                return Flow.ChestEnded;
            case ReservationStatus.Lost:
                reservations.Apply(chest.Id, ReservationEvent.OwnerLost);
                SkipChest(chest);
                return Flow.ChestEnded;
            default:
                SkipChest(chest);
                return Flow.ChestEnded;
        }

        Reservation<RestockPlan>? reservation = reservations.Find(chest.Id);
        if (reservation is not null && reservation.State == ReservationState.Requesting)
        {
            reservation.Apply(ReservationEvent.Grant);
        }

        if (layer.GetContainer(chest.Id) is null)
        {
            SkipChest(chest);
            return Flow.ChestEnded;
        }

        reservations.Apply(chest.Id, ReservationEvent.BeginWrite);
        writing = true;
        return Flow.Next;
    }

    private Flow Execute(ChestWork chest, RestockMove move, int budget, ref int done)
    {
        int target = move.TargetIndex;
        int count = move.Count;
        if (stale.Contains(target))
        {
            LeftOne(count);
            cursor++;
            return Flow.Next;
        }

        if (done >= budget)
        {
            return Flow.Yield;
        }

        ContainerSnapshot? live = layer.GetContainer(chest.Id);
        if (live is null)
        {
            SkipChest(chest);
            return Flow.ChestEnded;
        }

        ItemStackSnapshot? source = sources[chest.Id][move.SourceStackIndex];
        if (source is null || source.Count < count || IndexOf(live.Contents, source) < 0)
        {
            SkipChest(chest);
            return Flow.ChestEnded;
        }

        ItemStackSnapshot want = expected[target];
        if (layer.ReadCarried(target) != want)
        {
            stale.Add(target);
            LeftOne(count);
            cursor++;
            return Flow.Next;
        }

        done++;
        layer.AddToCarried(target, want, count);
        ItemStackSnapshot? after = layer.ReadCarried(target);
        if (after is null || !SameKey(after, want))
        {
            Stop(RestockStopReason.ConfirmFailed);
            return Flow.Stopped;
        }

        int delta = after.Count - want.Count;
        if (delta < 0 || delta > count)
        {
            Stop(RestockStopReason.ConfirmFailed);
            return Flow.Stopped;
        }

        if (delta == 0)
        {
            LeftOne(count);
            cursor++;
            return Flow.Next;
        }

        ContainerOperationResult removed = layer.RemoveFromStack(chest.Id, move.SourceStackIndex, source, delta);
        if (removed == ContainerOperationResult.Success)
        {
            movedItems += delta;
            leftDuringRun += count - delta;
            expected[target] = after;
            sources[chest.Id][move.SourceStackIndex] = Reduced(source, delta);
            topped.Add(target);
            movesCompleted++;
            cursor++;
            return Flow.Next;
        }

        if (removed == ContainerOperationResult.NotReserved)
        {
            reservations.Apply(chest.Id, ReservationEvent.OwnerLost);
        }

        layer.RemoveFromCarried(target, after, delta);
        if (layer.ReadCarried(target) == want)
        {
            LeftOne(count);
            cursor++;
            SkipChest(chest);
            return Flow.ChestEnded;
        }

        unrecoveredItems += delta;
        Stop(RestockStopReason.RollbackFailed);
        return Flow.Stopped;
    }

    private static ItemStackSnapshot? Reduced(ItemStackSnapshot stack, int by) =>
        stack.Count - by <= 0
            ? null
            : new ItemStackSnapshot(stack.PrefabName, stack.Count - by, stack.Quality, stack.MaxStack, stack.Identity);

    private void LeftOne(int count)
    {
        leftDuringRun += count;
        movesSkipped++;
    }

    private void LeaveRest(ChestWork chest)
    {
        for (; cursor < chest.Moves.Count; cursor++)
        {
            LeftOne(Plan.Moves[chest.Moves[cursor]].Count);
        }
    }

    /// <summary>Counts the rest of a chest as left, releases it and goes to the next chest (RR11).</summary>
    private void SkipChest(ChestWork chest)
    {
        LeaveRest(chest);
        if (requested)
        {
            Release(chest.Id);
        }

        NextChest();
    }

    private void NextChest()
    {
        reservations.RemoveTerminal();
        chestIndex++;
        cursor = 0;
        requested = false;
        writing = false;
    }

    private void Stop(RestockStopReason reason)
    {
        if (chestIndex < chests.Count)
        {
            ChestWork current = chests[chestIndex];
            LeaveRest(current);
            for (int i = chestIndex + 1; i < chests.Count; i++)
            {
                foreach (int index in chests[i].Moves)
                {
                    LeftOne(Plan.Moves[index].Count);
                }
            }

            chestIndex = chests.Count;
            bool held = requested;
            requested = false;
            writing = false;
            status = ExecutionStatus.Stopped;
            stopReason = reason;
            if (held)
            {
                Release(current.Id);
            }

            return;
        }

        status = ExecutionStatus.Stopped;
        stopReason = reason;
    }

    private void Release(ContainerId chest)
    {
        Reservation<RestockPlan>? reservation = reservations.Find(chest);
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

    private sealed class ChestWork
    {
        internal ChestWork(ContainerId id)
        {
            Id = id;
        }

        internal ContainerId Id { get; }

        internal List<int> Moves { get; } = new List<int>();
    }
}
