namespace BaseStorage.Core.Tests.Organize;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.PlanAssert;

/// <summary>GW-U-567 and GW-U-568: the same input gives the same plan, and the inputs are never mutated (AC-12, P2).</summary>
public sealed class OrganizeDeterminismTests
{
    // GW-U-567
    [Fact]
    public void PlanEIsElementWiseEqualAcrossTwoCalls()
    {
        PlanInput input = E();

        AssertSamePlan(Run(input), Run(input));
    }

    // GW-U-567
    [Fact]
    public void PerformanceBaseIsElementWiseEqualAcrossTwoCalls()
    {
        PlanInput input = PerfBase();

        AssertSamePlan(Run(input), Run(input));
    }

    // GW-U-567 (derived)
    [Fact]
    public void ReverseInsertionOrderOfTheDictionariesGivesTheSamePlan()
    {
        PlanInput input = E();
        PlanInput reversed = input with
        {
            Assignments = Reversed(input.Assignments),
            InUseBy = Reversed(input.InUseBy),
            Kinds = Reversed(input.Kinds),
        };

        AssertSamePlan(Run(input), Run(reversed));
    }

    // GW-U-568
    [Fact]
    public void BaseEContainersAreUnchangedAfterPlanning()
    {
        PlanInput input = E();
        PlanInput copy = E();

        Run(input);

        Assert.Equal(copy.Containers.Length, input.Containers.Length);
        for (int i = 0; i < copy.Containers.Length; i++)
        {
            Assert.Equal(copy.Containers[i], input.Containers[i]);
        }
    }

    // GW-U-568
    [Fact]
    public void DictionariesAreUnchangedAndResultsShareTheInputInstances()
    {
        PlanInput input = E();
        PlanInput copy = E();

        OrganizePlan plan = Run(input);

        Assert.Equal(copy.Assignments.Count, input.Assignments.Count);
        Assert.Equal(copy.InUseBy.Count, input.InUseBy.Count);
        Assert.Equal(copy.Kinds.Count, input.Kinds.Count);
        foreach (KeyValuePair<ContainerId, CategoryAssignment> pair in copy.Assignments)
        {
            Assert.Equal(pair.Value, input.Assignments[pair.Key]);
        }

        foreach (KeyValuePair<ContainerId, string> pair in copy.InUseBy)
        {
            Assert.Equal(pair.Value, input.InUseBy[pair.Key]);
        }

        Assert.Same(input.Containers[0], plan.Layouts[0].Container);
        Assert.Same(input.Containers[4], plan.Skipped[0].Container);
    }

    private static Dictionary<TKey, TValue> Reversed<TKey, TValue>(Dictionary<TKey, TValue> source)
        where TKey : notnull =>
        source.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value);
}
