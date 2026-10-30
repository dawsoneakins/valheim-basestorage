namespace BaseStorage.Core.Tests.Organize.Execution;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Organize.Execution;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using static BaseStorage.Core.Tests.Organize.Execution.ExecutionHarness;

/// <summary>
/// Story AC-2 and AC-4 (contract X2, X3, X5; 4.2, 4.3; 5.2): fault injection on base E. A failed move stops the remaining plan and leaves
/// items where they are; a confirmed add whose remove fails is taken back; and ConservationAssert passes after every case.
/// </summary>
public sealed class ExecutionFaultTests
{
    // GW-U-910 (AC-2, AC-4, contract 5.2 row 1): the destination of the first move is full, so nothing moves at all.
    [Fact]
    public void DestinationFullOnTheFirstMoveLeavesEveryChestAsSeeded()
    {
        ExecutionHarness h = Named("E", arrange: fake => fake.InjectFault(OrganizeFixtures.Id(202), FakeFault.DestinationFull));

        h.Run(20);

        OrganizeExecutionResult result = h.Result;
        Assert.Equal(ExecutionStatus.Stopped, result.Status);
        Assert.Equal(ExecutionStopReason.DestinationFull, result.StopReason);
        Assert.Equal(OrganizeFixtures.Id(202), result.FailedChest);
        Assert.Same(h.Plan.Moves[0], result.FailedMove);
        Assert.Equal(0, result.MovesCompleted);
        Assert.Equal(6, result.MovesRemaining);
        Assert.Equal(0, result.ChestsCompleted);
        Assert.Equal(3, result.ChestsPlanned);
        Assert.Equal(0L, result.ItemsMoved);
        h.AssertUntouched(201, 202, 203, 204, 205, 206, 207, 208, 209);
        h.AssertAllReleased();
        h.AssertConserved();
        Assert.Empty(h.CallsOf("Remove"));
    }

    // GW-U-911 (AC-2, contract 5.2 row 2): a full destination later in the plan stops it there; earlier moves stay done, later ones never run.
    [Fact]
    public void DestinationFullLaterStopsTheRemainingPlanAndKeepsEarlierMoves()
    {
        ExecutionHarness h = Named("E", arrange: fake => fake.InjectFault(OrganizeFixtures.Id(208), FakeFault.DestinationFull));

        h.Run(20);

        OrganizeExecutionResult result = h.Result;
        Assert.Equal(ExecutionStatus.Stopped, result.Status);
        Assert.Equal(ExecutionStopReason.DestinationFull, result.StopReason);
        Assert.Equal(OrganizeFixtures.Id(208), result.FailedChest);
        Assert.Same(h.Plan.Moves[4], result.FailedMove);
        Assert.Equal(4, result.MovesCompleted);
        Assert.Equal(2, result.MovesRemaining);
        Assert.Equal(80L, result.ItemsMoved);
        Assert.Equal(4, result.StacksMoved);
        Assert.Equal(2, result.ChestsCompleted);

        Assert.Equal(Expect(("Wood", 100, 50)), Totals(h.Live(201).Contents));
        Assert.Equal(Expect(("Stone", 55, 50), ("Flint", 5, 50)), Totals(h.Live(202).Contents));
        Assert.Equal(Expect(("Stone", 40, 50), ("Flint", 7, 50)), Totals(h.Live(203).Contents));
        Assert.Empty(h.Live(204).Contents);
        Assert.Empty(h.Live(208).Contents);
        Assert.Equal(
            Expect(("Wood", 35, 50), ("Resin", 12, 50), ("CookedMeat", 5, 50), ("TrophyDeer", 1, 20)),
            Totals(h.Live(209).Contents));
        h.AssertAllReleased();
        h.AssertConserved();
        Assert.Equal(4, h.CallsOf("Remove").Count);
    }

