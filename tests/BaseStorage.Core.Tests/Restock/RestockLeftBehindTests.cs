namespace BaseStorage.Core.Tests.Restock;

using System.Collections.Generic;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Restock.RestockFixtures;

/// <summary>S-QR-001 AC-5 (contract 7.4 and 7.7, rules Q6, Q10 and Q12): what stays behind, and what is counted.</summary>
public sealed class RestockLeftBehindTests
{
    // GW-U-2511 (Q6, Q10)
    [Fact]
    public void Q06_Q10_InUseItemsStayAndAreCountedUpToNeed()
    {
        var carried = new List<CarriedStack> { H(S("Wood", 10)) };

        // An in-use chest with plain Wood gives 40 left in use.
        RestockPlan inUse = RestockPlanner.Plan(carried, Origin(), new List<ContainerSnapshot> { CF(937, 1, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Wood", 50)) }, NoAssign(), Craftable());
        Assert.Empty(inUse.Moves);
        Assert.Equal(40, inUse.LeftInUse);

        // Keep, in use, with KeepChestsCraftable off: ignored (Q5 (a) before (c)), so nothing is counted.
        var keep938 = new Dictionary<ContainerId, CategoryAssignment> { [Id(938)] = CategoryAssignment.Keep };
        RestockPlan keptInUse = RestockPlanner.Plan(
            carried, Origin(), new List<ContainerSnapshot> { CF(938, 1, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Wood", 50)) }, keep938, NotCraftable());
        Assert.Equal(0, keptInUse.LeftInUse);

        // In use with an over-full stack: ignored (Q5 (b) before (c)), so nothing is counted.
        RestockPlan overFull = RestockPlanner.Plan(
            carried, Origin(), new List<ContainerSnapshot> { CF(941, 1, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Wood", 60)) }, NoAssign(), Craftable());
        Assert.Equal(0, overFull.LeftInUse);

        // Two in-use chests with 100 Wood in all: the count is capped at the need of 40.
        RestockPlan twoChests = RestockPlanner.Plan(
            carried,
            Origin(),
            new List<ContainerSnapshot>
            {
                CF(942, 1, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Wood", 50)),
                CF(943, 2, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Wood", 50)),
            },
            NoAssign(),
            Craftable());
        Assert.Equal(40, twoChests.LeftInUse);

        // A usable chest with 20 Wood and an in-use chest with 50 Wood: 20 moves, and 20 are left in use.
        RestockPlan usableAndInUse = RestockPlanner.Plan(
            carried,
            Origin(),
            new List<ContainerSnapshot>
            {
                C(965, 1, 0, 2, S("Wood", 20)),
                CF(966, 2, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Wood", 50)),
            },
            NoAssign(),
            Craftable());
        Assert.Equal(new[] { Row(965, 0, 0, 20) }, Shape(usableAndInUse));
        Assert.Equal(20, usableAndInUse.LeftInUse);
    }

    // GW-U-2513 (Q12)
    [Fact]
    public void Q12_ItemsNoTargetWantsAreNotLeftBehind()
    {
        RestockPlan wood = RestockPlanner.Plan(
            new List<CarriedStack> { H(S("Wood", 10)) },
            Origin(),
            new List<ContainerSnapshot> { C(967, 1, 0, 2, S("Wood", 50), S("Wood", 50)) },
            NoAssign(),
            Craftable());
        Assert.Equal(new[] { Row(967, 0, 0, 40) }, Shape(wood));
        Assert.Equal(0, wood.LeftInUse);
        Assert.Equal(0, wood.LeftDifferentIdentity);
        Assert.Equal(0, wood.LeftBehind);

        // On plan a, the items that no target wants change no count, and LeftBehind stays within the room.
        RestockPlan a = RestockPlanner.Plan(MainCarried(), Origin(), MainChests(), AssignR(), Craftable());
        Assert.Equal(30, a.LeftInUse);
        Assert.Equal(20, a.LeftDifferentIdentity);
        Assert.True(a.LeftBehind <= a.RemainingRoom, "LeftBehind is above RemainingRoom.");
    }
}
