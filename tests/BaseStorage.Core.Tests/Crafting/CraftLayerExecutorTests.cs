namespace BaseStorage.Core.Tests.Crafting;

using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;

/// <summary>GW-U-1106, GW-U-1108 and GW-U-1109 (AC-2 and AC-4, consumption contract 4.3): the executor over the craft layer.</summary>
public sealed class CraftLayerExecutorTests
{
    // GW-U-1106 (4.2 last paragraph): the executor over the craft layer runs the happy path to Success.
    [Fact]
    public void TheExecutorOverTheCraftLayerConsumesThePlannedAmounts()
    {
        var rig = new CraftRig();
        ConsumptionPlan plan = rig.Plan() ?? throw new InvalidOperationException("The fixture has no plan.");

        ConsumptionResult result = rig.Pump(rig.Executor.Execute(plan));

        Assert.Equal(ConsumptionOutcome.Success, result.Outcome);
        Assert.Empty(rig.World.ContentsOf(CraftFixtures.Inventory));
        Assert.Equal(new[] { "Resin 2" }, rig.Describe(CraftFixtures.C5));
        Assert.Equal(new[] { "Wood 6" }, rig.Describe(CraftFixtures.C7));
        Assert.All(new[] { CraftFixtures.C5, CraftFixtures.C7 }, id => Assert.Equal(ReservationStatus.None, rig.StatusOf(id)));
    }

    // GW-U-1108 (F1 deny, F2 timeout, F5 contents changed, F7 take failure): the outcome, conservation, chests None and no kept clone.
    [Theory]
    [InlineData("deny", ConsumptionOutcome.ReservationFailed)]
    [InlineData("timeout", ConsumptionOutcome.ReservationFailed)]
    [InlineData("changed", ConsumptionOutcome.ContentsChanged)]
    [InlineData("take", ConsumptionOutcome.TakeFailed)]
    public void FaultsOverTheCraftLayerRollBackAndConsumeNothing(string scenario, ConsumptionOutcome expected)
    {
        var rig = new CraftRig();
        ConsumptionPlan plan = rig.Plan() ?? throw new InvalidOperationException("The fixture has no plan.");
        Action<CraftRig>? onRequest = null;
        switch (scenario)
        {
            case "deny":
                onRequest = r => r.Driver.OnOpenResponse(r.Chest7, false);
                break;
            case "timeout":
                onRequest = r => r.Clock.Advance(TimeSpan.FromMilliseconds(CraftFixtures.ReserveTimeoutMs));
                break;
            case "changed":
                rig.World.MoveOut(CraftFixtures.C5, 0, 1);
                break;
            case "take":
                rig.World.FailTakeNumber = 3;
                break;
        }

        IEnumerable<ContainerSnapshot> before = rig.World.Views().ToArray();
        ConsumptionExecution execution = rig.Executor.Execute(plan);
        ConsumptionResult result = rig.Pump(execution, onRequest);

        Assert.Equal(expected, result.Outcome);
        ConservationAssert.Holds(before, rig.World.Views());
        Assert.All(new[] { CraftFixtures.C5, CraftFixtures.C7 }, id => Assert.Equal(ReservationStatus.None, rig.StatusOf(id)));
        Assert.Empty(rig.World.Kept);
    }

    // GW-U-1109 (F7): every PutBack matched a kept clone, so the inventory and the chest get back the same stacks.
    [Fact]
    public void RollbackPutsBackTheSameStacks()
    {
        var rig = new CraftRig();
        rig.World.FailTakeNumber = 3;
        ConsumptionPlan plan = rig.Plan() ?? throw new InvalidOperationException("The fixture has no plan.");

        ConsumptionResult result = rig.Pump(rig.Executor.Execute(plan));

        Assert.Equal(ConsumptionOutcome.TakeFailed, result.Outcome);
        Assert.Contains(rig.World.ContentsOf(CraftFixtures.Inventory), stack => stack.PrefabName == "Wood" && stack.Count == 3 && stack.Quality == 1);
        Assert.Contains(rig.World.ContentsOf(CraftFixtures.C5), stack => stack.PrefabName == "Wood" && stack.Count == 8 && stack.Quality == 1);
    }

    // GW-U-1109: a PutBack with no matching clone returns DestinationFull and adds nothing.
    [Fact]
    public void APutBackWithNoMatchingCloneAddsNothing()
    {
        var rig = new CraftRig();
        int before = rig.World.ContentsOf(CraftFixtures.C5).Count;

        ContainerOperationResult result = rig.World.PutBack(CraftFixtures.C5, CraftFixtures.Wood(5));

        Assert.Equal(ContainerOperationResult.DestinationFull, result);
        Assert.Equal(before, rig.World.ContentsOf(CraftFixtures.C5).Count);
    }
}
