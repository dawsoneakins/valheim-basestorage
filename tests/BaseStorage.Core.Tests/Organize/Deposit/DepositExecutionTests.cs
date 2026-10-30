namespace BaseStorage.Core.Tests.Organize.Deposit;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Organize.Execution;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Tests.Settings;
using BaseStorage.Core.Ui;
using static BaseStorage.Core.Tests.Organize.Deposit.DepositFixtures;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>
/// AC-4.9, AC-4.8, principle 1: the contract 6.3 Deposit executed through <see cref="OrganizeExecutor"/> over the fake layer, happy path
/// and injected faults (reservation refused, destination full, inventory changed, chest changed before the run), with conservation and
/// untouched excluded stacks after every case, and the counts flowing into <see cref="OrganizeRunReport"/> (deposit contract 6.10).
/// </summary>
public sealed class DepositExecutionTests
{
    /// <summary>The excluded stacks of inventory I, which must be in the live inventory, by value, after every run.</summary>
    private static readonly ItemStackSnapshot[] ExcludedStacks =
    {
        S("SwordIron", 1, 1, 2), S("CookedMeat", 5), S("HelmetBronze", 1, 1), S("Resin", 12), S("ArrowWood", 20), S("Hammer", 1, 1),
    };

    // GW-U-1070 (AC-4.9, AC-4.8): the happy path completes all 5 moves (130 items, 4 stacks) in one batch; excluded stacks stay.
    [Fact]
    public void TheDepositCompletesThroughTheExecutor()
    {
        Harness h = Harness.ForI();

        List<int> steps = h.Run();

        Assert.Equal(ExecutionStatus.Completed, h.Result.Status);
        Assert.Equal(5, h.Result.MovesCompleted);
        Assert.Equal(4, h.Result.StacksMoved);
        Assert.Equal(130, h.Result.ItemsMoved);
        Assert.Equal(1, h.Result.ChestsPlanned);
        Assert.Equal(1, h.Result.ChestsCompleted);
        Assert.Equal(2, h.Result.NoRoomStacks);
        Assert.Equal(21, h.Result.NoRoomItems);
        Assert.Equal(1, h.Result.SkippedChests);
        Assert.Equal(5, steps.Sum());
        h.AssertConserved();
        h.AssertExcludedUntouched();
        h.AssertLiveTotalsMatchLayouts();
        Assert.Equal(new[] { "Wood x50 q1 max50", "Wood x50 q1 max50" }, h.Live(301).Contents.Select(PlanAssert.Describe));
        Assert.Equal(new[] { "Wood x30 q1 max50" }, h.Live(306).Contents.Select(PlanAssert.Describe));
        Assert.Equal(new[] { "Stone x30 q1 max50" }, h.Live(304).Contents.Select(PlanAssert.Describe));
        Assert.Equal(h.Input.Containers.Single(c => c.Id.Id == 305), h.Live(305));
        h.AssertAllReleased();
    }

    // GW-U-1071 (AC-4.8 fault): a refused reservation on destination 306 stops the run before any move.
    [Fact]
    public void ARefusedReservationStopsBeforeAnyMove()
    {
        Harness h = Harness.ForI(fake => fake.InjectFault(Id(306), FakeFault.ReservationRefused));

        h.Run();

        Assert.Equal(ExecutionStatus.Stopped, h.Result.Status);
        Assert.Equal(ExecutionStopReason.ReservationRefused, h.Result.StopReason);
        Assert.Equal(Id(306), h.Result.FailedChest);
        Assert.Null(h.Result.FailedMove);
        Assert.Equal(0, h.Result.MovesCompleted);
        h.AssertConserved();
        h.AssertExcludedUntouched();
        h.AssertWorldUnchanged();
        h.AssertAllReleased();
    }

