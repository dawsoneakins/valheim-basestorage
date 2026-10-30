namespace BaseStorage.Core.Tests.Consumption;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Tests.Settings;
using Xunit;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Consumption.ConsumptionFixtures;

/// <summary>AC-1 and AC-4 (contract 2.2, 2.3, CX10, F12, F14): one execution at a time, argument validation, and Cancel.</summary>
public sealed class ExecutorLifecycleTests
{
    // GW-U-508 (AC-1, F14, CX10): a second Execute while one is pending throws, and adds no call and no settings read.
    [Fact]
    public void ASecondExecuteWhilePendingThrowsWithoutAnyCall()
    {
        RecordingContainerLayer layer = LayerW();
        layer.Inner.AutoResolveReservations = false;
        FakeSettingsSource settings = new();
        ConsumptionExecutor executor = Executor(layer, null, settings);
        _ = executor.Execute(PlanW());
        string[] callsBefore = layer.Calls.ToArray();
        int readsBefore = settings.IntCalls;

        Assert.Throws<InvalidOperationException>(() => executor.Execute(PlanW()));

        Assert.Equal(callsBefore, layer.Calls);
        Assert.Equal(readsBefore, settings.IntCalls);
    }

    // GW-U-508 (AC-1, F14): after Cancel the executor accepts a new Execute.
    [Fact]
    public void AfterCancelExecuteSucceeds()
    {
        RecordingContainerLayer layer = LayerW();
        layer.Inner.AutoResolveReservations = false;
        ConsumptionExecutor executor = Executor(layer);
        ConsumptionExecution first = executor.Execute(PlanW());
        first.Cancel();

        ConsumptionExecution second = executor.Execute(PlanW());

        Assert.False(second.IsComplete);
    }

    // GW-U-508 (AC-1, CX10): after a success the executor accepts a new Execute.
    [Fact]
    public void AfterASuccessExecuteSucceeds()
    {
        ConsumptionExecutor executor = Executor(LayerW());
        Assert.Equal(ConsumptionOutcome.Success, Drive(executor.Execute(PlanW())).Outcome);

        ConsumptionExecution next = executor.Execute(PlanW());

        Assert.False(next.IsComplete);
    }

    // GW-U-508 (AC-1, CX10): a null plan is rejected first, with its parameter name, even while another execution is pending.
    [Fact]
    public void ANullPlanIsRejectedEvenWhileAnExecutionIsPending()
    {
        RecordingContainerLayer layer = LayerW();
        layer.Inner.AutoResolveReservations = false;
        ConsumptionExecutor executor = Executor(layer);
        _ = executor.Execute(PlanW());

        ArgumentNullException thrown = Assert.Throws<ArgumentNullException>(() => executor.Execute(null!));

        Assert.Equal("plan", thrown.ParamName);
    }

    // GW-U-536 (AC-4, F12): Cancel releases every chest and completes as Cancelled; a later Poll or Cancel changes nothing.
    [Fact]
    public void CancelReleasesEveryChestAndCompletesAsCancelled()
    {
        ManualClock clock = new(TimeSpan.FromSeconds(10));
        RecordingContainerLayer layer = LayerW();
        layer.Inner.AutoResolveReservations = false;
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();
        ConsumptionExecution execution = Executor(layer, clock).Execute(PlanW());
        Assert.Null(execution.Poll());

        execution.Cancel();
        ConsumptionResult result = Done(execution.Result);

        Assert.Equal(ConsumptionOutcome.Cancelled, result.Outcome);
        Assert.Equal(new[] { "Release:5", "Release:7" }, layer.Calls.TakeLast(2));
        AssertUntouched(result, layer, before);

        string[] callsBefore = layer.Calls.ToArray();
        int readsBefore = clock.Reads;
        Assert.Same(result, Done(execution.Poll()));
        execution.Cancel();
        Assert.Same(result, Done(execution.Result));
        Assert.Equal(callsBefore, layer.Calls);
        Assert.Equal(readsBefore, clock.Reads);
    }

    // GW-U-536 (AC-4, CX8, F12): Cancel does not throw when a release throws, and the other chest is still released.
    [Fact]
    public void CancelDoesNotThrowWhenARelease()
    {
        RecordingContainerLayer layer = LayerW();
        layer.Inner.AutoResolveReservations = false;
        ConsumptionExecution execution = Executor(layer).Execute(PlanW());
        Assert.Null(execution.Poll());
        layer.ThrowOn = call => call == "Release:5" ? new InvalidOperationException("release") : null;

        execution.Cancel();

        Assert.Contains("Release:7", layer.Calls);
        Assert.Equal(ConsumptionOutcome.Cancelled, Done(execution.Result).Outcome);
        Assert.Equal(ReservationStatus.None, layer.Inner.GetReservationStatus(Id(7)));
    }
}
