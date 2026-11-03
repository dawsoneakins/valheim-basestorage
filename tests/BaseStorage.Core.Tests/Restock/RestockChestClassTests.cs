namespace BaseStorage.Core.Tests.Restock;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Restock.RestockFixtures;

/// <summary>S-QR-001 AC-4 (contract 7.4, rules Q5 and Q6): which chests may be sources.</summary>
public sealed class RestockChestClassTests
{
    // GW-U-2506 (Q5, Keep gating)
    [Fact]
    public void Q05_KeepChestsAreSourcesOnlyWhenKeepChestsCraftable()
    {
        var keep931 = new Dictionary<ContainerId, CategoryAssignment> { [Id(931)] = CategoryAssignment.Keep };
        var carried = new List<CarriedStack> { H(S("Wood", 10)) };

        RestockPlan keptCraftable = RestockPlanner.Plan(carried, Origin(), new List<ContainerSnapshot> { C(931, 1, 0, 2, S("Wood", 50)) }, keep931, Craftable());
        Assert.Equal(new[] { Row(931, 0, 0, 40) }, Shape(keptCraftable));

        RestockPlan keptNotCraftable = RestockPlanner.Plan(carried, Origin(), new List<ContainerSnapshot> { C(931, 1, 0, 2, S("Wood", 50)) }, keep931, NotCraftable());
        Assert.Empty(keptNotCraftable.Moves);

        var pair = new List<ContainerSnapshot> { C(931, 1, 0, 2, S("Wood", 50)), C(932, 5, 0, 1, S("Wood", 50)) };
        RestockPlan pairNotCraftable = RestockPlanner.Plan(carried, Origin(), pair, keep931, NotCraftable());
        Assert.Equal(new[] { Row(932, 0, 0, 40) }, Shape(pairNotCraftable));

        RestockPlan pairCraftable = RestockPlanner.Plan(carried, Origin(), pair, keep931, Craftable());
        Assert.Equal(new[] { Row(931, 0, 0, 40) }, Shape(pairCraftable));
    }

    // GW-U-2507 (Q5, other eligibility and the content checks)
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Q05_OnlyEligibleChestsWithValidContentsAreSources(int caseIndex)
    {
        (uint id, ContainerSnapshot chest, EligibilityOptions options, int moved) = Case(caseIndex);
        var carried = new List<CarriedStack> { H(S("Wood", 10)) };

        RestockPlan plan = RestockPlanner.Plan(carried, Origin(), new List<ContainerSnapshot> { chest }, NoAssign(), options);

        if (moved == 0)
        {
            Assert.Empty(plan.Moves);
        }
        else
        {
            Assert.Equal(new[] { Row(id, 0, 0, moved) }, Shape(plan));
        }

        Assert.Equal(0, plan.LeftInUse);
        Assert.Equal(0, plan.LeftDifferentIdentity);
        Assert.Equal(40 - moved, plan.RemainingRoom);
    }

    private static (uint Id, ContainerSnapshot Chest, EligibilityOptions Options, int Moved) Case(int caseIndex) => caseIndex switch
    {
        0 => (931, C(931, 1, 0, 2, S("Wood", 50)), Craftable(), 40),
        1 => (933, CF(933, 1, 0, 2, ContainerFlags.Tombstone, S("Wood", 50)), Craftable(), 0),
        2 => (934, CF(934, 1, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.Vehicle, S("Wood", 50)), Craftable(), 0),
        3 => (934, CF(934, 1, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.Vehicle, S("Wood", 50)), new EligibilityOptions(false, true), 40),
        4 => (935, CF(935, 1, 0, 2, ContainerFlags.None, S("Wood", 50)), Craftable(), 0),
        5 => (936, CF(936, 1, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.AccessDenied, S("Wood", 50)), Craftable(), 0),
        6 => (939, new ContainerSnapshot(Id(939), new WorldPosition(1f, 30f, 0f), 1, 1, new[] { S("Wood", 50), S("Wood", 50) }, ContainerFlags.PlayerBuilt), Craftable(), 0),
        7 => (940, C(940, 1, 0, 2, S("Wood", 60)), Craftable(), 0),
        _ => throw new ArgumentOutOfRangeException(nameof(caseIndex)),
    };
}
