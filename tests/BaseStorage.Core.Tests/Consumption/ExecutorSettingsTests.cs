namespace BaseStorage.Core.Tests.Consumption;

using System;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Tests.Settings;
using Xunit;
using static BaseStorage.Core.Tests.Consumption.ConsumptionFixtures;

/// <summary>AC-1 (contract 2.2, CX3, F3): the timeout comes from settings, is read once per Execute, is normalised and is not cached.</summary>
public sealed class ExecutorSettingsTests
{
    // GW-U-504 (AC-1, F3, CX3): the timeout is read once per Execute and normalised to at least 1 ms; 250 ms is exact.
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(250, 250)]
    public void TheTimeoutIsReadOnceAndNormalisedToAtLeastOneMillisecond(int configured, int effectiveMs)
    {
        ManualClock clock = new(TimeSpan.FromSeconds(10));
        RecordingContainerLayer layer = LayerW();
        layer.Inner.AutoResolveReservations = false;
        FakeSettingsSource settings = new FakeSettingsSource().WithInt(SettingKeys.ReserveTimeoutMs, configured);
        ConsumptionExecution execution = Executor(layer, clock, settings).Execute(PlanW());

        Assert.Equal(1, settings.IntCalls);
        clock.Advance(TimeSpan.FromMilliseconds(effectiveMs - 1));
        Assert.Null(execution.Poll());
        clock.Advance(TimeSpan.FromMilliseconds(1));
        ConsumptionResult result = Done(execution.Poll());

        Assert.Equal(ConsumptionOutcome.ReservationFailed, result.Outcome);
        Assert.Equal(ReservationFailure.Timeout, result.ReservationFailure);
        Assert.Equal(1, settings.IntCalls);
    }

    // GW-U-504 (AC-1, CX3): the timeout is not cached; a second Execute on the same executor reads the new value.
    [Fact]
    public void TheTimeoutIsNotCachedAcrossExecutions()
    {
        ManualClock clock = new(TimeSpan.FromSeconds(10));
        RecordingContainerLayer layer = LayerW();
        layer.Inner.AutoResolveReservations = false;
        FakeSettingsSource settings = new();
        ConsumptionExecutor executor = Executor(layer, clock, settings);
        executor.Execute(PlanW()).Cancel();
        settings.WithInt(SettingKeys.ReserveTimeoutMs, 3000);

        ConsumptionExecution second = executor.Execute(PlanW());
        Assert.Equal(2, settings.IntCalls);
        clock.Advance(TimeSpan.FromMilliseconds(1500));
        Assert.Null(second.Poll());
        clock.Advance(TimeSpan.FromMilliseconds(1500));
        ConsumptionResult result = Done(second.Poll());

        Assert.Equal(ConsumptionOutcome.ReservationFailed, result.Outcome);
        Assert.Equal(ReservationFailure.Timeout, result.ReservationFailure);
    }

    // GW-U-504 (AC-1, CX3): a settings read that throws propagates the same instance, with nothing called and nothing left pending.
    [Fact]
    public void ASettingsReadThatThrowsPropagatesAndLeavesNothingPending()
    {
        ManualClock clock = new(TimeSpan.FromSeconds(10));
        RecordingContainerLayer layer = LayerW();
        FakeSettingsSource settings = new();
        InvalidOperationException failure = new("settings unavailable");
        settings.Throws = failure;
        ConsumptionExecutor executor = Executor(layer, clock, settings);

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() => executor.Execute(PlanW()));

        Assert.Same(failure, thrown);
        Assert.Empty(layer.Calls);
        Assert.Equal(0, clock.Reads);
        settings.Throws = null;
        ConsumptionExecution execution = executor.Execute(PlanW());
        Assert.False(execution.IsComplete);
    }
}
