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
/// Story AC-1 (contract X2, X4, X7, X8; 5.1): the executor processes the plan chest by chest through reservations, adds to the
/// destination, confirms, and only then removes from the source; the end state matches the layouts and conserves every item.
/// </summary>
public sealed class ExecutionHappyPathTests
{
    // GW-U-900 (AC-1, contract 5.1): base E completes in the contracted frames with the contracted counts and end state.
    [Fact]
    public void BaseECompletesWithTheContractedFramesCountsAndEndState()
    {
        ExecutionHarness h = Named("E");
        Assert.Equal(ExecutionStatus.NotStarted, h.Executor.Status);
        Assert.False(h.Executor.IsComplete);

        List<int> steps = h.Run(20);

        Assert.Equal(new[] { 0, 1, 1, 4 }, steps);
        Assert.Equal(0, h.Executor.Step());
        Assert.Equal(0, h.Executor.Step());
        Assert.Equal(ExecutionStatus.Completed, h.Executor.Status);
        Assert.True(h.Executor.IsComplete);

        OrganizeExecutionResult result = h.Result;
        Assert.Equal(ExecutionStatus.Completed, result.Status);
        Assert.Equal(ExecutionStopReason.None, result.StopReason);
        Assert.Equal(default(ContainerId), result.FailedChest);
        Assert.Null(result.FailedMove);
        Assert.Equal(6, result.MovesCompleted);
        Assert.Equal(0, result.MovesRemaining);
        Assert.Equal(5, result.StacksMoved);
        Assert.Equal(127L, result.ItemsMoved);
        Assert.Equal(3, result.ChestsPlanned);
        Assert.Equal(3, result.ChestsCompleted);
        Assert.Equal(0L, result.UnrecoveredItems);

        Assert.Equal(Expect(("Wood", 100, 50)), Totals(h.Live(201).Contents));
        Assert.Equal(Expect(("Stone", 55, 50), ("Flint", 5, 50)), Totals(h.Live(202).Contents));
        Assert.Equal(Expect(("Stone", 40, 50), ("Flint", 7, 50)), Totals(h.Live(203).Contents));
        Assert.Equal(Expect(("Resin", 12, 50)), Totals(h.Live(204).Contents));
        Assert.Equal(Expect(("Wood", 35, 50)), Totals(h.Live(208).Contents));
        Assert.Equal(Expect(("CookedMeat", 5, 50), ("TrophyDeer", 1, 20)), Totals(h.Live(209).Contents));
        h.AssertLiveTotalsMatchLayouts();
        h.AssertUntouched(205, 206, 207);
        h.AssertAllReleased();
        h.AssertConserved();
    }

    // GW-U-901 (AC-1, contract X2, X4): for every move the add precedes the remove, batches are bracketed by request and release,
    // and the next batch's requests come after the previous batch's releases.
    [Theory]
    [InlineData("E")]
    [InlineData("Merge")]
    [InlineData("Overflow")]
    [InlineData("Chained")]
    public void EveryMoveAddsBeforeItRemovesAndBatchesAreBracketedByReservations(string fixture)
    {
        ExecutionHarness h = Named(fixture);
        h.Run(20);
        Assert.Equal(ExecutionStatus.Completed, h.Executor.Status);

        List<string> calls = h.Recorder.Calls;
        List<int> addAt = Positions(calls, "Add ");
        List<int> removeAt = Positions(calls, "Remove ");
        Assert.Equal(h.Plan.Moves.Count, addAt.Count);
        Assert.Equal(h.Plan.Moves.Count, removeAt.Count);
        for (int i = 0; i < h.Plan.Moves.Count; i++)
        {
            PlannedMove move = h.Plan.Moves[i];
            Assert.Equal(
                RecordingContainerLayer.Line("Add", move.Destination) + " " + move.PrefabName + " x" + move.Count + " Success",
                calls[addAt[i]]);
            Assert.StartsWith(RecordingContainerLayer.Line("Remove", move.Source) + "#", calls[removeAt[i]]);
            Assert.EndsWith(" " + move.PrefabName + " x" + move.Count + " Success", calls[removeAt[i]]);
            Assert.True(addAt[i] < removeAt[i], "Move " + i + " removed before it added.");
            if (i > 0)
            {
                Assert.True(removeAt[i - 1] < addAt[i], "Move " + i + " started before move " + (i - 1) + " finished.");
            }
        }

        // Batches: maximal runs of consecutive moves with the same source (contract X4).
        var batches = new List<(int First, int Last, List<ContainerId> Chests)>();
        for (int i = 0; i < h.Plan.Moves.Count; i++)
        {
            PlannedMove move = h.Plan.Moves[i];
            if (batches.Count == 0 || h.Plan.Moves[batches[batches.Count - 1].First].Source != move.Source)
            {
                batches.Add((i, i, new List<ContainerId> { move.Source }));
            }

            (int first, _, List<ContainerId> chests) = batches[batches.Count - 1];
            if (!chests.Contains(move.Destination))
            {
                chests.Add(move.Destination);
            }

            batches[batches.Count - 1] = (first, i, chests);
        }

        int cursor = 0;
        foreach ((int first, int last, List<ContainerId> chests) in batches)
        {
            int firstWrite = addAt[first];
            int lastWrite = removeAt[last];
            int lastRelease = -1;
            foreach (ContainerId chest in chests)
            {
                int request = calls.FindIndex(cursor, line => line == RecordingContainerLayer.Line("Request", chest));
                int release = calls.FindIndex(cursor, line => line == RecordingContainerLayer.Line("Release", chest));
                Assert.True(request >= 0 && request < firstWrite, "Chest " + chest.Id + " was not requested before its batch wrote.");
                Assert.True(release > lastWrite, "Chest " + chest.Id + " was not released after its batch finished writing.");
                lastRelease = Math.Max(lastRelease, release);
            }

            // Every write of the batch sits between the batch's requests and releases.
            Assert.True(cursor <= firstWrite && lastWrite < lastRelease);
            cursor = lastRelease + 1;
        }

        Assert.Equal(batches.Count, h.Result.ChestsPlanned);
        Assert.Equal(batches.Count, h.Result.ChestsCompleted);
    }

