namespace BaseStorage.Core.Consumption;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;

/// <summary>One pending or finished consumption. It is produced only by ConsumptionExecutor.Execute, and it has reference equality.</summary>
public sealed class ConsumptionExecution
{
    private readonly IContainerLayer layer;
    private readonly ConsumptionPlan plan;
    private readonly ChestReservations<ConsumptionPlan> registry;
    private readonly List<TakeGroup> takenGroups = new List<TakeGroup>();
    private ConsumptionResult? result;

    internal ConsumptionExecution(IContainerLayer layer, ConsumptionPlan plan, ChestReservations<ConsumptionPlan> registry)
    {
        this.layer = layer;
        this.plan = plan;
        this.registry = registry;
    }

    /// <summary>Gets the plan being executed, as passed to Execute.</summary>
    public ConsumptionPlan Plan => plan;

    /// <summary>Gets a value indicating whether the execution has reached a terminal outcome.</summary>
    public bool IsComplete => result != null;

    /// <summary>Gets the terminal result, or null while the execution is pending.</summary>
    public ConsumptionResult? Result => result;

    /// <summary>Advances the execution by one step. Call once per frame until it returns a result.</summary>
    /// <returns>Null while waiting for reservations; otherwise the terminal result, the same instance on every later call.</returns>
    /// <exception cref="Exception">Any exception thrown by the container layer during this call is rethrown after cleanup; the execution is then complete with outcome Faulted.</exception>
    public ConsumptionResult? Poll()
    {
        if (result != null)
        {
            return result;
        }

        try
        {
            return PollCore();
        }
        catch
        {
            if (result == null)
            {
                Rollback(out List<ItemStackSnapshot> unrestored);
                Finish(ConsumptionOutcome.Faulted, default, ReservationFailure.None, null, unrestored);
            }

            throw;
        }
    }

    /// <summary>Abandons a pending execution: releases every reservation and completes with outcome Cancelled. A no-op once complete.</summary>
    public void Cancel()
    {
        if (result != null)
        {
            return;
        }

        Finish(ConsumptionOutcome.Cancelled, default, ReservationFailure.None, null, Array.Empty<ItemStackSnapshot>());
    }

    internal void Start()
    {
        try
        {
            foreach (ContainerSnapshot chest in plan.ExpectedContainers)
            {
                registry.Begin(chest.Id, plan);
                layer.RequestReservation(chest.Id);
            }
        }
        catch
        {
            Finish(ConsumptionOutcome.Faulted, default, ReservationFailure.None, null, Array.Empty<ItemStackSnapshot>());
            throw;
        }
    }

    private static bool SameContents(IReadOnlyList<ItemStackSnapshot> a, IReadOnlyList<ItemStackSnapshot> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }

    private ConsumptionResult? PollCore()
    {
        foreach (ContainerSnapshot chest in plan.ExpectedContainers)
        {
            Reservation<ConsumptionPlan>? reservation = registry.Find(chest.Id);
            if (reservation == null || reservation.IsTerminal)
            {
                continue;
            }

            ReservationStatus status = layer.GetReservationStatus(chest.Id);
            if (reservation.State == ReservationState.Requesting)
            {
                switch (status)
                {
                    case ReservationStatus.Pending:
                        break;
                    case ReservationStatus.Granted:
                        reservation.Apply(ReservationEvent.Grant);
                        break;
                    case ReservationStatus.Denied:
                        reservation.Apply(ReservationEvent.Deny);
                        break;
                    default:
                        reservation.Apply(ReservationEvent.OwnerLost);
                        break;
                }
            }
            else if (status != ReservationStatus.Granted)
            {
                reservation.Apply(ReservationEvent.OwnerLost);
            }
        }

        registry.CheckTimeouts();

        bool allGranted = true;
        foreach (ContainerSnapshot chest in plan.ExpectedContainers)
        {
            Reservation<ConsumptionPlan>? reservation = registry.Find(chest.Id);
            if (reservation != null && reservation.State == ReservationState.Failed)
            {
                return Finish(ConsumptionOutcome.ReservationFailed, chest.Id, reservation.Failure, null, Array.Empty<ItemStackSnapshot>());
            }

            if (reservation == null || reservation.State != ReservationState.Granted)
            {
                allGranted = false;
            }
        }

        if (!allGranted)
        {
            return null;
        }

        ContainerId? mismatch = Verify();
        if (mismatch.HasValue)
        {
            return Finish(ConsumptionOutcome.ContentsChanged, mismatch.Value, ReservationFailure.None, null, Array.Empty<ItemStackSnapshot>());
        }

        foreach (ContainerSnapshot chest in plan.ExpectedContainers)
        {
            registry.Apply(chest.Id, ReservationEvent.BeginWrite);
        }

        return Take();
    }

