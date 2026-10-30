namespace BaseStorage.Core.Tests.Consumption;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Tests.Reservations;
using Xunit;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Consumption.ConsumptionFixtures;

/// <summary>AC-1 (contract 3.1, ADR-GWIN-030): the layer status maps to reservation events, and a failure is attributed before release.</summary>
public sealed class ExecutorStatusMappingTests
{
    // GW-U-505 (AC-1, 3.1): the status mapping table, one named row per case, each untouched.
    [Theory]
    [InlineData("owner disconnect while requesting")]
    [InlineData("unknown chest")]
    [InlineData("granted then lost while another chest is pending")]
    [InlineData("denied")]
    public void EachStatusMappingRowReportsTheExpectedChest(string caseName)
    {
        ManualClock clock = new(TimeSpan.FromSeconds(10));
        RecordingContainerLayer layer = caseName == "unknown chest" ? LayerOf(InventoryW(), Chest5W()) : LayerW();
        ConsumptionPlan plan = PlanW();
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();
        ContainerId expectedChest = Id(5);
        ReservationFailure expectedFailure = ReservationFailure.OwnerLost;
        ConsumptionResult result;

        switch (caseName)
        {
            case "owner disconnect while requesting":
                {
                    ConsumptionExecution execution = Executor(layer, clock).Execute(plan);
                    Assert.Null(execution.Poll());
                    layer.Inner.InjectFault(Id(5), FakeFault.OwnerDisconnect);
                    result = Done(execution.Poll());
                    break;
                }

            case "unknown chest":
                {
                    expectedChest = Id(7);
                    result = Drive(Executor(layer, clock).Execute(plan));
                    break;
                }

            case "granted then lost while another chest is pending":
                {
                    layer.Inner.AutoResolveReservations = false;
                    ConsumptionExecution execution = Executor(layer, clock).Execute(plan);
                    layer.Inner.AutoResolveReservations = true;
                    layer.Inner.GetReservationStatus(Id(5));
                    layer.Inner.AutoResolveReservations = false;
                    Assert.Null(execution.Poll());
                    layer.Inner.InjectFault(Id(5), FakeFault.OwnerDisconnect);
                    result = Done(execution.Poll());
                    break;
                }

            default:
                {
                    layer.Inner.InjectFault(Id(5), FakeFault.ReservationRefused);
                    expectedFailure = ReservationFailure.Denied;
                    result = Drive(Executor(layer, clock).Execute(plan));
                    break;
                }
        }

        Assert.Equal(ConsumptionOutcome.ReservationFailed, result.Outcome);
        Assert.Equal(expectedChest, result.FailedContainer);
        Assert.Equal(expectedFailure, result.ReservationFailure);
        AssertUntouched(result, layer, before);
    }

    // GW-U-506 (AC-1, ADR-GWIN-030): the failure is attributed to the lost chest before release, not to a cancelled lower-id chest.
    [Fact]
    public void TheFailureIsAttributedToTheLostChestBeforeRelease()
    {
        RecordingContainerLayer layer = LayerW();
        layer.Inner.AutoResolveReservations = false;
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();
        ConsumptionExecution execution = Executor(layer).Execute(PlanW());
        layer.Inner.InjectFault(Id(7), FakeFault.OwnerDisconnect);

        ConsumptionResult result = Done(execution.Poll());

        Assert.Equal(Id(7), result.FailedContainer);
        Assert.Equal(ReservationFailure.OwnerLost, result.ReservationFailure);
        Assert.Equal(new[] { "Release:5", "Release:7" }, layer.Calls.TakeLast(2));
        AssertUntouched(result, layer, before);
    }
}
