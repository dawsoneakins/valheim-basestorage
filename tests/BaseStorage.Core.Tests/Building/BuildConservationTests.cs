namespace BaseStorage.Core.Tests.Building;

using System.Globalization;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Consumption;
using BaseStorage.Core.Tests.Crafting;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Plugin.Building;
using BaseStorage.Plugin.Crafting;

/// <summary>
/// GW-U-1609, GW-U-1610, GW-U-1627 to GW-U-1630 (AC-5, AC-8; contract BF1, section 11 BT1 to BT6): the real executor over the
/// fake container layer, and the session over the craft world, conserve every item.
/// </summary>
public sealed class BuildConservationTests
{
    private static RecordingContainerLayer LayerFor(ContainerSnapshot inventory, params ContainerSnapshot[] chests)
    {
        var fake = new FakeContainerLayer(inventory);
        foreach (ContainerSnapshot chest in chests)
        {
            fake.AddChest(chest);
        }

        return new RecordingContainerLayer(fake);
    }

    private static ConsumptionPlan PiecePlan(ContainerSnapshot inventory, params ContainerSnapshot[] chests)
    {
        ConsumptionPlan? plan = ConsumptionPlanner.Plan(CraftFixtures.Snapshot(inventory, chests), BuildRig.PieceRequirements());
        Assert.NotNull(plan);
        return plan!;
    }

    private static ConsumptionExecutor ExecutorOver(IContainerLayer layer, IClock clock) =>
        new ConsumptionExecutor(layer, clock, CraftFixtures.Settings());

    private static ContainerSnapshot Inventory() => BuildRig.InventoryOf(CraftFixtures.Wood(3));

    // GW-U-1609 (BT6, AC-5): the planner takes the inventory, then the nearer chest's stacks smallest first, then the farther chest.
    [Fact]
    public void ThePlannerTakesTheInventoryThenTheNearestChestSmallestStackFirst()
    {
        ContainerSnapshot far = BuildRig.ChestOf(CraftFixtures.C7, 10f, ContainerFlags.PlayerBuilt, CraftFixtures.Wood(10));
        ContainerSnapshot near = BuildRig.ChestOf(CraftFixtures.C5, 5f, ContainerFlags.PlayerBuilt, CraftFixtures.Wood(2), CraftFixtures.Wood(8));

        ConsumptionPlan? woodPlan = ConsumptionPlanner.Plan(CraftFixtures.Snapshot(Inventory(), new[] { near, far }), new[] { new CraftRequirement("Wood", 15) });
        Assert.NotNull(woodPlan);
        ConsumptionPlan plan = woodPlan!;

        Assert.Equal(
            new[]
            {
                (CraftFixtures.Inventory, "Wood", 3),
                (CraftFixtures.C5, "Wood", 2),
                (CraftFixtures.C5, "Wood", 8),
                (CraftFixtures.C7, "Wood", 2),
            },
            plan.Takes.Select(take => (take.ContainerId, take.Stack.PrefabName, take.Count)).ToArray());
    }

    // GW-U-1610 (BT3, AC-5): a granted execution takes inventory first, then c5 before c7 with the smallest stack first; totals drop by exactly the piece.
    [Fact]
    public void ASuccessfulExecutionFollowsThePlanOrderAndConservesTheTotals()
    {
        ContainerSnapshot inventory = Inventory();
        ContainerSnapshot c5 = BuildRig.C5Snapshot();
        ContainerSnapshot c7 = BuildRig.C7Snapshot();
        RecordingContainerLayer layer = LayerFor(inventory, c5, c7);
        ConsumptionPlan plan = PiecePlan(inventory, c5, c7);
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionResult result = ConsumptionFixtures.Drive(ExecutorOver(layer, new ManualClock(CraftFixtures.Start)).Execute(plan));

        Assert.Equal(ConsumptionOutcome.Success, result.Outcome);
        string[] removes = layer.Calls.Where(call => call.StartsWith("Remove:", StringComparison.Ordinal)).Select(call => call.Split(':')[1]).ToArray();
        Assert.Equal(new[] { "0", "5", "7", "5" }, removes);
        Assert.Equal(
            plan.Takes.Select(take => take.ContainerId.Id.ToString(CultureInfo.InvariantCulture)).ToArray(),
            removes);

        SortedDictionary<string, int> start = ConsumptionFixtures.Totals(before);
        SortedDictionary<string, int> end = ConsumptionFixtures.Totals(layer.Inner.Capture());
        Assert.Equal(start["Wood"] - 15, end["Wood"]);
        Assert.Equal(start["Resin"] - 2, end["Resin"]);
        ConsumptionFixtures.AssertAllReleased(layer, CraftFixtures.C5, CraftFixtures.C7);
    }