    // GW-U-912 (AC-2, AC-4, contract 4.3, 5.2 row 3): the source reports ContainerChanged on the remove after a confirmed add; the add
    // is taken back, the plan stops, and nothing is duplicated.
    [Fact]
    public void ChestChangedOnTheRemoveTakesTheAddBackAndStops()
    {
        ExecutionHarness h = Named("E", arrange: fake => fake.InjectFault(OrganizeFixtures.Id(209), FakeFault.ChestChanged));

        h.Run(20);

        OrganizeExecutionResult result = h.Result;
        Assert.Equal(ExecutionStatus.Stopped, result.Status);
        Assert.Equal(ExecutionStopReason.ChestChanged, result.StopReason);
        Assert.Equal(OrganizeFixtures.Id(209), result.FailedChest);
        Assert.Same(h.Plan.Moves[2], result.FailedMove);
        Assert.Equal(2, result.MovesCompleted);
        Assert.Equal(0L, result.UnrecoveredItems);
        Assert.Equal(2, result.ChestsCompleted);

        Assert.Equal(Expect(("Wood", 50, 50)), Totals(h.Live(201).Contents));
        h.AssertUntouched(209, 204, 208);
        h.AssertAllReleased();
        h.AssertConserved();

        // The rollback is visible in the call log: Add 201 Wood x40 Success, Remove 209 ... ContainerChanged, then a Remove 201 Wood x40 Success.
        List<string> calls = h.Recorder.Calls;
        int add = calls.FindIndex(line => line.StartsWith("Add 201 Wood x40 Success", StringComparison.Ordinal));
        int failedRemove = calls.FindIndex(add, line => line.StartsWith("Remove 209#", StringComparison.Ordinal) && line.EndsWith("ContainerChanged", StringComparison.Ordinal));
        int rollback = calls.FindIndex(failedRemove, line => line.StartsWith("Remove 201#", StringComparison.Ordinal) && line.EndsWith("Wood x40 Success", StringComparison.Ordinal));
        Assert.True(add >= 0 && failedRemove > add && rollback > failedRemove, string.Join(Environment.NewLine, calls));
    }

    // GW-U-913 (AC-2, AC-4, contract X5, 5.2 row 4): another player edits a source chest after the plan was made; the verification before the
    // batch detects it and nothing of that chest moves.
    [Fact]
    public void ALiveEditBeforeTheBatchIsDetectedByTheVerification()
    {
        ExecutionHarness h = Named("E", movesPerFrame: 1);
        ContainerId chest209 = OrganizeFixtures.Id(209);

        Assert.Equal(0, h.Executor.Step());
        Assert.Equal(1, h.Executor.Step());
        Assert.Equal(1, h.Executor.Step());
        Assert.Equal(2, h.Result.MovesCompleted);
        Assert.Equal(ExecutionStatus.Running, h.Executor.Status);

        // The fake has resolved the pending request of 209 to Granted; edit it as another player would have.
        Assert.Equal(ReservationStatus.Granted, h.Fake.GetReservationStatus(chest209));
        Assert.Equal(ContainerOperationResult.Success, h.Fake.RemoveFromStack(chest209, 2, OrganizeFixtures.S("Resin", 12), 5));
        IReadOnlyList<ContainerSnapshot> afterEdit = h.Fake.Capture();

        h.Run(1);

        OrganizeExecutionResult result = h.Result;
        Assert.Equal(ExecutionStatus.Stopped, result.Status);
        Assert.Equal(ExecutionStopReason.ChestChanged, result.StopReason);
        Assert.Equal(chest209, result.FailedChest);
        Assert.Null(result.FailedMove);
        Assert.Equal(2, result.MovesCompleted);
        Assert.Equal(Expect(("Wood", 85, 50), ("Resin", 7, 50), ("CookedMeat", 5, 50), ("TrophyDeer", 1, 20)), Totals(h.Live(209).Contents));
        Assert.DoesNotContain(h.Recorder.Calls, line => line.StartsWith("Add 208", StringComparison.Ordinal) || line.StartsWith("Add 204", StringComparison.Ordinal));
        h.AssertAllReleased();
        ConservationAssert.Holds(afterEdit, h.Fake.Capture());
    }

