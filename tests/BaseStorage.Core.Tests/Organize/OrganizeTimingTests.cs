namespace BaseStorage.Core.Tests.Organize;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BaseStorage.Core.Organize;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>The non-parallel collection of the timing test (ADR-GWIN-039): no other test runs while the planner is timed.</summary>
[CollectionDefinition(OrganizeTimingCollection.Name, DisableParallelization = true)]
public sealed class OrganizeTimingCollection
{
    /// <summary>The collection name.</summary>
    internal const string Name = "Organize timing (non-parallel)";
}

/// <summary>GW-U-572 (AC-14, P12): 60 chests of 32 slots plan in under 50 ms, measured as the minimum of five calls after a warm-up.</summary>
[Collection(OrganizeTimingCollection.Name)]
public sealed class OrganizeTimingTests
{
    private const double BudgetMilliseconds = 50.0;
    private const int Samples = 5;

    // GW-U-572
    [Fact]
    public void SixtyChestsOfThirtyTwoSlotsPlanInUnderFiftyMillisecondsAtTheMinimumOfFiveCalls()
    {
        PlanInput input = PerfBase();
        Assert.Equal(60, input.Containers.Length);
        Assert.All(input.Containers, container => Assert.Equal(32, container.SlotCount));

        _ = Run(input);

        var milliseconds = new List<double>(Samples);
        OrganizePlan? last = null;
        for (int i = 0; i < Samples; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            last = Run(input);
            stopwatch.Stop();
            milliseconds.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        double minimum = milliseconds.Min();
        Assert.True(
            minimum < BudgetMilliseconds,
            "Minimum of 5 timed Plan calls was " + minimum.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)
                + " ms; samples (ms): " + string.Join(", ", milliseconds.Select(ms => ms.ToString("F3", System.Globalization.CultureInfo.InvariantCulture))));

        Assert.NotNull(last);
        OrganizePlan timed = last!;
        Assert.NotEmpty(timed.Moves);
        Assert.NotEmpty(timed.ClaimedOverflow);
        PlanReplay.ReplayResult replay = PlanReplay.Replay(input.Containers, timed);
        Assert.Equal(timed.Moves.Count, replay.Steps.Count);
    }
}