    // GW-U-1072 (AC-4.8 fault): a full destination 304 stops the run after the three Wood moves; the Stone stays in the inventory.
    [Fact]
    public void AFullDestinationStopsTheRunAndKeepsTheRest()
    {
        Harness h = Harness.ForI(fake => fake.InjectFault(Id(304), FakeFault.DestinationFull));

        h.Run();

        Assert.Equal(ExecutionStatus.Stopped, h.Result.Status);
        Assert.Equal(ExecutionStopReason.DestinationFull, h.Result.StopReason);
        Assert.Equal(Id(304), h.Result.FailedChest);
        Assert.Same(h.Plan.Moves[3], h.Result.FailedMove);
        Assert.Equal(3, h.Result.MovesCompleted);
        Assert.Equal(2, h.Result.MovesRemaining);
        Assert.Equal(90, h.Result.ItemsMoved);
        h.AssertConserved();
        h.AssertExcludedUntouched();
        Assert.Contains(h.LiveInventory().Contents, stack => stack == S("Stone", 30));
        Assert.Contains(h.LiveInventory().Contents, stack => stack == S("Wood", 10));
        Assert.Empty(h.Live(304).Contents);
        h.AssertAllReleased();
    }

    // GW-U-1073 (AC-4.8 fault, X2): an inventory whose remove fails rolls the add back; nothing is lost or duplicated.
    [Fact]
    public void AChangedInventoryRollsTheAddBack()
    {
        Harness h = Harness.ForI(fake => fake.InjectFault(PlayerId, FakeFault.ChestChanged));

        h.Run();

        Assert.Equal(ExecutionStatus.Stopped, h.Result.Status);
        Assert.Equal(ExecutionStopReason.ChestChanged, h.Result.StopReason);
        Assert.Equal(PlayerId, h.Result.FailedChest);
        Assert.Same(h.Plan.Moves[0], h.Result.FailedMove);
        Assert.Equal(0, h.Result.MovesCompleted);
        Assert.Equal(0, h.Result.UnrecoveredItems);
        h.AssertConserved();
        h.AssertExcludedUntouched();
        h.AssertWorldUnchanged();
        h.AssertAllReleased();
    }

    // GW-U-1074 (AC-4.8 fault, X5): a destination that changed between planning and running fails verification; nothing moves.
    [Fact]
    public void AChestChangedBeforeTheRunStopsAtVerification()
    {
        Harness h = Harness.ForI(fake =>
        {
            FakeFixtures.Grant(fake, Id(306));
            Assert.Equal(ContainerOperationResult.Success, fake.AddItems(Id(306), S("Stone", 1)));
            fake.ReleaseReservation(Id(306));
        });

        h.Run();

        Assert.Equal(ExecutionStatus.Stopped, h.Result.Status);
        Assert.Equal(ExecutionStopReason.ChestChanged, h.Result.StopReason);
        Assert.Equal(Id(306), h.Result.FailedChest);
        Assert.Null(h.Result.FailedMove);
        Assert.Equal(0, h.Result.MovesCompleted);
        h.AssertConserved();
        h.AssertExcludedUntouched();
        h.AssertWorldUnchanged();
        h.AssertAllReleased();
    }

    // GW-U-1075 (AC-4.9, S-E004-009 contract 10.2): the counts flow into OrganizeRunReport and the deposit summary key.
    [Fact]
    public void TheCountsFlowIntoTheRunReport()
    {
        Harness h = Harness.ForI();
        h.Run();
        OrganizeExecutionResult result = h.Result;

        OrganizeRunReport report = OrganizeRunReport.FromPlan(h.Plan, result.StacksMoved, result.Status == ExecutionStatus.Stopped, null);
        Assert.Equal(4, report.MovedStacks);
        Assert.Equal(2, report.NoRoomStacks);
        Assert.False(report.StoppedEarly);
        Assert.Equal(new[] { SkipReason.Keep }, report.Skipped.Select(entry => entry.Kind));
        Assert.Equal("Put away: 4 stacks moved, 2 no room", StringTable.English.Format(StringKeys.MessageDepositSummary, report.MovedStacks, report.NoRoomStacks));
        Assert.Equal("Organized 4 stacks; 1 chest skipped (Keep); 2 stacks had no room", OrganizeSummaryFormatter.Summary(report));

        Harness stopped = Harness.ForI(fake => fake.InjectFault(Id(304), FakeFault.DestinationFull));
        stopped.Run();
        OrganizeRunReport stoppedReport = OrganizeRunReport.FromPlan(stopped.Plan, stopped.Result.StacksMoved, stopped.Result.Status == ExecutionStatus.Stopped, "chest full");
        Assert.Equal(2, stoppedReport.MovedStacks);
        Assert.True(stoppedReport.StoppedEarly);
        Assert.Equal("Organized 2 stacks; stopped early: chest full; 1 chest skipped (Keep); 2 stacks had no room", OrganizeSummaryFormatter.Summary(stoppedReport));
    }

