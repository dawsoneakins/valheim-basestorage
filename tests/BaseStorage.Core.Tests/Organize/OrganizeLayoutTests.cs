namespace BaseStorage.Core.Tests.Organize;

using System.Linq;
using BaseStorage.Core.Organize;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.PlanAssert;

/// <summary>GW-U-554: the layouts per role are exact, in snapshot order, and the claimed overflow layouts are the same instances (AC-6).</summary>
public sealed class OrganizeLayoutTests
{
    // GW-U-554
    [Fact]
    public void BaseELayoutsAreExactAndInSnapshotOrder()
    {
        OrganizePlan plan = Run(E());

        Assert.Equal(
            new[]
            {
                Layout(201, ChestRole.Category, "Wood", S("Wood", 50), S("Wood", 50)),
                Layout(202, ChestRole.Category, "Stone", S("Flint", 5), S("Stone", 50), S("Stone", 5)),
                Layout(203, ChestRole.Category, "Stone", S("Flint", 7), S("Stone", 40)),
                Layout(204, ChestRole.ClaimedOverflow, "Misc", S("Resin", 12)),
                Layout(208, ChestRole.ClaimedOverflow, "Wood", S("Wood", 35)),
                Layout(209, ChestRole.Source, null, S("CookedMeat", 5), S("TrophyDeer", 1, 20)),
            },
            LayoutsOf(plan));
    }

    // GW-U-554
    [Fact]
    public void ClaimedOverflowEntriesAreTheSameInstancesAsTheMatchingLayouts()
    {
        OrganizePlan plan = Run(E());

        Assert.Equal(new uint[] { 208, 204 }, plan.ClaimedOverflow.Select(l => l.Id.Id));
        for (int i = 0; i < plan.ClaimedOverflow.Count; i++)
        {
            Assert.Same(plan.FindLayout(plan.ClaimedOverflow[i].Id), plan.ClaimedOverflow[i]);
        }
    }

    // GW-U-554
    [Fact]
    public void UnusedOverflowChestHasOneEmptyLayoutWithNoCategory()
    {
        OrganizePlan plan = Run(Make(new[] { C(704, 0, 0, 2) }));

        Assert.Equal(Layout(704, ChestRole.UnusedOverflow, null), LayoutRowOf(plan.Layouts[0]));
        Assert.Null(plan.Layouts[0].CategoryName);
        Assert.Empty(plan.Layouts[0].Stacks);
    }
}
