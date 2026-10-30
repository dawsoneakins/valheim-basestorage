namespace BaseStorage.Core.Tests.Consumption;

using System;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using Xunit;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Consumption.ConsumptionFixtures;

/// <summary>AC-1 and AC-4 (contract 3.7, CX8, F11): every chest is released once per execution, and a release exception never changes the outcome.</summary>
public sealed class ExecutorReleaseTests
{
    // GW-U-507 (AC-1, CX8): on a reservation failure every chest is released once, in ascending order, including ones never granted.
    [Theory]
    [InlineData("F1")]
    [InlineData("F2")]
    [InlineData("F4")]
    [InlineData("unknown")]
    public void EveryChestIsReleasedOnceInAscendingOrderOnAReservationFailure(string name)
    {
        ScenarioRun run = ConsumptionScenarios.Run(name);

        Assert.Equal(new[] { "Release:5", "Release:7" }, run.Layer.Calls.TakeLast(2));
        Assert.Equal(
            new[] { "Release:5", "Release:7" },
            run.Layer.Calls.Where(call => call.StartsWith("Release:", StringComparison.Ordinal)));
        Assert.DoesNotContain(
            run.Layer.Calls,
            call => call.StartsWith("Get:", StringComparison.Ordinal)
                || call.StartsWith("Remove:", StringComparison.Ordinal)
                || call.StartsWith("Add:", StringComparison.Ordinal));
        AssertAllReleased(run.Layer, Id(5), Id(7));
    }

    // GW-U-535 (AC-4, CX8, F11): a release exception on chest 5 never changes the success, and chest 7 is still released.
    [Fact]
    public void AReleaseExceptionNeverChangesTheSuccessOutcome()
    {
        RecordingContainerLayer layer = LayerW();
        layer.ThrowOn = call => call == "Release:5" ? new InvalidOperationException("release") : null;

        ConsumptionResult result = Drive(Executor(layer).Execute(PlanW()));

        Assert.Equal(ConsumptionOutcome.Success, result.Outcome);
        Assert.True(result.IsSuccess);
        Assert.Null(result.MessageKey);
        Assert.Contains("Release:7", layer.Calls);
        Assert.Equal(ReservationStatus.None, layer.Inner.GetReservationStatus(Id(7)));
        Assert.Equal(FinalWorld(), layer.Inner.Capture());
    }

    // GW-U-535 (AC-4, CX8, F11): the same release exception on the refused scenario keeps the failure and its conservation.
    [Fact]
    public void AReleaseExceptionOnAFailureKeepsTheFailure()
    {
        RecordingContainerLayer layer = LayerW();
        layer.Inner.InjectFault(Id(7), FakeFault.ReservationRefused);
        layer.ThrowOn = call => call == "Release:5" ? new InvalidOperationException("release") : null;
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();

        ConsumptionResult result = Drive(Executor(layer).Execute(PlanW()));

        Assert.Equal(ConsumptionOutcome.ReservationFailed, result.Outcome);
        Assert.Equal(Id(7), result.FailedContainer);
        Assert.Equal(MaterialsMoved, result.MessageKey);
        ConservationAssert.Holds(before, layer.Inner.Capture());
        Assert.Equal(ReservationStatus.None, layer.Inner.GetReservationStatus(Id(7)));
    }

    // The world of contract 4.2 after the happy path: inventory empty, chest 5 holds Stone 2, chest 7 holds Wood 6.
    private static System.Collections.Generic.IReadOnlyList<ContainerSnapshot> FinalWorld() => new[]
    {
        Make(1u, 0f, 0f, 0f, 8, 4, ContainerFlags.None),
        Make(5u, 3f, 0f, 4f, 5, 2, ContainerFlags.PlayerBuilt, FakeFixtures.Stone(2)),
        Make(7u, 6f, 0f, 8f, 5, 2, ContainerFlags.PlayerBuilt, FakeFixtures.Wood(6)),
    };
}