    // GW-U-1627 (BT1, AC-8): a denied reservation consumes nothing: no removal, no addition, conservation holds, every chest released.
    [Fact]
    public void ADeniedReservationConsumesNothing()
    {
        ContainerSnapshot inventory = Inventory();
        RecordingContainerLayer layer = LayerFor(inventory, BuildRig.C5Snapshot(), BuildRig.C7Snapshot());
        layer.Inner.InjectFault(CraftFixtures.C7, FakeFault.ReservationRefused);
        ConsumptionPlan plan = PiecePlan(inventory, BuildRig.C5Snapshot(), BuildRig.C7Snapshot());
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionResult result = ConsumptionFixtures.Drive(ExecutorOver(layer, new ManualClock(CraftFixtures.Start)).Execute(plan));

        Assert.Equal(ConsumptionOutcome.ReservationFailed, result.Outcome);
        Assert.Equal(CraftFixtures.C7, result.FailedContainer);
        Assert.DoesNotContain(layer.Calls, call => call.StartsWith("Remove:", StringComparison.Ordinal) || call.StartsWith("Add:", StringComparison.Ordinal));
        ConservationAssert.Holds(before, layer.Inner.Capture());
        ConsumptionFixtures.AssertAllReleased(layer, CraftFixtures.C5, CraftFixtures.C7);
    }

    // GW-U-1628 (BT2, AC-8): a timeout at 1500 ms is a reservation failure and takes nothing.
    [Fact]
    public void ATimeoutTakesNothingAndConserves()
    {
        ContainerSnapshot inventory = Inventory();
        RecordingContainerLayer layer = LayerFor(inventory, BuildRig.C5Snapshot(), BuildRig.C7Snapshot());
        layer.Inner.AutoResolveReservations = false;
        var clock = new ManualClock(CraftFixtures.Start);
        ConsumptionPlan plan = PiecePlan(inventory, BuildRig.C5Snapshot(), BuildRig.C7Snapshot());
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();
        ConsumptionExecution execution = ExecutorOver(layer, clock).Execute(plan);

        clock.Advance(TimeSpan.FromMilliseconds(CraftFixtures.ReserveTimeoutMs));
        ConsumptionResult result = ConsumptionFixtures.Drive(execution);

        Assert.Equal(ConsumptionOutcome.ReservationFailed, result.Outcome);
        Assert.DoesNotContain(layer.Calls, call => call.StartsWith("Remove:", StringComparison.Ordinal));
        ConservationAssert.Holds(before, layer.Inner.Capture());
    }

    // GW-U-1628 (BT2, AC-8): a chest whose contents changed is ContentsChanged at that chest, nothing is taken and conservation holds.
    [Fact]
    public void AChangedChestIsContentsChangedAndConserves()
    {
        ContainerSnapshot inventory = Inventory();
        ContainerSnapshot movedC5 = BuildRig.ChestOf(CraftFixtures.C5, 5f, ContainerFlags.PlayerBuilt, CraftFixtures.Wood(7), CraftFixtures.Resin(4));
        RecordingContainerLayer layer = LayerFor(inventory, movedC5, BuildRig.C7Snapshot());
        ConsumptionPlan plan = PiecePlan(inventory, BuildRig.C5Snapshot(), BuildRig.C7Snapshot());
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionResult result = ConsumptionFixtures.Drive(ExecutorOver(layer, new ManualClock(CraftFixtures.Start)).Execute(plan));

        Assert.Equal(ConsumptionOutcome.ContentsChanged, result.Outcome);
        Assert.Equal(CraftFixtures.C5, result.FailedContainer);
        ConservationAssert.Holds(before, layer.Inner.Capture());
    }

