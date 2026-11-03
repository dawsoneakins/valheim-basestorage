namespace BaseStorage.Core.Tests.Restock;

using System.Collections.Generic;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Restock.RestockFixtures;

/// <summary>S-QR-001 AC-4 and AC-5 (contract 7.8): empty and degenerate inputs.</summary>
public sealed class RestockDegenerateTests
{
    // GW-U-2509 (7.8 row 3)
    [Fact]
    public void UnknownAssignmentIdsAreIgnored()
    {
        var unknown = new Dictionary<ContainerId, CategoryAssignment> { [Id(999)] = CategoryAssignment.Keep };

        RestockPlan actual = RestockPlanner.Plan(MainCarried(), Origin(), MainChests(), unknown, NotCraftable());
        RestockPlan expected = RestockPlanner.Plan(MainCarried(), Origin(), MainChests(), AssignR(), Craftable());

        Assert.Equal(Shape(expected), Shape(actual));
        Assert.Equal(expected.MovedItems, actual.MovedItems);
        Assert.Equal(expected.ToppedUpStacks, actual.ToppedUpStacks);
        Assert.Equal(expected.RemainingRoom, actual.RemainingRoom);
        Assert.Equal(expected.LeftInUse, actual.LeftInUse);
        Assert.Equal(expected.LeftDifferentIdentity, actual.LeftDifferentIdentity);
        Assert.Equal(expected.LeftBehind, actual.LeftBehind);
        Assert.Equal(expected.CarriedAfter.Count, actual.CarriedAfter.Count);
        for (int t = 0; t < expected.CarriedAfter.Count; t++)
        {
            Assert.True(expected.CarriedAfter[t] == actual.CarriedAfter[t], "CarriedAfter[" + t + "] differs.");
        }
    }

    // GW-U-2519 (7.8 rows 1-2)
    [Fact]
    public void EmptyInputsGiveAnEmptyPlan()
    {
        RestockPlan none = RestockPlanner.Plan(new List<CarriedStack>(), Origin(), MainChests(), AssignR(), Craftable());
        Assert.Empty(none.Moves);
        Assert.Empty(none.CarriedAfter);
        Assert.Equal(0, none.MovedItems);
        Assert.Equal(0, none.ToppedUpStacks);
        Assert.Equal(0, none.RemainingRoom);
        Assert.Equal(0, none.LeftInUse);
        Assert.Equal(0, none.LeftDifferentIdentity);
        Assert.Equal(0, none.LeftBehind);

        var carried = new List<CarriedStack> { H(S("Wood", 10)) };
        RestockPlan noChests = RestockPlanner.Plan(carried, Origin(), new List<ContainerSnapshot>(), NoAssign(), Craftable());
        Assert.Empty(noChests.Moves);
        Assert.Same(carried[0].Stack, noChests.CarriedAfter[0]);
        Assert.Equal(40, noChests.RemainingRoom);
        Assert.Equal(0, noChests.LeftBehind);
    }
}
