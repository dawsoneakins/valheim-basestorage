namespace BaseStorage.Core.Tests.Consumption;

using System;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Tests.Fakes;
using Xunit;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Consumption.ConsumptionFixtures;

/// <summary>AC-4 (spec AC-4, CX6, CX8): the conservation sweep over every failure scenario, each on a fresh world.</summary>
public sealed class ExecutorConservationTests
{
    // GW-U-537 (AC-4, CX6): every failure scenario conserves items, releases both chests, is not a success, and carries the materials-moved key.
    [Theory]
    [MemberData(nameof(ConsumptionScenarios.FailureCases), MemberType = typeof(ConsumptionScenarios))]
    public void EveryFailureScenarioConservesItemsAndReleasesEveryChest(string name)
    {
        ScenarioRun run = ConsumptionScenarios.Run(name);

        ConservationAssert.Holds(run.Before, run.After);
        AssertAllReleased(run.Layer, Id(5), Id(7));
        Assert.False(run.Result.IsSuccess);
        Assert.Equal(MaterialsMoved, run.Result.MessageKey);
        if (name is "F1" or "F2" or "F4")
        {
            Assert.DoesNotContain(
                run.Layer.Calls,
                call => call.StartsWith("Remove:", StringComparison.Ordinal) || call.StartsWith("Add:", StringComparison.Ordinal));
        }
    }
}