    // GW-U-914 (AC-4, contract 5.2 row 5): a refused reservation stops the plan before the batch writes anything.
    [Fact]
    public void ReservationRefusedStopsBeforeTheBatchWrites()
    {
        ExecutionHarness h = Named("E", arrange: fake => fake.InjectFault(OrganizeFixtures.Id(209), FakeFault.ReservationRefused));

        h.Run(20);

        OrganizeExecutionResult result = h.Result;
        Assert.Equal(ExecutionStatus.Stopped, result.Status);
        Assert.Equal(ExecutionStopReason.ReservationRefused, result.StopReason);
        Assert.Equal(OrganizeFixtures.Id(209), result.FailedChest);
        Assert.Null(result.FailedMove);
        Assert.Equal(2, result.MovesCompleted);
        Assert.Equal(2, result.ChestsCompleted);
        h.AssertUntouched(209, 204, 208);
        Assert.Equal(Expect(("Wood", 50, 50)), Totals(h.Live(201).Contents));
        h.AssertAllReleased();
        h.AssertConserved();
        Assert.DoesNotContain(h.Recorder.Calls, line => line.StartsWith("Add 201 Wood x40", StringComparison.Ordinal));
    }

    // GW-U-915 (AC-4, contract X7, 5.2 row 6): a reservation that stays pending past ReserveTimeoutMs times out; the chests are released.
    [Fact]
    public void APendingReservationTimesOutAfterReserveTimeoutMs()
    {
        ExecutionHarness h = Named("E", reserveTimeoutMs: 1500, arrange: fake => fake.AutoResolveReservations = false);

        Assert.Equal(0, h.Executor.Step());
        Assert.Equal(ReservationStatus.Pending, h.Fake.GetReservationStatus(OrganizeFixtures.Id(201)));
        Assert.Equal(ReservationStatus.Pending, h.Fake.GetReservationStatus(OrganizeFixtures.Id(202)));

        h.Clock.Set(TimeSpan.FromMilliseconds(100));
        Assert.Equal(0, h.Executor.Step());
        Assert.Equal(ExecutionStatus.Running, h.Executor.Status);
        h.Clock.Set(TimeSpan.FromMilliseconds(1499));
        Assert.Equal(0, h.Executor.Step());
        Assert.Equal(ExecutionStatus.Running, h.Executor.Status);

        h.Clock.Set(TimeSpan.FromMilliseconds(1500));
        Assert.Equal(0, h.Executor.Step());

        OrganizeExecutionResult result = h.Result;
        Assert.Equal(ExecutionStatus.Stopped, result.Status);
        Assert.Equal(ExecutionStopReason.ReservationTimedOut, result.StopReason);
        Assert.Equal(OrganizeFixtures.Id(201), result.FailedChest);
        Assert.Null(result.FailedMove);
        Assert.Equal(0, result.MovesCompleted);
        h.AssertAllReleased();
        h.AssertUntouched(201, 202, 203, 204, 208, 209);
        h.AssertConserved();
    }

    // GW-U-916 (AC-4, contract 5.2 row 7): an owner that disconnects makes the reservation Lost; the plan stops with nothing moved.
    [Fact]
    public void AnOwnerDisconnectStopsWithReservationLost()
    {
        ExecutionHarness h = Named("E", arrange: fake => fake.InjectFault(OrganizeFixtures.Id(202), FakeFault.OwnerDisconnect));

        h.Run(20);

        OrganizeExecutionResult result = h.Result;
        Assert.Equal(ExecutionStatus.Stopped, result.Status);
        Assert.Equal(ExecutionStopReason.ReservationLost, result.StopReason);
        Assert.Equal(OrganizeFixtures.Id(202), result.FailedChest);
        Assert.Equal(0, result.MovesCompleted);
        h.AssertUntouched(201, 202, 203, 204, 208, 209);
        h.AssertAllReleased();
        h.AssertConserved();
    }

