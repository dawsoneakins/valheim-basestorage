namespace BaseStorage.Core.Tests.Consumption;

using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Tests.Settings;
using Xunit;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Consumption.ConsumptionFixtures;

/// <summary>AC-1 (contract 2.2, CX5, F1): the reservation phase of an execution, before any take.</summary>
public sealed class ExecutorReserveTests
{
    // GW-U-500 (AC-1, CX5): Execute requests every chest in ascending id order and takes nothing.
    [Fact]
    public void ExecuteReservesEveryChestInAscendingOrderAndTakesNothing()
    {
        RecordingContainerLayer layer = LayerW();
        layer.Inner.AutoResolveReservations = false;
        ManualClock clock = new(System.TimeSpan.FromSeconds(10));
        FakeSettingsSource settings = new();
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionExecution execution = Executor(layer, clock, settings).Execute(PlanW());

        Assert.Equal(new[] { "Request:5", "Request:7" }, layer.Calls);
        Assert.Equal(2, clock.Reads);
        Assert.Equal(1, settings.IntCalls);
        Assert.False(execution.IsComplete);
        Assert.Null(execution.Result);
        Assert.Equal(ReservationStatus.Pending, layer.Inner.GetReservationStatus(Id(5)));
        Assert.Equal(ReservationStatus.Pending, layer.Inner.GetReservationStatus(Id(7)));
        Assert.Equal(before, layer.Inner.Capture());
    }

    // GW-U-500 (AC-1, CX5): the constructor makes no call on the layer, the clock or the settings.
    [Fact]
    public void TheConstructorMakesNoCall()
    {
        RecordingContainerLayer layer = LayerW();
        ManualClock clock = new(System.TimeSpan.FromSeconds(10));
        FakeSettingsSource settings = new();

        _ = Executor(layer, clock, settings);

        Assert.Empty(layer.Calls);
        Assert.Equal(0, clock.Reads);
        Assert.Equal(0, settings.IntCalls);
    }

    // GW-U-501 (AC-1, F1): a refused reservation consumes nothing and reports the refused chest.
    [Fact]
    public void ARefusedChestFailsWithDeniedAndConsumesNothing()
    {
        RecordingContainerLayer layer = LayerW();
        layer.Inner.InjectFault(Id(7), FakeFault.ReservationRefused);
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();
        ConsumptionExecution execution = Executor(layer).Execute(PlanW());

        Assert.Null(execution.Poll());
        ConsumptionResult result = Done(execution.Poll());

        Assert.Equal(ConsumptionOutcome.ReservationFailed, result.Outcome);
        Assert.Equal(Id(7), result.FailedContainer);
        Assert.Equal(ReservationFailure.Denied, result.ReservationFailure);
        AssertUntouched(result, layer, before);
    }

    // GW-U-501 (AC-1, F1): when both chests are refused, the lowest id is the reported one.
    [Fact]
    public void WhenEveryChestIsRefusedTheLowestIdIsReported()
    {
        RecordingContainerLayer layer = LayerW();
        layer.Inner.InjectFault(Id(5), FakeFault.ReservationRefused);
        layer.Inner.InjectFault(Id(7), FakeFault.ReservationRefused);
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();
        ConsumptionExecution execution = Executor(layer).Execute(PlanW());

        ConsumptionResult result = Drive(execution);

        Assert.Equal(ConsumptionOutcome.ReservationFailed, result.Outcome);
        Assert.Equal(Id(5), result.FailedContainer);
        Assert.Equal(ReservationFailure.Denied, result.ReservationFailure);
        AssertUntouched(result, layer, before);
    }
}
