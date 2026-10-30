namespace BaseStorage.Core.Tests.Availability;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Availability.PlanFixtures;

/// <summary>AC-1 to AC-4 and AC-7 (ADR-GWIN-028 steps 2, 3 and 6): the order of the takes, and the inventory-only and whole-stack cases.</summary>
public sealed class ConsumptionPlanOrderTests
{
    /// <summary>Theory amounts: Wood 15 to 113, the range that needs at least one chest and can still be paid.</summary>
    public static IEnumerable<object[]> WoodFrom15To113() => Enumerable.Range(15, 99).Select(amount => new object[] { amount });

    // GW-U-440 (AC-1): wood that the inventory covers needs no chest, and every take is from the player inventory.
    [Fact]
    public void InventoryCoversItWithoutAChest()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 3));

        Assert.Equal(new[] { Take(snapshot, 1, 2, 3) }, plan.Takes);
        Assert.Empty(plan.ExpectedContainers);
        Assert.Equal(Id(1), plan.PlayerInventoryId);
        Assert.All(plan.Takes, take => Assert.Equal(plan.PlayerInventoryId, take.ContainerId));
    }

    // GW-U-440 (AC-1): a stone requirement is paid from the inventory's only stone stack, with no expected containers.
    [Fact]
    public void InventoryStoneIsPaidWithoutAChest()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Stone", 3));

        Assert.Equal(new[] { Take(snapshot, 1, 1, 3) }, plan.Takes);
        Assert.Empty(plan.ExpectedContainers);
    }

    // GW-U-441 (AC-2): the inventory is drained first, and the chests take only the remainder, nearest chest and smallest stack first.
    [Fact]
    public void InventoryTakesComeFirstAndChestsTakeOnlyTheRemainder()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 20));

        Assert.Equal(
            new[] { Take(snapshot, 1, 2, 4), Take(snapshot, 1, 0, 10), Take(snapshot, 5, 1, 2), Take(snapshot, 5, 0, 4) },
            plan.Takes);
        Assert.Equal(14, plan.Takes.Where(take => take.ContainerId == plan.PlayerInventoryId).Sum(take => take.Count));
        Assert.Equal(6, plan.Takes.Where(take => take.ContainerId != plan.PlayerInventoryId).Sum(take => take.Count));
    }

    // GW-U-441 (AC-2): a requirement that the inventory mostly covers takes one item from the nearest chest.
    [Fact]
    public void ChestTakesOnlyTheLastItemsNeeded()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 15));

        Assert.Equal(new[] { Take(snapshot, 1, 2, 4), Take(snapshot, 1, 0, 10), Take(snapshot, 5, 1, 1) }, plan.Takes);
    }

    // GW-U-442 (AC-3): chests are visited by ascending horizontal distance, ties by id, and Y is ignored.
    [Fact]
    public void ChestsAreVisitedByHorizontalDistanceThenIdIgnoringY()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 59));

        Assert.Equal(
            new[]
            {
                Take(snapshot, 1, 2, 4),
                Take(snapshot, 1, 0, 10),
                Take(snapshot, 5, 1, 2),
                Take(snapshot, 5, 0, 8),
                Take(snapshot, 5, 2, 8),
                Take(snapshot, 7, 1, 6),
                Take(snapshot, 7, 0, 20),
                Take(snapshot, 2, 0, 1),
            },
            plan.Takes);
    }

    // GW-U-442 (AC-3): with all the wood requested, container 2 is taken in full, then container 9 last.
    [Fact]
    public void AllTheWoodVisitsEveryChestInDistanceOrder()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 113));

        Assert.Equal(
            new[]
            {
                Take(snapshot, 1, 2, 4),
                Take(snapshot, 1, 0, 10),
                Take(snapshot, 5, 1, 2),
                Take(snapshot, 5, 0, 8),
                Take(snapshot, 5, 2, 8),
                Take(snapshot, 7, 1, 6),
                Take(snapshot, 7, 0, 20),
                Take(snapshot, 2, 0, 50),
                Take(snapshot, 9, 0, 5),
            },
            plan.Takes);
    }

    // GW-U-443 (AC-3): a farther chest is not touched while a nearer one still holds the item.
    [Fact]
    public void NoFartherChestIsTouchedWhileANearerOneHoldsTheItem()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan drainedNearest = Planned(snapshot, new CraftRequirement("Wood", 32));
        ConsumptionPlan oneMore = Planned(snapshot, new CraftRequirement("Wood", 33));
        ConsumptionPlan intoSecond = Planned(snapshot, new CraftRequirement("Wood", 58));

        Assert.Equal(
            new[] { Take(snapshot, 1, 2, 4), Take(snapshot, 1, 0, 10), Take(snapshot, 5, 1, 2), Take(snapshot, 5, 0, 8), Take(snapshot, 5, 2, 8) },
            drainedNearest.Takes);
        Assert.Equal(new uint[] { 5 }, Ids(drainedNearest.ExpectedContainers));
        Assert.Equal(
            new[]
            {
                Take(snapshot, 1, 2, 4), Take(snapshot, 1, 0, 10), Take(snapshot, 5, 1, 2), Take(snapshot, 5, 0, 8),
                Take(snapshot, 5, 2, 8), Take(snapshot, 7, 1, 1),
            },
            oneMore.Takes);
        Assert.Equal(new uint[] { 5, 7 }, Ids(oneMore.ExpectedContainers));
        Assert.Equal(new uint[] { 5, 7 }, Ids(intoSecond.ExpectedContainers));
        Assert.DoesNotContain(intoSecond.Takes, take => take.ContainerId.Id == 2 || take.ContainerId.Id == 9);
        Assert.Equal(26, intoSecond.Takes.Where(take => take.ContainerId.Id == 7).Sum(take => take.Count));
    }

    // GW-U-443 (AC-3, theory): for every chest take from chest X, each earlier chest in the order 5, 7, 2, 9 is drained of wood first.
    [Theory]
    [MemberData(nameof(WoodFrom15To113))]
    public void EveryEarlierChestIsDrainedBeforeALaterOneIsTouched(int amount)
    {
        BaseSnapshot snapshot = WorldP();
        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", amount));
        uint[] order = { 5u, 7u, 2u, 9u };
        int[] woodTotals = { 18, 26, 50, 5 };

        for (int later = 0; later < order.Length; later++)
        {
            if (!plan.Takes.Any(take => take.ContainerId.Id == order[later]))
            {
                continue;
            }

            for (int earlier = 0; earlier < later; earlier++)
            {
                int taken = plan.Takes.Where(take => take.ContainerId.Id == order[earlier]).Sum(take => take.Count);
                Assert.True(
                    taken == woodTotals[earlier],
                    $"Wood {amount}: chest {order[earlier]} must be drained ({taken} of {woodTotals[earlier]}) before chest {order[later]} is touched.");
            }
        }
    }

    // GW-U-444 (AC-4): within a chest, the smallest stacks go first, with ties broken by slot; the inventory is ordered the same way.
    [Fact]
    public void SmallestStacksGoFirstWithTiesBySlot()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 32));

        Assert.Equal(
            new[] { Take(snapshot, 1, 2, 4), Take(snapshot, 1, 0, 10), Take(snapshot, 5, 1, 2), Take(snapshot, 5, 0, 8), Take(snapshot, 5, 2, 8) },
            plan.Takes);
    }

    // GW-U-444 (AC-4): slot 2 of chest 5 is untouched while slot 0 is not exhausted.
    [Fact]
    public void EqualSmallStacksAreTakenInSlotOrderAndTheLastOneIsPartTaken()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 21));

        Assert.Equal(
            new[] { Take(snapshot, 1, 2, 4), Take(snapshot, 1, 0, 10), Take(snapshot, 5, 1, 2), Take(snapshot, 5, 0, 5) },
            plan.Takes);
    }

    // GW-U-444 (AC-4): on chest 7, the 6-stack goes before the 20-stack.
    [Fact]
    public void OnChestSevenTheSixStackGoesBeforeTheTwentyStack()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 39));

        Assert.Equal(
            new[]
            {
                Take(snapshot, 1, 2, 4), Take(snapshot, 1, 0, 10), Take(snapshot, 5, 1, 2), Take(snapshot, 5, 0, 8),
                Take(snapshot, 5, 2, 8), Take(snapshot, 7, 1, 6), Take(snapshot, 7, 0, 1),
            },
            plan.Takes);
    }

    // GW-U-448 (AC-7): an amount equal to the inventory total on 9.7 keepOn is one take from one stack, with no chest.
    [Fact]
    public void AmountEqualToTheInventoryWoodIsOneTakeWithNoChest()
    {
        World world = Create();
        BaseSnapshot snapshot = Snapshot(world, true);

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 12));

        Assert.Equal(new[] { Take(snapshot, 1, 0, 12) }, plan.Takes);
        Assert.Empty(plan.ExpectedContainers);
    }

    // GW-U-448 (AC-7): an amount equal to the single resin stack on 9.7 keepOn is one take, with no chest.
    [Fact]
    public void AmountEqualToTheSingleResinStackIsOneTake()
    {
        World world = Create();
        BaseSnapshot snapshot = Snapshot(world, true);

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Resin", 5));

        Assert.Equal(new[] { Take(snapshot, 1, 1, 5) }, plan.Takes);
        Assert.Empty(plan.ExpectedContainers);
    }

    // GW-U-448 (AC-7): on world P, 14 wood is the whole inventory, so no chest is named.
    [Fact]
    public void AmountEqualToTheWholeInventoryNamesNoChest()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 14));

        Assert.Equal(new[] { Take(snapshot, 1, 2, 4), Take(snapshot, 1, 0, 10) }, plan.Takes);
        Assert.Empty(plan.ExpectedContainers);
    }

    // GW-U-448 (AC-7): 4 wood is exactly the smallest inventory stack.
    [Fact]
    public void AmountEqualToTheSmallestInventoryStackIsOneTake()
    {
        BaseSnapshot snapshot = WorldP();

        ConsumptionPlan plan = Planned(snapshot, new CraftRequirement("Wood", 4));

        Assert.Equal(new[] { Take(snapshot, 1, 2, 4) }, plan.Takes);
        Assert.Empty(plan.ExpectedContainers);
    }
}