    /// <summary>The Deposit execution harness: plan I into D, seed the fake with the inventory and every chest, arrange faults, run.</summary>
    private sealed class Harness
    {
        private Harness(DepositPlan deposit, OrganizeFixtures.PlanInput input, FakeContainerLayer fake, OrganizeExecutor executor, IReadOnlyList<ContainerSnapshot> before)
        {
            Deposit = deposit;
            Input = input;
            Fake = fake;
            Executor = executor;
            Before = before;
        }

        public DepositPlan Deposit { get; }

        public OrganizeFixtures.PlanInput Input { get; }

        public FakeContainerLayer Fake { get; }

        public OrganizeExecutor Executor { get; }

        public IReadOnlyList<ContainerSnapshot> Before { get; }

        public OrganizePlan Plan => Deposit.Plan;

        public OrganizeExecutionResult Result => Executor.Result;

        public static Harness ForI(Action<FakeContainerLayer>? arrange = null)
        {
            DepositPlan deposit = RunI();
            OrganizeFixtures.PlanInput input = D();
            var fake = new FakeContainerLayer(deposit.Player.Container);
            foreach (ContainerSnapshot container in input.Containers)
            {
                fake.AddChest(container);
            }

            arrange?.Invoke(fake);
            IReadOnlyList<ContainerSnapshot> before = fake.Capture();
            FakeSettingsSource source = new FakeSettingsSource().WithInt(SettingKeys.MovesPerFrame, 20).WithInt(SettingKeys.ReserveTimeoutMs, 1500);
            var executor = new OrganizeExecutor(deposit.Plan, fake, new ManualClock(TimeSpan.Zero), new BaseStorageSettings(source));
            return new Harness(deposit, input, fake, executor, before);
        }

        public List<int> Run()
        {
            var steps = new List<int>();
            while (!Executor.IsComplete)
            {
                Assert.True(steps.Count < 1000, "The executor did not complete within 1000 steps.");
                steps.Add(Executor.Step());
            }

            return steps;
        }

        public ContainerSnapshot Live(uint serial) => FakeFixtures.Get(Fake, Id(serial));

        public ContainerSnapshot LiveInventory() => FakeFixtures.Get(Fake, PlayerId);

        public void AssertConserved() => ConservationAssert.Holds(Before, Fake.Capture());

        /// <summary>Every excluded stack of inventory I is still in the live inventory by value, and the excluded item totals are unchanged.</summary>
        public void AssertExcludedUntouched()
        {
            ContainerSnapshot live = LiveInventory();
            foreach (ItemStackSnapshot excluded in ExcludedStacks)
            {
                Assert.Contains(live.Contents, stack => stack == excluded);
            }

            Assert.Equal(6, Deposit.Excluded.Count);
            Assert.Equal(ExcludedStacks, Deposit.Excluded.Select(entry => entry.Source.Stack));
        }

        public void AssertWorldUnchanged() => Assert.Equal(Before, Fake.Capture());

        public void AssertLiveTotalsMatchLayouts()
        {
            foreach (ChestLayout layout in Plan.Layouts)
            {
                Assert.Equal(PlanAssert.ItemTotals(layout.Stacks), PlanAssert.ItemTotals(FakeFixtures.Get(Fake, layout.Id).Contents));
            }
        }

        public void AssertAllReleased()
        {
            foreach (ContainerSnapshot chest in Fake.GetContainers())
            {
                Assert.Equal(ReservationStatus.None, Fake.GetReservationStatus(chest.Id));
            }
        }
    }
}
