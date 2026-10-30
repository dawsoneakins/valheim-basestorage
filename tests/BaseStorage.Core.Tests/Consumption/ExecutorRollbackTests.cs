namespace BaseStorage.Core.Tests.Consumption;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using Xunit;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Consumption.ConsumptionFixtures;
using static BaseStorage.Core.Tests.Fakes.FakeFixtures;

/// <summary>AC-3 and AC-4 (contract 3.5, F7, F8, F9): a failed take rolls back in reverse, falls back to the inventory, and reports what it could not restore.</summary>
public sealed class ExecutorRollbackTests
{
    // GW-U-525 (AC-3, F7): a mid-way take failure never reports success, is rolled back, and never attempts the Stone take.
    [Fact]
    public void AMidWayTakeFailureNeverReportsSuccessAndIsRolledBack()
    {
        RecordingContainerLayer layer = LayerW();
        layer.Inner.InjectFault(Id(7), FakeFault.ChestChanged);
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionResult result = Drive(Executor(layer).Execute(PlanW()));

        Assert.Equal(ConsumptionOutcome.TakeFailed, result.Outcome);
        Assert.Equal(Id(7), result.FailedContainer);
        Assert.Equal(ContainerOperationResult.ContainerChanged, result.FailedOperation);
        Assert.Empty(result.Unrestored);
        Assert.Equal(new[] { "Add:5:Wood/8", "Add:1:Wood/3", "Release:5", "Release:7" }, Tail(layer, "Remove:7:0:Wood/10:4"));
        Assert.DoesNotContain("Remove:5:0:Stone/4:2", layer.Calls);
        AssertConserved(result, layer, before);
    }

    // GW-U-530 (AC-4, F8): when the source refuses the rollback, the item goes back to the inventory, and the world is conserved.
    [Fact]
    public void ARefusedSourceFallsBackToTheInventory()
    {
        RecordingContainerLayer layer = LayerW();
        layer.Inner.InjectFault(Id(7), FakeFault.ChestChanged);
        layer.Inner.InjectFault(Id(5), FakeFault.DestinationFull);
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionResult result = Drive(Executor(layer).Execute(PlanW()));

        Assert.Equal(ConsumptionOutcome.TakeFailed, result.Outcome);
        Assert.Equal(Id(7), result.FailedContainer);
        Assert.Equal(ContainerOperationResult.ContainerChanged, result.FailedOperation);
        Assert.Empty(result.Unrestored);
        Assert.Equal(
            new[] { "Add:5:Wood/8", "Add:1:Wood/8", "Add:1:Wood/3", "Release:5", "Release:7" },
            Tail(layer, "Remove:7:0:Wood/10:4"));
        Assert.Equal(new[] { Stone(4) }, FakeFixtures.Get(layer.Inner, Id(5)).Contents);
        Assert.Equal(new[] { Wood(11) }, FakeFixtures.Get(layer.Inner, Id(1)).Contents);
        AssertConserved(result, layer, before);
    }

    // GW-U-531 (AC-4, F9): when every destination refuses, the items are reported in Unrestored, never silently lost.
    [Fact]
    public void WhenEveryDestinationRefusesTheItemsAreReportedNotLost()
    {
        RecordingContainerLayer layer = LayerW();
        layer.Inner.InjectFault(Id(7), FakeFault.ChestChanged);
        layer.Inner.InjectFault(Id(5), FakeFault.DestinationFull);
        layer.Inner.InjectFault(Id(1), FakeFault.DestinationFull);
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionResult result = Drive(Executor(layer).Execute(PlanW()));

        Assert.Equal(ConsumptionOutcome.TakeFailed, result.Outcome);
        Assert.Equal(new[] { Wood(8), Wood(3) }, result.Unrestored);
        List<ContainerSnapshot> after = layer.Inner.Capture().ToList();
        after.Add(Make(999u, 0f, 0f, 0f, 8, 4, ContainerFlags.None, result.Unrestored.ToArray()));
        ConservationAssert.Holds(before, after);
        AssertAllReleased(layer, Id(5), Id(7));
        Assert.Equal(MaterialsMoved, result.MessageKey);
    }

    // GW-U-532 (AC-4, 3.5): an exception from AddItems during rollback counts as a refusal, and the next destination is tried.
    [Fact]
    public void AnAddExceptionDuringRollbackCountsAsARefusal()
    {
        RecordingContainerLayer layer = LayerW();
        layer.Inner.InjectFault(Id(7), FakeFault.ChestChanged);
        layer.ThrowOn = call => call == "Add:5:Wood/8" ? new System.InvalidOperationException("add") : null;
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionResult result = Drive(Executor(layer).Execute(PlanW()));

        Assert.Equal(ConsumptionOutcome.TakeFailed, result.Outcome);
        Assert.Equal(Id(7), result.FailedContainer);
        Assert.Empty(result.Unrestored);
        Assert.Equal(
            new[] { "Add:5:Wood/8", "Add:1:Wood/8", "Add:1:Wood/3", "Release:5", "Release:7" },
            Tail(layer, "Remove:7:0:Wood/10:4"));
        AssertConserved(result, layer, before);
    }
}
