namespace BaseStorage.Core.Tests.Organize;

using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Categories.Defaults;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Organize.Deposit;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.RandomBaseGenerator;

/// <summary>Organize sorts everything it can: no stack stays put for want of a chest of its own while a better one has room.</summary>
public sealed class OrganizeSortEverythingTests
{
    private static PlanInput Base(ContainerSnapshot[] chests, Dictionary<ContainerId, CategoryAssignment> assignments) =>
        new PlanInput(chests, assignments, new Dictionary<ContainerId, string>(), DefaultCategories.Catalog, new Dictionary<string, ItemKind>(), Options(), 100);

    private static CategoryAssignment Cat(string name) => CategoryAssignment.ForCategory(name);

    [Fact]
    public void AStackWhoseCategoryHasNoChestGoesToTheNearestRelatedChest()
    {
        PlanInput input = Base(
            new[]
            {
                C(1, 0f, 0f, 4, S("Wood", 10), S("RawMeat", 5)),
                C(2, 0f, 0f, 4, S("CookedMeat", 5)),
                C(3, 0f, 0f, 4, S("Stone", 5)),
            },
            Assign((1, Cat("Wood")), (2, Cat("Cooked Food")), (3, Cat("Stone & Minerals"))));

        OrganizePlan plan = Run(input, true);

        PlannedMove move = Assert.Single(plan.Moves);
        Assert.Equal(Id(2), move.Destination);
        Assert.Equal("RawMeat", move.PrefabName);
        Assert.Equal("Cooked Food", move.CategoryName);
        Assert.Empty(plan.NoRoom);
        Assert.Equal(1, plan.Spilled);
    }

    [Fact]
    public void AChestThatTakesAGuestKeepsItsCategory()
    {
        PlanInput input = Base(
            new[]
            {
                C(1, 0f, 0f, 3, S("Wood", 10), S("SwordIron", 1, 1), S("ShieldWood", 1, 1)),
                C(2, 0f, 0f, 5, S("TrophyDeer", 1, 20)),
            },
            Assign((1, Cat("Wood"))));
        input = input with { Threshold = 50, Kinds = new Dictionary<string, ItemKind> { ["SwordIron"] = ItemKind.Weapon, ["ShieldWood"] = ItemKind.Shield, ["TrophyDeer"] = ItemKind.Trophy } };

        OrganizePlan plan = Run(input, true);

        ChestAutoAssignment rule = Assert.Single(plan.RulesToSave, r => r.Id == Id(2));
        Assert.Equal(CategoryAssignment.ForCategory("Trophies"), rule.Assignment);
    }

    [Fact]
    public void AGuestNeverLandsOnAStackThatMayLeaveWithThePlannedAmount()
    {
        PlanInput input = Base(
            new[]
            {
                C(1, 0f, 0f, 2, S("Wood", 10), S("RawMeat", 5)),
                C(2, 0f, 0f, 2, S("Stone", 10), S("RawMeat", 7)),
                C(3, 0f, 0f, 4, S("CookedMeat", 5)),
            },
            Assign((1, Cat("Wood")), (2, Cat("Stone & Minerals")), (3, Cat("Cooked Food"))));

        OrganizePlan plan = Run(input, true);

        Assert.All(plan.Moves, move => Assert.Equal(Id(3), move.Destination));
        Assert.Equal(12, plan.Moves.Sum(move => move.Count));
    }

    [Fact]
    public void AMiscChestComesBeforeARelatedOne()
    {
        PlanInput input = Base(
            new[]
            {
                C(1, 0f, 0f, 4, S("Wood", 10), S("RawMeat", 5)),
                C(2, 0f, 0f, 4, S("CookedMeat", 5)),
                C(3, 0f, 0f, 4, S("Coins", 5)),
            },
            Assign((1, Cat("Wood")), (2, Cat("Cooked Food")), (3, Cat("Misc"))));

        Assert.Equal(Id(3), Assert.Single(Run(input, true).Moves).Destination);
    }

    [Fact]
    public void AStackStaysWhenNoChestIsBetterThanTheOneItIsIn()
    {
        PlanInput input = Base(
            new[]
            {
                C(1, 0f, 0f, 4, S("CookedMeat", 5), S("RawMeat", 5)),
                C(2, 0f, 0f, 4, S("Wood", 5)),
            },
            Assign((1, Cat("Cooked Food")), (2, Cat("Wood"))));

        OrganizePlan plan = Run(input, true);

        Assert.Empty(plan.Moves);
        Assert.Equal("RawMeat", Assert.Single(plan.NoRoom).PrefabName);
    }