    // GW-U-1629 (BT4, AC-8, CX6): a fault on the c7 take after the earlier takes rolls every take back; the outcome is not Success.
    [Fact]
    public void AMidTakeFaultRollsBackAndConserves()
    {
        ContainerSnapshot inventory = Inventory();
        RecordingContainerLayer layer = LayerFor(inventory, BuildRig.C5Snapshot(), BuildRig.C7Snapshot());
        layer.Inner.InjectFault(CraftFixtures.C7, FakeFault.ChestChanged);
        ConsumptionPlan plan = PiecePlan(inventory, BuildRig.C5Snapshot(), BuildRig.C7Snapshot());
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionResult result = ConsumptionFixtures.Drive(ExecutorOver(layer, new ManualClock(CraftFixtures.Start)).Execute(plan));

        Assert.NotEqual(ConsumptionOutcome.Success, result.Outcome);
        ConservationAssert.Holds(before, layer.Inner.Capture());
        ConsumptionFixtures.AssertAllReleased(layer, CraftFixtures.C5, CraftFixtures.C7);
    }

    // GW-U-1630 (BT5, T2, AC-8): a denied grant has no commit and conserves the world.
    [Fact]
    public void SessionDeniedGrantConservesTheWorld()
    {
        var rig = new BuildRig();
        var before = rig.World.Views().ToArray();
        rig.StartFixture();

        rig.Driver.OnOpenResponse(rig.Chest7, false);
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.DoesNotContain("Commit", rig.BuildGame.Calls);
        ConservationAssert.Holds(before, rig.World.Views());
    }

    // GW-U-1630 (BT5, T3, AC-8): a chest moved by another player before the grant has no commit, and conserves everything but that move.
    [Fact]
    public void SessionMovedChestConservesEverythingButTheExternalMove()
    {
        var rig = new BuildRig();
        rig.StartFixture();
        rig.World.MoveOut(CraftFixtures.C5, 0, 1);
        var before = rig.World.Views().ToArray();

        rig.Grant(rig.Chest7);
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.DoesNotContain("Commit", rig.BuildGame.Calls);
        ConservationAssert.Holds(before, rig.World.Views());
    }

    // GW-U-1630 (BT5, T4, AC-8): a timeout has no commit and conserves the world.
    [Fact]
    public void SessionTimeoutConservesTheWorld()
    {
        var rig = new BuildRig();
        rig.StartFixture();
        var before = rig.World.Views().ToArray();

        rig.Clock.Advance(TimeSpan.FromMilliseconds(CraftFixtures.ReserveTimeoutMs));
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.DoesNotContain("Commit", rig.BuildGame.Calls);
        ConservationAssert.Holds(before, rig.World.Views());
    }

    // GW-U-1630 (BT5, T12, AC-8): a refused commit returns every taken stack, and the totals equal those before.
    [Fact]
    public void SessionRefusedCommitReturnsEverythingAndConserves()
    {
        var rig = new BuildRig();
        rig.BuildGame.CommitBody = _ => { };
        var before = rig.World.Views().ToArray();

        rig.RunS1();

        Assert.Equal(BuildEnd.Returned, rig.Session.LastEnd);
        ConservationAssert.Holds(before, rig.World.Views());
    }

    // GW-U-1630 (BT5, T14, AC-8): a creation refused by the guard takes nothing.
    [Fact]
    public void SessionGuardedCreationTakesNothing()
    {
        var rig = new BuildRig();
        var before = rig.World.Views().ToArray();

        rig.Session.EnterGuard();
        Assert.False(rig.Session.OnPlacePiece(false));
        Assert.False(rig.Session.AfterTryPlacePiece(true));

        Assert.Empty(rig.BuildGame.Calls);
        ConservationAssert.Holds(before, rig.World.Views());
    }
}
