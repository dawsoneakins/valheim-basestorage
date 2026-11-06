namespace BaseStorage.Core.Tests.Organize.Deposit;

using System;
using System.Linq;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.Deposit.DepositFixtures;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>Put away selected: only the picked stacks move, and picking overrides every rule except worn or broken stacks.</summary>
public sealed class DepositSelectionTests
{
    private static FavouriteSlots Slots(params (int X, int Y)[] slots) =>
        new FavouriteSlots(slots.Select(s => new InventorySlot(s.X, s.Y)));

    private static DepositPlan Plan(PlayerInventorySnapshot player, DepositExclusions exclusions) =>
        DepositPlanner.Plan(
            player,
            exclusions,
            new[] { C(1, 0, 0, 16) },
            Assign((1, CategoryAssignment.ForCategory("Wood"))),
            InUse(),
            Catalog(),
            DepositKinds(),
            Options(),
            DefaultThreshold);

    [Fact]
    public void OnlyTheSelectedStackMoves()
    {
        var player = Inventory(At(0, 1, S("Wood", 10)), At(1, 1, S("FineWood", 10)));

        var plan = Plan(player, NoExclusions().OnlySelected(Slots((1, 1))));

        var move = Assert.Single(plan.Plan.Moves);
        Assert.Equal("FineWood", move.PrefabName);
        Assert.Equal(DepositExclusion.NotSelected, plan.FindExcluded(0)!.Reasons);
    }

    [Fact]
    public void PickingOverridesTheHotbarTheNeverListAndFavourites()
    {
        var player = Inventory(At(0, 0, S("Wood", 10)), At(1, 1, S("Hammer", 1, maxStack: 1)), At(5, 1, S("FineWood", 10)));
        var exclusions = DefaultExclusions(new InventorySlot(5, 1)).OnlySelected(Slots((0, 0), (1, 1), (5, 1)));

        var plan = Plan(player, exclusions);

        Assert.Empty(plan.Excluded);
    }

    [Fact]
    public void EquippedStacksNeverMoveEvenWhenPicked()
    {
        var player = Inventory(At(0, 1, S("SwordIron", 1, maxStack: 1), InventoryItemTraits.Equipped));

        var plan = Plan(player, NoExclusions().OnlySelected(Slots((0, 1))));

        Assert.Empty(plan.Plan.Moves);
        Assert.Equal(DepositExclusion.Equipped, plan.FindExcluded(0)!.Reasons);
    }

    [Fact]
    public void AStackAboveItsMaximumNeverMovesEvenWhenPicked()
    {
        var player = Inventory(At(0, 1, S("Wood", 60)));

        var plan = Plan(player, NoExclusions().OnlySelected(Slots((0, 1))));

        Assert.Empty(plan.Plan.Moves);
        Assert.Equal(DepositExclusion.InvalidStack, plan.FindExcluded(0)!.Reasons);
    }

    [Fact]
    public void PickingOneOfTwoIdenticalStacksMovesOnlyThatOne()
    {
        var player = Inventory(At(0, 1, S("Wood", 10)), At(1, 1, S("Wood", 10)));

        var plan = Plan(player, NoExclusions().OnlySelected(Slots((1, 1))));

        var move = Assert.Single(plan.Plan.Moves);
        Assert.Equal(1, move.SourceStackIndex);
        Assert.Equal(DepositExclusion.NotSelected, plan.FindExcluded(0)!.Reasons);
    }

    [Fact]
    public void ANormalPutAwayStillKeepsAStackLikeAnExcludedOne()
    {
        var player = Inventory(At(0, 0, S("Wood", 10)), At(1, 1, S("Wood", 10)));

        var plan = Plan(player, NoExclusions());

        Assert.Empty(plan.Plan.Moves);
        Assert.Equal(DepositExclusion.SameItemAsExcluded, plan.FindExcluded(1)!.Reasons);
    }

    [Fact]
    public void ANullSelectionIsRefused() =>
        Assert.Throws<ArgumentNullException>(() => NoExclusions().OnlySelected(null!));
}