    private static List<int> Positions(List<string> calls, string prefix)
    {
        var positions = new List<int>();
        for (int i = 0; i < calls.Count; i++)
        {
            if (calls[i].StartsWith(prefix, StringComparison.Ordinal))
            {
                positions.Add(i);
            }
        }

        return positions;
    }

    // GW-U-902 (AC-1, contract 5.1): every fixture base completes, its live totals equal its layouts, and conservation holds. SkipCombined is
    // in GW-U-932 instead: its seeds 515 and 516 carry invalid contents on purpose, which ConservationAssert's limit checks would flag.
    [Theory]
    [InlineData("E")]
    [InlineData("Merge")]
    [InlineData("Overflow")]
    [InlineData("OverflowNo405")]
    [InlineData("Solo702")]
    [InlineData("Pair702")]
    [InlineData("Iron")]
    [InlineData("Chained")]
    [InlineData("ChainedNoB")]
    [InlineData("Perf")]
    public void EveryFixtureCompletesToItsLayoutsAndConservesItems(string fixture)
    {
        ExecutionHarness h = Named(fixture);

        h.Run(20);

        Assert.Equal(ExecutionStatus.Completed, h.Executor.Status);
        Assert.Equal(h.Plan.Moves.Count, h.Result.MovesCompleted);
        Assert.Equal(0, h.Result.MovesRemaining);
        Assert.Equal(h.Plan.Moves.Sum(m => (long)m.Count), h.Result.ItemsMoved);
        h.AssertLiveTotalsMatchLayouts();
        foreach (SkippedChest skipped in h.Plan.Skipped)
        {
            h.AssertUntouched(skipped.Id.Id);
        }

        h.AssertAllReleased();
        h.AssertConserved();
    }

    // GW-U-903 (AC-1, contract X8): a Deposit-shaped plan whose source is the player inventory needs no reservation wait and
    // completes in one step.
    [Fact]
    public void APlanFromThePlayerInventoryCompletesInOneStepWithoutWaiting()
    {
        ContainerId inventoryId = OrganizeFixtures.Id(7);
        var inventory = new ContainerSnapshot(
            inventoryId,
            new WorldPosition(0f, 30f, 0f),
            8,
            4,
            new[] { OrganizeFixtures.S("Wood", 20), OrganizeFixtures.S("Stone", 5), OrganizeFixtures.S("CopperOre", 3) },
            ContainerFlags.PlayerBuilt);
        OrganizeFixtures.PlanInput input = OrganizeFixtures.Make(
            new[] { inventory, OrganizeFixtures.C(901, 2, 0, 2, OrganizeFixtures.S("Wood", 10)), OrganizeFixtures.C(902, 4, 0, 2, OrganizeFixtures.S("Stone", 10)) },
            OrganizeFixtures.Assign(
                (901, BaseStorage.Core.Persistence.CategoryAssignment.ForCategory("Wood")),
                (902, BaseStorage.Core.Persistence.CategoryAssignment.ForCategory("Stone"))));
        ExecutionHarness h = For(input, playerInventory: inventory);
        Assert.Equal(2, h.Plan.Moves.Count);
        Assert.All(h.Plan.Moves, move => Assert.Equal(inventoryId, move.Source));

        // Step 1: the inventory is Granted at once, but the destinations answer Pending first; step 2 writes both moves.
        List<int> steps = h.Run(20);

        Assert.Equal(new[] { 0, 2 }, steps);
        Assert.Equal(ExecutionStatus.Completed, h.Executor.Status);
        Assert.Equal(ReservationStatus.Granted, h.Fake.GetReservationStatus(inventoryId));
        Assert.Equal(Expect(("Wood", 30, 50)), Totals(h.Live(901).Contents));
        Assert.Equal(Expect(("Stone", 15, 50)), Totals(h.Live(902).Contents));
        Assert.Equal(new[] { OrganizeFixtures.S("CopperOre", 3) }, FakeFixtures.Get(h.Fake, inventoryId).Contents);
        Assert.Equal(1, h.Result.NoRoomStacks);
        h.AssertConserved();
    }
}