    // GW-U-917 (AC-2, contract 4.3, 5.2 row 8, derived): both chests of a move report ContainerChanged, so the add cannot be taken back; the
    // result says so loudly with the unrecovered count. This is the one case where the game state defeats principle 1.
    [Fact]
    public void ADoubleFaultReportsRollbackFailedWithTheUnrecoveredCount()
    {
        ExecutionHarness h = Named("Merge", arrange: fake =>
        {
            fake.InjectFault(OrganizeFixtures.Id(303), FakeFault.ChestChanged);
            fake.InjectFault(OrganizeFixtures.Id(302), FakeFault.ChestChanged);
        });

        h.Run(20);

        OrganizeExecutionResult result = h.Result;
        Assert.Equal(ExecutionStatus.Stopped, result.Status);
        Assert.Equal(ExecutionStopReason.RollbackFailed, result.StopReason);
        Assert.Equal(OrganizeFixtures.Id(302), result.FailedChest);
        Assert.Same(h.Plan.Moves[0], result.FailedMove);
        Assert.Equal(0, result.MovesCompleted);
        Assert.Equal(5L, result.UnrecoveredItems);
        Assert.Equal(Expect(("Stone", 50, 50)), Totals(h.Live(302).Contents));
        h.AssertUntouched(301, 303, 304);
        h.AssertAllReleased();

        long stoneBefore = h.Before.SelectMany(c => c.Contents).Where(s => s.PrefabName == "Stone").Sum(s => (long)s.Count);
        long stoneAfter = h.Fake.Capture().SelectMany(c => c.Contents).Where(s => s.PrefabName == "Stone").Sum(s => (long)s.Count);
        Assert.Equal(stoneBefore + 5, stoneAfter);
    }

    // GW-U-918 (AC-2, contract 3.1, 5.2 row 9): Abort ends the run, releases every reservation and leaves the remaining items where they are.
    [Fact]
    public void AbortMidRunReleasesEverythingAndStopsWithAborted()
    {
        ExecutionHarness h = Named("E", movesPerFrame: 1);
        Assert.Equal(0, h.Executor.Step());
        Assert.Equal(1, h.Executor.Step());
        Assert.Equal(ExecutionStatus.Running, h.Executor.Status);

        Assert.True(h.Executor.Abort());

        OrganizeExecutionResult result = h.Result;
        Assert.Equal(ExecutionStatus.Stopped, result.Status);
        Assert.Equal(ExecutionStopReason.Aborted, result.StopReason);
        Assert.Equal(default(ContainerId), result.FailedChest);
        Assert.Null(result.FailedMove);
        Assert.Equal(1, result.MovesCompleted);
        Assert.Equal(5, result.MovesRemaining);
        h.AssertAllReleased();
        h.AssertConserved();
        Assert.Equal(0, h.Executor.Step());
        Assert.False(h.Executor.Abort());
        Assert.Equal(ExecutionStopReason.Aborted, h.Result.StopReason);
    }

    // GW-U-918 (AC-2): Abort before the first step stops at once and returns true.
    [Fact]
    public void AbortBeforeTheFirstStepStopsAtOnce()
    {
        ExecutionHarness h = Named("E");

        Assert.True(h.Executor.Abort());

        Assert.Equal(ExecutionStatus.Stopped, h.Executor.Status);
        Assert.Equal(ExecutionStopReason.Aborted, h.Result.StopReason);
        Assert.Equal(0, h.Executor.Step());
        Assert.Empty(h.Recorder.Calls);
    }

    // GW-U-919 (AC-2, AC-4, contract 4.2): a chest the plan knows but the layer does not (unloaded) reports None after the request; the plan stops.
    [Fact]
    public void AnUnloadedChestStopsWithChestUnavailable()
    {
        ExecutionHarness h = For(OrganizeFixtures.E(), omit: new uint[] { 204 });

        h.Run(20);

        OrganizeExecutionResult result = h.Result;
        Assert.Equal(ExecutionStatus.Stopped, result.Status);
        Assert.Equal(ExecutionStopReason.ChestUnavailable, result.StopReason);
        Assert.Equal(OrganizeFixtures.Id(204), result.FailedChest);
        Assert.Null(result.FailedMove);
        Assert.Equal(2, result.MovesCompleted);
        h.AssertUntouched(209, 208);
        h.AssertAllReleased();
        h.AssertConserved();
    }
}
