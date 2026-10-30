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
using static BaseStorage.Core.Tests.Fakes.FakeFixtures;

/// <summary>AC-3 (contract 4.2, CX7, CX9): the happy path, its exact call sequence, its final world, and the success ordering.</summary>
public sealed class ExecutorHappyPathTests
{
    // GW-U-520 (AC-3, 4.2, CX9): the exact call sequence and clock reads of the happy path, as literals.
    [Fact]
    public void HappyPathFollowsTheExactCallSequenceAndClockReads()
    {
        ManualClock clock = new(TimeSpan.FromSeconds(10));
        RecordingContainerLayer layer = LayerW();
        ConsumptionExecution execution = Executor(layer, clock).Execute(PlanW());
        Assert.Equal(new[] { "Request:5", "Request:7" }, layer.Calls);
        Assert.Equal(2, clock.Reads);

        int mark = layer.Calls.Count;
        Assert.Null(execution.Poll());
        Assert.Equal(new[] { "Status:5=Pending", "Status:7=Pending" }, Since(layer, mark));
        Assert.Equal(4, clock.Reads);

        mark = layer.Calls.Count;
        ConsumptionResult result = Done(execution.Poll());
        Assert.Equal(
            new[]
            {
                "Status:5=Granted", "Status:7=Granted", "InvId", "Get:1", "Get:5", "Get:7", "Get:1", "Remove:1:0:Wood/3:3",
                "Get:5", "Remove:5:0:Wood/8:8", "Get:7", "Remove:7:0:Wood/10:4", "Get:5", "Remove:5:0:Stone/4:2",
                "Release:5", "Release:7",
            },
            Since(layer, mark));
        Assert.Equal(6, clock.Reads);
        Assert.Equal(ConsumptionOutcome.Success, result.Outcome);

        mark = layer.Calls.Count;
        int reads = clock.Reads;
        Assert.Same(result, Done(execution.Poll()));
        Assert.Empty(Since(layer, mark));
        Assert.Equal(reads, clock.Reads);
    }

    // GW-U-521 (AC-3, 4.2): the final world, the final result, and the exact per-type consumption.
    [Fact]
    public void HappyPathEndsWithTheContractFinalWorldAndResult()
    {
        RecordingContainerLayer layer = LayerW();
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();
        ConsumptionPlan plan = PlanW();

        ConsumptionResult result = Drive(Executor(layer).Execute(plan));

        Assert.Equal(
            new[]
            {
                Make(1u, 0f, 0f, 0f, 8, 4, ContainerFlags.None),
                Make(5u, 3f, 0f, 4f, 5, 2, ContainerFlags.PlayerBuilt, Stone(2)),
                Make(7u, 6f, 0f, 8f, 5, 2, ContainerFlags.PlayerBuilt, Wood(6)),
            },
            layer.Inner.Capture());
        Assert.Equal(ConsumptionOutcome.Success, result.Outcome);
        Assert.True(result.IsSuccess);
        Assert.Equal(default(ContainerId), result.FailedContainer);
        Assert.Equal(ReservationFailure.None, result.ReservationFailure);
        Assert.Null(result.FailedOperation);
        Assert.Empty(result.Unrestored);
        Assert.Null(result.MessageKey);
        AssertConsumedExactly(before, layer.Inner.Capture(), plan.Requirements);
    }

    // GW-U-522 (AC-3, CX7): every layer call during the verify, take and release phase sees an incomplete execution with no result.
    [Fact]
    public void SuccessIsReportedOnlyAfterEveryTakeAndEveryRelease()
    {
        RecordingContainerLayer layer = LayerW();
        ConsumptionExecution execution = Executor(layer).Execute(PlanW());
        Assert.Null(execution.Poll());
        List<(string Call, bool IsComplete, ConsumptionResult? Result)> observed = new();
        layer.Observe = call => observed.Add((call, execution.IsComplete, execution.Result));

        ConsumptionResult result = Done(execution.Poll());

        Assert.Equal("Release:7", observed.Last().Call);
        Assert.All(observed, entry =>
        {
            Assert.False(entry.IsComplete);
            Assert.Null(entry.Result);
        });
        Assert.Same(result, execution.Result);
    }

    // GW-U-522 (AC-3, CX7, F7): on a mid-way take failure IsSuccess is never seen as true, and no call follows the final release.
    [Fact]
    public void OnATakeFailureNothingIsCalledAfterTheFinalReleaseAndNoSuccessIsSeen()
    {
        RecordingContainerLayer layer = LayerW();
        layer.Inner.InjectFault(Id(7), FakeFault.ChestChanged);
        ConsumptionExecution execution = Executor(layer).Execute(PlanW());
        Assert.Null(execution.Poll());
        List<(string Call, bool IsComplete, bool IsSuccess)> observed = new();
        layer.Observe = call => observed.Add((call, execution.IsComplete, execution.Result?.IsSuccess ?? false));

        ConsumptionResult result = Done(execution.Poll());

        Assert.Equal(ConsumptionOutcome.TakeFailed, result.Outcome);
        Assert.False(result.IsSuccess);
        Assert.Equal("Release:7", observed.Last().Call);
        Assert.All(observed, entry =>
        {
            Assert.False(entry.IsComplete);
            Assert.False(entry.IsSuccess);
        });
    }
}
