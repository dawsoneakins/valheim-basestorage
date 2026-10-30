namespace BaseStorage.Core.Tests.Organize;

using System.Linq;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Organize;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.PlanAssert;

/// <summary>GW-U-540 and GW-U-542: base E of contract 6.2, its exact move list and its auto-assign result.</summary>
public sealed class OrganizeBaseETests
{
    // GW-U-540
    [Fact]
    public void PlanEMovesAreTheContractListInOrderWithEveryProperty()
    {
        OrganizePlan plan = Run(E());

        Assert.Equal(
            new[]
            {
                Move(201, 1, 202, "Stone", 1, 50, 10, "Stone"),
                Move(203, 0, 201, "Wood", 1, 50, 20, "Wood"),
                Move(209, 0, 201, "Wood", 1, 50, 40, "Wood"),
                Move(209, 1, 201, "Wood", 1, 50, 10, "Wood"),
                Move(209, 1, 208, "Wood", 1, 50, 35, "Wood"),
                Move(209, 2, 204, "Resin", 1, 50, 12, "Misc"),
            },
            MovesOf(plan));
    }

    // GW-U-542
    [Fact]
    public void AutoAssignChestsAndOutcomesAreExposedForBaseE()
    {
        OrganizePlan plan = Run(E());

        Assert.Equal(new uint[] { 201, 202, 203, 204, 208, 209 }, plan.AutoAssign.Chests.Select(c => c.Id.Id));
        Assert.Equal(
            new[]
            {
                AutoAssignOutcome.AlreadyAssigned,
                AutoAssignOutcome.AlreadyAssigned,
                AutoAssignOutcome.Assigned,
                AutoAssignOutcome.Overflow,
                AutoAssignOutcome.Overflow,
                AutoAssignOutcome.SourceOnly,
            },
            plan.AutoAssign.Chests.Select(c => c.Outcome));
        Assert.Equal("Stone", plan.AutoAssign.Find(Id(203))?.CategoryName);
    }

    // GW-U-542
    [Fact]
    public void OverflowChestsAreExposedAndTheAssignedChestKeepsItsStackInPlace()
    {
        OrganizePlan plan = Run(E());

        Assert.Equal(new uint[] { 204, 208 }, plan.AutoAssign.OverflowChests.Select(c => c.Id.Id));
        Assert.DoesNotContain(plan.Moves, m => m.Source == Id(203) && (m.SourceStackIndex == 1 || m.SourceStackIndex == 2));
    }
}