    [Fact]
    public void AFullBaseStillSaysNoRoom()
    {
        PlanInput input = Base(
            new[] { C(1, 0f, 0f, 1, S("RawMeat", 5)), C(2, 0f, 0f, 1, S("Wood", 5)) },
            Assign((1, Cat("Wood")), (2, Cat("Stone & Minerals"))));

        OrganizePlan plan = Run(input, true);

        Assert.Empty(plan.Moves);
        Assert.Equal(2, plan.NoRoom.Count);
    }

    [Fact]
    public void TheLastEmptyChestGoesToTheCategoryThatNeedsTheMostRoom()
    {
        ItemStackSnapshot[] held = Enumerable.Range(0, 6).Select(_ => S("Stone", 50)).Prepend(S("Coins", 1)).ToArray();
        PlanInput input = Base(
            new[]
            {
                C(1, 0f, 0f, 8, held),
                C(2, 0f, 0f, 4),
            },
            Assign((1, Cat("Wood"))));

        OrganizePlan plan = Run(input, true);

        ChestLayout claimed = Assert.Single(plan.ClaimedOverflow);
        Assert.Equal("Stone & Minerals", claimed.CategoryName);
    }

    [Fact]
    public void WithTheSettingOffAStackWithNoChestStaysPut()
    {
        PlanInput input = Base(
            new[] { C(1, 0f, 0f, 4, S("Wood", 10), S("RawMeat", 5)), C(2, 0f, 0f, 4, S("CookedMeat", 5)) },
            Assign((1, Cat("Wood")), (2, Cat("Cooked Food"))));

        OrganizePlan plan = Run(input, false);

        Assert.Empty(plan.Moves);
        Assert.Equal(0, plan.Spilled);
    }

    [Fact]
    public void PutAwaySendsAStackWithNoChestToTheNearestRelatedChest()
    {
        PlanInput input = Base(
            new[] { C(1, 0f, 0f, 4, S("CookedMeat", 5)), C(2, 0f, 0f, 4, S("Wood", 5)) },
            Assign((1, Cat("Cooked Food")), (2, Cat("Wood"))));
        PlayerInventorySnapshot player = DepositFixtures.Inventory(DepositFixtures.At(0, 1, S("RawMeat", 5)));

        DepositPlan spilled = Plan(player, input, true);
        DepositPlan kept = Plan(player, input, false);

        Assert.Equal(Id(1), Assert.Single(spilled.Plan.Moves).Destination);
        Assert.Empty(spilled.Plan.NoRoom);
        Assert.Empty(kept.Plan.Moves);
        Assert.Single(kept.Plan.NoRoom);
    }

    [Fact]
    public void RepeatedOrganizesSettleAndNeverLeaveMoreStacksMisplaced()
    {
        for (int index = 0; index < CorpusSize; index++)
        {
            PlanInput input = Generate(index).Input;
            long misplaced = long.MaxValue;
            bool settled = false;
            for (int round = 0; round < 5 && !settled; round++)
            {
                OrganizePlan plan = Run(input, true);
                settled = plan.Moves.Count == 0;
                long now = plan.Moves.Sum(move => (long)move.Count) + plan.NoRoom.Sum(entry => (long)entry.Count);
                Assert.True(now <= misplaced, "base " + index + " round " + round + " has more to move than the round before: " + now + " after " + misplaced);
                misplaced = now;
                input = Applied(input, plan);
            }

            Assert.True(settled, "base " + index + " still moves stacks after 5 rounds");
        }
    }

    private static DepositPlan Plan(PlayerInventorySnapshot player, PlanInput input, bool spill) =>
        DepositPlanner.Plan(player, DepositFixtures.NoExclusions(), input.Containers, input.Assignments, input.InUseBy, input.Catalog, input.Kinds, input.Options, input.Threshold, spill);

    /// <summary>The base as it stands after a plan ran, with the categories Organize saved.</summary>
    private static PlanInput Applied(PlanInput input, OrganizePlan plan)
    {
        PlanReplay.ReplayResult replay = PlanReplay.Replay(input.Containers, plan);
        ContainerSnapshot[] chests = input.Containers
            .Select(chest => replay.Final.TryGetValue(chest.Id, out var stacks)
                ? new ContainerSnapshot(chest.Id, chest.Position, chest.Width, chest.Height, stacks, chest.Flags)
                : chest)
            .ToArray();
        var assignments = new Dictionary<ContainerId, CategoryAssignment>(input.Assignments);
        foreach (ChestAutoAssignment rule in plan.RulesToSave)
        {
            assignments[rule.Id] = rule.Assignment!;
        }

        return input with { Containers = chests, Assignments = assignments };
    }
}
