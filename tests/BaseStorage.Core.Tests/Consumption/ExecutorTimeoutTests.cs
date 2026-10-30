namespace BaseStorage.Core.Tests.Consumption;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Reservations;
using Xunit;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Consumption.ConsumptionFixtures;

/// <summary>AC-1 (contract 3.2, F2, F3): the reserve timeout is exact, measured only on the manual clock.</summary>
public sealed class ExecutorTimeoutTests
{
    // GW-U-502 (AC-1, F2): the timeout is exactly ReserveTimeoutMs, measured on the manual clock.
    [Fact]
    public void TheTimeoutIsExactlyReserveTimeoutMsOnTheManualClock()
    {
        ManualClock clock = new(TimeSpan.FromSeconds(10));
        RecordingContainerLayer layer = LayerW();
        layer.Inner.AutoResolveReservations = false;
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();
        ConsumptionExecution execution = Executor(layer, clock).Execute(PlanW());

        Assert.Null(execution.Poll());
        clock.Advance(TimeSpan.FromMilliseconds(1499));
        Assert.Null(execution.Poll());
        clock.Advance(TimeSpan.FromMilliseconds(1));
        ConsumptionResult result = Done(execution.Poll());

        Assert.Equal(ConsumptionOutcome.ReservationFailed, result.Outcome);
        Assert.Equal(Id(5), result.FailedContainer);
        Assert.Equal(ReservationFailure.Timeout, result.ReservationFailure);
        AssertUntouched(result, layer, before);
    }

    // GW-U-503 (AC-1, 3.2 step 2): a grant at or past the deadline fails, even though it arrived.
    [Fact]
    public void AGrantAtOrPastTheDeadlineFails()
    {
        ManualClock clock = new(TimeSpan.FromSeconds(10));
        RecordingContainerLayer layer = LayerW();
        layer.Inner.AutoResolveReservations = false;
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();
        ConsumptionExecution execution = Executor(layer, clock).Execute(PlanW());

        clock.Advance(TimeSpan.FromMilliseconds(1500));
        layer.Inner.ResolvePendingReservations();
        ConsumptionResult result = Done(execution.Poll());

        Assert.Equal(ConsumptionOutcome.ReservationFailed, result.Outcome);
        Assert.Equal(ReservationFailure.Timeout, result.ReservationFailure);
        Assert.Equal(Id(5), result.FailedContainer);
        Assert.DoesNotContain(layer.Calls, call => call.StartsWith("Get:", StringComparison.Ordinal));
        AssertUntouched(result, layer, before);
    }

    // GW-U-503 (AC-1, 3.2 step 2): a grant one millisecond before the deadline succeeds and consumes exactly.
    [Fact]
    public void AGrantBeforeTheDeadlineSucceeds()
    {
        ManualClock clock = new(TimeSpan.FromSeconds(10));
        RecordingContainerLayer layer = LayerW();
        layer.Inner.AutoResolveReservations = false;
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();
        ConsumptionPlan plan = PlanW();
        ConsumptionExecution execution = Executor(layer, clock).Execute(plan);

        clock.Advance(TimeSpan.FromMilliseconds(1499));
        layer.Inner.ResolvePendingReservations();
        ConsumptionResult result = Done(execution.Poll());

        Assert.Equal(ConsumptionOutcome.Success, result.Outcome);
        Assert.Null(result.MessageKey);
        AssertConsumedExactly(before, layer.Inner.Capture(), plan.Requirements);
    }
}
