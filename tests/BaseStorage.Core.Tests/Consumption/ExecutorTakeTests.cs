namespace BaseStorage.Core.Tests.Consumption;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Tests.Settings;
using Xunit;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Consumption.ConsumptionFixtures;
using static BaseStorage.Core.Tests.Fakes.FakeFixtures;

/// <summary>AC-3 (contract 3.4, 3.7 F13): takes are grouped per slot, located by value, and the inventory-only plan takes without reserving.</summary>
public sealed class ExecutorTakeTests
{
    // GW-U-523 (AC-3, 3.4): takes on one slot are merged into one removal of their total.
    [Fact]
    public void TakesOnOneSlotAreMergedIntoOneRemove()
    {
        ContainerSnapshot inventory = Make(1u, 0f, 0f, 0f, 8, 4, ContainerFlags.None);
        ContainerSnapshot chest5 = Make(5u, 3f, 0f, 4f, 5, 2, ContainerFlags.PlayerBuilt, Wood(8));
        ConsumptionPlan plan = PlanFor(
            inventory,
            new[] { chest5 },
            new[] { new CraftRequirement("Wood", 3), new CraftRequirement("Wood", 4) });
        Assert.Equal(new[] { 3, 4 }, plan.Takes.Where(take => take.ContainerId == Id(5) && take.StackIndex == 0).Select(take => take.Count));
        RecordingContainerLayer layer = LayerOf(inventory, chest5);
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionResult result = Drive(Executor(layer).Execute(plan));

        Assert.Equal(
            new[] { "Remove:5:0:Wood/8:7" },
            layer.Calls.Where(call => call.StartsWith("Remove:", StringComparison.Ordinal)));
        Assert.Equal(new[] { Wood(1) }, FakeFixtures.Get(layer.Inner, Id(5)).Contents);
        Assert.Equal(ConsumptionOutcome.Success, result.Outcome);
        AssertConsumedExactly(before, layer.Inner.Capture(), plan.Requirements);
    }

    // GW-U-524 (AC-3, 3.4 step 2, G3): after the whole-stack remove, the Stone take locates its stack at index 0.
    [Fact]
    public void TheStoneTakeLocatesItsStackByValueAfterRenumbering()
    {
        RecordingContainerLayer layer = LayerW();

        ConsumptionResult result = Drive(Executor(layer).Execute(PlanW()));

        Assert.Contains("Remove:5:0:Stone/4:2", layer.Calls);
        Assert.Equal(ConsumptionOutcome.Success, result.Outcome);
    }

    // GW-U-524 (AC-3, 3.4 step 2): when the planned slot holds another stack, the lowest value match is removed, and the world is conserved.
    [Fact]
    public void ALocateByValueRemovesTheLowestMatchingSlot()
    {
        RecordingContainerLayer layer = LayerW();
        layer.GetOverride(Id(7), 2, Make(7u, 6f, 0f, 8f, 5, 2, ContainerFlags.PlayerBuilt, Stack("Resin", 1, 1, 50), Wood(10)));
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionResult result = Drive(Executor(layer).Execute(PlanW()));

        Assert.Contains("Remove:7:1:Wood/10:4", layer.Calls);
        Assert.Equal(ConsumptionOutcome.TakeFailed, result.Outcome);
        Assert.Equal(ContainerOperationResult.ContainerChanged, result.FailedOperation);
        AssertConserved(result, layer, before);
    }

    // GW-U-524 (AC-3, 3.4 step 2): when no stack matches by value, the take fails with ContainerChanged, nothing is removed, and the earlier takes are rolled back.
    [Fact]
    public void ANoMatchTakeFailsWithContainerChangedWithoutRemoving()
    {
        RecordingContainerLayer layer = LayerW();
        layer.GetOverride(Id(7), 2, Make(7u, 6f, 0f, 8f, 5, 2, ContainerFlags.PlayerBuilt, Wood(9)));
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionResult result = Drive(Executor(layer).Execute(PlanW()));

        Assert.Equal(ConsumptionOutcome.TakeFailed, result.Outcome);
        Assert.Equal(ContainerOperationResult.ContainerChanged, result.FailedOperation);
        Assert.DoesNotContain(layer.Calls, call => call.StartsWith("Remove:7:", StringComparison.Ordinal));
        Assert.Equal(new[] { "Add:5:Wood/8", "Add:1:Wood/3", "Release:5", "Release:7" }, layer.Calls.TakeLast(4));
        AssertConserved(result, layer, before);
    }

    // GW-U-526 (AC-3, 2.4): a layer result that stops a removal is reported as the failed operation, without delegating.
    [Fact]
    public void ANotReservedRemovalIsReportedAsTheFailedOperation()
    {
        RecordingContainerLayer layer = LayerW();
        layer.ResultOverride = call => call.StartsWith("Remove:7:", StringComparison.Ordinal) ? ContainerOperationResult.NotReserved : null;
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionResult result = Drive(Executor(layer).Execute(PlanW()));

        Assert.Equal(ConsumptionOutcome.TakeFailed, result.Outcome);
        Assert.Equal(Id(7), result.FailedContainer);
        Assert.Equal(ContainerOperationResult.NotReserved, result.FailedOperation);
        AssertConserved(result, layer, before);
    }

    // GW-U-526 (AC-3, 2.4): a container that is missing during the take phase fails with NotFound and removes nothing from chest 7.
    [Fact]
    public void AMissingContainerDuringTheTakeFailsWithNotFound()
    {
        RecordingContainerLayer layer = LayerW();
        layer.GetOverride(Id(7), 2, null);
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionResult result = Drive(Executor(layer).Execute(PlanW()));

        Assert.Equal(ConsumptionOutcome.TakeFailed, result.Outcome);
        Assert.Equal(ContainerOperationResult.NotFound, result.FailedOperation);
        Assert.DoesNotContain(layer.Calls, call => call.StartsWith("Remove:7:", StringComparison.Ordinal));
        AssertConserved(result, layer, before);
    }

    // GW-U-527 (AC-3, F13): an inventory-only plan makes no layer call and no clock read at Execute, and one Poll takes.
    [Fact]
    public void AnInventoryOnlyPlanMakesNoLayerCallAtExecuteAndTakesInOnePoll()
    {
        ContainerSnapshot inventory = Make(1u, 0f, 0f, 0f, 8, 4, ContainerFlags.None, Wood(20));
        ConsumptionPlan plan = PlanFor(inventory, Array.Empty<ContainerSnapshot>(), new[] { new CraftRequirement("Wood", 5) });
        ManualClock clock = new(TimeSpan.FromSeconds(10));
        FakeSettingsSource settings = new();
        RecordingContainerLayer layer = LayerOf(inventory);
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionExecution execution = Executor(layer, clock, settings).Execute(plan);

        Assert.Empty(layer.Calls);
        Assert.Equal(0, clock.Reads);
        Assert.Equal(1, settings.IntCalls);
        ConsumptionResult result = Done(execution.Poll());
        Assert.Equal(new[] { "InvId", "Get:1", "Get:1", "Remove:1:0:Wood/20:5" }, layer.Calls);
        Assert.Equal(ConsumptionOutcome.Success, result.Outcome);
        AssertConsumedExactly(before, layer.Inner.Capture(), plan.Requirements);
    }
}