    private ContainerId? Verify()
    {
        bool usesInventory = false;
        foreach (ConsumptionTake take in plan.Takes)
        {
            if (take.ContainerId == plan.PlayerInventoryId)
            {
                usesInventory = true;
                break;
            }
        }

        if (usesInventory)
        {
            if (layer.PlayerInventoryId != plan.PlayerInventoryId)
            {
                return plan.PlayerInventoryId;
            }

            ContainerSnapshot? inventory = layer.GetContainer(plan.PlayerInventoryId);
            if (inventory == null)
            {
                return plan.PlayerInventoryId;
            }

            foreach (ConsumptionTake take in plan.Takes)
            {
                if (take.ContainerId != plan.PlayerInventoryId)
                {
                    continue;
                }

                if (take.StackIndex < 0 || take.StackIndex >= inventory.Contents.Count || inventory.Contents[take.StackIndex] != take.Stack)
                {
                    return plan.PlayerInventoryId;
                }
            }
        }

        foreach (ContainerSnapshot expected in plan.ExpectedContainers)
        {
            ContainerSnapshot? fresh = layer.GetContainer(expected.Id);
            if (fresh == null || !SameContents(fresh.Contents, expected.Contents))
            {
                return expected.Id;
            }
        }

        return null;
    }

    private List<TakeGroup> BuildGroups()
    {
        var groups = new List<TakeGroup>();
        foreach (ConsumptionTake take in plan.Takes)
        {
            TakeGroup? existing = null;
            foreach (TakeGroup g in groups)
            {
                if (g.ContainerId == take.ContainerId && g.StackIndex == take.StackIndex)
                {
                    existing = g;
                    break;
                }
            }

            if (existing != null)
            {
                existing.Count += take.Count;
            }
            else
            {
                groups.Add(new TakeGroup(take.ContainerId, take.StackIndex, take.Stack, take.Count));
            }
        }

        return groups;
    }

    private ConsumptionResult Take()
    {
        foreach (TakeGroup group in BuildGroups())
        {
            ContainerOperationResult failure;
            ContainerSnapshot? c = layer.GetContainer(group.ContainerId);
            if (c == null)
            {
                failure = ContainerOperationResult.NotFound;
            }
            else
            {
                int index = Locate(c, group);

                failure = index < 0
                    ? ContainerOperationResult.ContainerChanged
                    : layer.RemoveFromStack(group.ContainerId, index, group.Stack, group.Count);
            }

            if (failure != ContainerOperationResult.Success)
            {
                Rollback(out List<ItemStackSnapshot> unrestored);
                return Finish(ConsumptionOutcome.TakeFailed, group.ContainerId, ReservationFailure.None, failure, unrestored);
            }

            takenGroups.Add(group);
        }

        return Finish(ConsumptionOutcome.Success, default, ReservationFailure.None, null, Array.Empty<ItemStackSnapshot>());
    }

    private ConsumptionResult Finish(ConsumptionOutcome outcome, ContainerId failedContainer, ReservationFailure failure, ContainerOperationResult? failedOperation, IReadOnlyList<ItemStackSnapshot> unrestored)
    {
        ReleaseAll();
        result = new ConsumptionResult(outcome, failedContainer, failure, failedOperation, unrestored);
        return result;
    }

    private int Locate(ContainerSnapshot container, TakeGroup group)
    {
        if (group.StackIndex >= 0 && group.StackIndex < container.Contents.Count && container.Contents[group.StackIndex] == group.Stack)
        {
            return group.StackIndex;
        }

        for (int i = 0; i < container.Contents.Count; i++)
        {
            if (container.Contents[i] == group.Stack)
            {
                return i;
            }
        }

        return -1;
    }

    private void Rollback(out List<ItemStackSnapshot> unrestored)
    {
        unrestored = new List<ItemStackSnapshot>();
        for (int i = takenGroups.Count - 1; i >= 0; i--)
        {
            TakeGroup g = takenGroups[i];
            var items = new ItemStackSnapshot(g.Stack.PrefabName, g.Count, g.Stack.Quality, g.Stack.MaxStack);
            if (TryAdd(g.ContainerId, items))
            {
                continue;
            }

            if (g.ContainerId != plan.PlayerInventoryId && TryAdd(plan.PlayerInventoryId, items))
            {
                continue;
            }

            unrestored.Add(items);
        }

        takenGroups.Clear();
    }

    private bool TryAdd(ContainerId id, ItemStackSnapshot items)
    {
        try
        {
            return layer.AddItems(id, items) == ContainerOperationResult.Success;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void ReleaseAll()
    {
        foreach (ContainerSnapshot chest in plan.ExpectedContainers)
        {
            Reservation<ConsumptionPlan>? reservation = registry.Find(chest.Id);
            if (reservation != null)
            {
                if (reservation.State == ReservationState.Requesting)
                {
                    reservation.Apply(ReservationEvent.Cancel);
                }
                else if (reservation.HoldsChest)
                {
                    reservation.Apply(ReservationEvent.BeginRelease);
                    reservation.Apply(ReservationEvent.CompleteRelease);
                }
            }

            try
            {
                layer.ReleaseReservation(chest.Id);
            }
            catch (Exception)
            {
                // CX8: a failing release never changes the outcome and never stops the remaining releases.
            }
        }
    }

    private sealed class TakeGroup
    {
        public TakeGroup(ContainerId containerId, int stackIndex, ItemStackSnapshot stack, int count)
        {
            ContainerId = containerId;
            StackIndex = stackIndex;
            Stack = stack;
            Count = count;
        }

        public ContainerId ContainerId { get; }

        public int StackIndex { get; }

        public ItemStackSnapshot Stack { get; }

        public int Count { get; set; }
    }
}
