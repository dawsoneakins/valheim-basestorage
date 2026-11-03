namespace BaseStorage.Core.Tests.Restock;

using System.Collections.Generic;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Restock.RestockFixtures;
using static BaseStorage.Core.Tests.Snapshots.Identities.IdentityFixtures;

/// <summary>S-QR-001 AC-3, AC-4, AC-5: one test per restock rule that is pinned on its own (contract 0.2, ADR-GWIN-172).</summary>
public sealed class RestockRuleTests
{
    // GW-U-2490 (Q1)
    [Fact]
    public void Q01_OnlyStacksOfTheSameKeyAreMatched()
    {
        var carried = new List<CarriedStack> { H(SI("Wood", 10, Wl2())) };
        var chests = new List<ContainerSnapshot>
        {
            C(903, 1, 0, 3, S("Wood", 50), SI("Wood", 15, Wl2()), SI("Wood", 40, Worn())),
        };

        RestockPlan plan = RestockPlanner.Plan(carried, Origin(), chests, NoAssign(), Craftable());

        Assert.Equal(new[] { Row(903, 1, 0, 15) }, Shape(plan));
    }

    // GW-U-2492 (Q3, O4)
    [Fact]
    public void Q03_TheStarredBitNeverMakesOrBlocksATarget()
    {
        List<ContainerSnapshot> Chests() => new List<ContainerSnapshot> { C(921, 1, 0, 2, S("Wood", 30)) };

        var plain = new List<CarriedStack> { H(S("Wood", 10)) };
        var starred = new List<CarriedStack> { new CarriedStack(S("Wood", 10), CarriedMarks.Hotbar | CarriedMarks.StarredSlot) };

        RestockPlan a = RestockPlanner.Plan(plain, Origin(), Chests(), NoAssign(), Craftable());
        RestockPlan b = RestockPlanner.Plan(starred, Origin(), Chests(), NoAssign(), Craftable());

        Assert.Equal(Shape(a), Shape(b));
        Assert.Equal(a.RemainingRoom, b.RemainingRoom);
        Assert.Equal(a.CarriedAfter[0].Count, b.CarriedAfter[0].Count);
        Assert.True(a.CarriedAfter[0] == b.CarriedAfter[0]);

        RestockPlan onlyStarred = RestockPlanner.Plan(
            new List<CarriedStack> { Star(S("Wood", 10)) }, Origin(), Chests(), NoAssign(), Craftable());
        Assert.Empty(onlyStarred.Moves);
        Assert.Equal(0, onlyStarred.RemainingRoom);
    }

    // GW-U-2493 (Q3, max stack 1)
    [Fact]
    public void Q03_MaxStackOneItemsAreSkipped()
    {
        var carried = new List<CarriedStack> { H(S("SwordIron", 1, 1)) };
        var chests = new List<ContainerSnapshot> { C(922, 1, 0, 2, S("SwordIron", 1, 1)) };

        RestockPlan plan = RestockPlanner.Plan(carried, Origin(), chests, NoAssign(), Craftable());

        Assert.Empty(plan.Moves);
        Assert.Equal(0, plan.RemainingRoom);
    }

    // GW-U-2494 (Q4)
    [Fact]
    public void Q04_NeverAddsAStackOrAnItemType()
    {
        List<CarriedStack> k = MainCarried();
        RestockPlan a = RestockPlanner.Plan(k, Origin(), MainChests(), AssignR(), Craftable());
        var targets = new HashSet<int> { 0, 1, 5, 7, 8, 9 };

        Assert.Equal(k.Count, a.CarriedAfter.Count);
        for (int t = 0; t < k.Count; t++)
        {
            ItemStackSnapshot before = k[t].Stack;
            ItemStackSnapshot after = a.CarriedAfter[t];
            Assert.Equal(before.PrefabName, after.PrefabName);
            Assert.Equal(before.Quality, after.Quality);
            Assert.Equal(before.MaxStack, after.MaxStack);
            Assert.Equal(before.Identity, after.Identity);
            if (!targets.Contains(t))
            {
                Assert.Same(before, after);
            }
        }

        foreach (RestockMove move in a.Moves)
        {
            Assert.Contains(move.TargetIndex, targets);
        }
    }

    // GW-U-2510 (Q15)
    [Fact]
    public void Q15_ChestsMarksAndPositionsAreInputsOnly()
    {
        // A chest far from every other one, with all flags usable, is still a source when it is passed.
        var far = new List<ContainerSnapshot> { C(971, 500, 500, 2, S("Wood", 50)) };
        RestockPlan farPlan = RestockPlanner.Plan(new List<CarriedStack> { H(S("Wood", 10)) }, Origin(), far, NoAssign(), Craftable());
        Assert.Equal(new[] { Row(971, 0, 0, 40) }, Shape(farPlan));

        // A chest that is not passed is never a source, even when its id is in the assignments.
        var assignments = new Dictionary<ContainerId, CategoryAssignment> { [Id(972)] = CategoryAssignment.Keep };
        RestockPlan notPassed = RestockPlanner.Plan(new List<CarriedStack> { H(S("Wood", 10)) }, Origin(), far, assignments, Craftable());
        Assert.Equal(new[] { Row(971, 0, 0, 40) }, Shape(notPassed));

        // Moving the origin changes only the order in which the passed chests are used.
        var pair = new List<ContainerSnapshot> { C(973, 1, 0, 1, S("Wood", 50)), C(974, 5, 0, 1, S("Wood", 50)) };
        RestockPlan fromHere = RestockPlanner.Plan(new List<CarriedStack> { H(S("Wood", 10)) }, Origin(), pair, NoAssign(), Craftable());
        RestockPlan fromThere = RestockPlanner.Plan(
            new List<CarriedStack> { H(S("Wood", 10)) }, new WorldPosition(6f, 30f, 0f), pair, NoAssign(), Craftable());
        Assert.Equal(new[] { Row(973, 0, 0, 40) }, Shape(fromHere));
        Assert.Equal(new[] { Row(974, 0, 0, 40) }, Shape(fromThere));
        Assert.Equal(fromHere.MovedItems, fromThere.MovedItems);
        Assert.Equal(fromHere.LeftBehind, fromThere.LeftBehind);
    }

    // GW-U-2516 (Q13)
    [Fact]
    public void Q13_RestockFillsAsFarAsUsableChestsAllow()
    {
        RestockPlan a = RestockPlanner.Plan(MainCarried(), Origin(), MainChests(), AssignR(), Craftable());
        RestockPlan b = RestockPlanner.Plan(MainCarried(), Origin(), MainChests(), AssignR(), NotCraftable());

        Assert.True(RestockOracle.IsMaximal(MainCarried(), MainChests(), AssignR(), Craftable(), a.Moves), "plan a is not maximal");
        Assert.True(RestockOracle.IsMaximal(MainCarried(), MainChests(), AssignR(), NotCraftable(), b.Moves), "plan b is not maximal");
    }

    // GW-U-2518 (Q14, saturation)
    [Fact]
    public void Q14_TotalsSaturateAtIntMaxValue()
    {
        var carried = new List<CarriedStack> { H(S("Wood", 1, int.MaxValue)), H(S("Wood", 1, int.MaxValue)) };

        RestockPlan plan = RestockPlanner.Plan(carried, Origin(), new List<ContainerSnapshot>(), NoAssign(), Craftable());

        Assert.Equal(int.MaxValue, plan.RemainingRoom);
        Assert.Equal(0, plan.LeftBehind);
    }
}
