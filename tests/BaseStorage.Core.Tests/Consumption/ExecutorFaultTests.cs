namespace BaseStorage.Core.Tests.Consumption;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Tests.Locking;
using Xunit;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Consumption.ConsumptionFixtures;

/// <summary>AC-4 (contract 2.2, 3.6, CX8, CX10, F10): a layer exception completes as Faulted, rolls back, releases, and is rethrown as the same instance.</summary>
public sealed class ExecutorFaultTests
{
    // GW-U-533 (AC-4, F10, 3.6): a layer exception mid-take completes as Faulted, rolls back, releases, and rethrows the same instance.
    [Fact]
    public void ALayerExceptionMidTakeCompletesAsFaultedAndRethrows()
    {
        RecordingContainerLayer layer = LayerW();
        InvalidOperationException boom = new("layer");
        int removes = 0;
        layer.ThrowOn = call => call.StartsWith("Remove:", StringComparison.Ordinal) && ++removes == 2 ? boom : null;
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();
        ConsumptionExecution execution = Executor(layer).Execute(PlanW());
        Assert.Null(execution.Poll());

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() => execution.Poll());

        Assert.Same(boom, thrown);
        ConsumptionResult result = Done(execution.Result);
        Assert.Equal(ConsumptionOutcome.Faulted, result.Outcome);
        Assert.Equal(default(ContainerId), result.FailedContainer);
        Assert.Empty(result.Unrestored);
        Assert.Equal(new[] { "Add:1:Wood/3", "Release:5", "Release:7" }, Tail(layer, "Remove:5:0:Wood/8:8"));
        AssertConserved(result, layer, before);

        string[] callsBefore = layer.Calls.ToArray();
        Assert.Same(result, Done(execution.Poll()));
        Assert.Equal(callsBefore, layer.Calls);
        Assert.False(Executor(layer).Execute(PlanW()).IsComplete);
    }

    // GW-U-534 (AC-4, 2.2, 3.6, ADR-GWIN-030): other exceptions also release every chest and rethrow the same instance.
    [Theory]
    [InlineData("request throws")]
    [InlineData("status throws on the first poll")]
    [InlineData("verify fetch throws")]
    [InlineData("clock throws after execute")]
    public void OtherExceptionsReleaseEveryChestAndRethrow(string caseName)
    {
        InvalidOperationException boom = new(caseName);
        CountingClock clock = new();
        RecordingContainerLayer layer = LayerW();
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();
        ConsumptionExecutor executor = Executor(layer, clock);

        if (caseName == "request throws")
        {
            layer.ThrowOn = call => call == "Request:7" ? boom : null;
            InvalidOperationException refused = Assert.Throws<InvalidOperationException>(() => executor.Execute(PlanW()));
            Assert.Same(boom, refused);
            Assert.Equal(new[] { "Request:5", "Request:7", "Release:5", "Release:7" }, layer.Calls);
            ConservationAssert.Holds(before, layer.Inner.Capture());
            AssertAllReleased(layer, Id(5), Id(7));
            layer.ThrowOn = null;
            Assert.False(executor.Execute(PlanW()).IsComplete);
            return;
        }

        if (caseName == "status throws on the first poll")
        {
            layer.ThrowOn = call => call == "Status:5" ? boom : null;
        }
        else if (caseName == "verify fetch throws")
        {
            layer.ThrowOn = call => call == "Get:7" ? boom : null;
        }

        ConsumptionExecution execution = executor.Execute(PlanW());
        if (caseName == "clock throws after execute")
        {
            clock.Throws = boom;
        }

        if (caseName == "verify fetch throws")
        {
            Assert.Null(execution.Poll());
        }

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() => execution.Poll());

        Assert.Same(boom, thrown);
        ConsumptionResult result = Done(execution.Result);
        Assert.Equal(ConsumptionOutcome.Faulted, result.Outcome);
        Assert.DoesNotContain(layer.Calls, call => call.StartsWith("Remove:", StringComparison.Ordinal));
        AssertConserved(result, layer, before);
    }
}
