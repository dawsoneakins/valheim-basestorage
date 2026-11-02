namespace BaseStorage.Core.Tests.Organize.Deposit;

using System;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Persistence;
using static BaseStorage.Core.Tests.Organize.Deposit.DepositFixtures;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>
/// AC-4.9: the four exclusion classes (hotbar, equipped, favourite, never list) individually and combined, the same-item rule and the
/// invalid-stack rule, both through <c>ExclusionOf</c> and through a whole plan (deposit contract 5.1, 6.4).
/// </summary>
public sealed class DepositExclusionTests
{
    // GW-U-1050 (AC-4.9): every slot of row 0 is hotbar, whatever the column; row 1 and below are not.
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(7)]
    public void TheWholeTopRowIsTheHotbar(int x)
    {
        Assert.Equal(0, DepositPlanner.HotbarRow);
        Assert.Equal(DepositExclusion.Hotbar, DepositPlanner.ExclusionOf(At(x, 0, S("Wood", 10)), NoExclusions(), ItemKind.Other));
        Assert.Equal(DepositExclusion.None, DepositPlanner.ExclusionOf(At(x, 1, S("Wood", 10)), NoExclusions(), ItemKind.Other));
        Assert.Equal(DepositExclusion.None, DepositPlanner.ExclusionOf(At(x, 3, S("Wood", 10)), NoExclusions(), ItemKind.Other));
    }

    // GW-U-1051 (AC-4.9): the equipped trait excludes; the other traits alone do not.
    [Fact]
    public void EquippedIsAnExclusionAndOtherTraitsAloneAreNot()
    {
        Assert.Equal(DepositExclusion.Equipped, DepositPlanner.ExclusionOf(At(2, 1, S("HelmetBronze", 1, 1), InventoryItemTraits.Equipped), NoExclusions(), ItemKind.Armour));
        Assert.True(At(2, 1, S("HelmetBronze", 1, 1), InventoryItemTraits.Equipped).IsEquipped);
        Assert.Equal(DepositExclusion.None, DepositPlanner.ExclusionOf(At(2, 1, S("CookedMeat", 5)), NoExclusions(), ItemKind.Food));
        Assert.Equal(DepositExclusion.None, DepositPlanner.ExclusionOf(At(2, 1, S("ArrowWood", 20), InventoryItemTraits.Ammo | InventoryItemTraits.Bait | InventoryItemTraits.Mead), NoExclusions(), ItemKind.Ammo));
    }

    // GW-U-1052 (AC-4.9): a favourite slot excludes the stack in it; a favourite elsewhere does not.
    [Fact]
    public void AFavouriteSlotExcludesOnlyItsOwnStack()
    {
        DepositExclusions exclusions = new DepositExclusions(new FavouriteSlots(new[] { new InventorySlot(5, 1) }), Array.Empty<string>());
        Assert.Equal(DepositExclusion.Favourite, DepositPlanner.ExclusionOf(At(5, 1, S("Resin", 12)), exclusions, ItemKind.Other));
        Assert.Equal(DepositExclusion.None, DepositPlanner.ExclusionOf(At(5, 2, S("Resin", 12)), exclusions, ItemKind.Other));
        Assert.Equal(DepositExclusion.None, DepositPlanner.ExclusionOf(At(4, 1, S("Resin", 12)), exclusions, ItemKind.Other));
    }

    // GW-U-1053 (AC-4.9): never-list matching: prefab names ordinally, groups by trait or kind, tokens ignoring case, unknown tokens never.
    [Theory]
    [InlineData("Hammer", "Hammer", ItemKind.Tool, InventoryItemTraits.None, true)]
    [InlineData("hammer", "Hammer", ItemKind.Tool, InventoryItemTraits.None, false)]
    [InlineData("Hammer", "Hoe", ItemKind.Tool, InventoryItemTraits.None, false)]
    [InlineData("@food", "SerpentStew", ItemKind.Food, InventoryItemTraits.None, false)]
    [InlineData("@Food", "CookedMeat", ItemKind.Food, InventoryItemTraits.None, false)]
    [InlineData("@mead", "MeadHealthMinor", ItemKind.Other, InventoryItemTraits.Mead, true)]
    [InlineData("@mead", "MeadHealthMinor", ItemKind.Other, InventoryItemTraits.None, false)]
    [InlineData("@ammo", "ArrowWood", ItemKind.Ammo, InventoryItemTraits.None, true)]
    [InlineData("@ammo", "BoltIron", ItemKind.Other, InventoryItemTraits.Ammo, true)]
    [InlineData("@AMMO", "ArrowWood", ItemKind.Ammo, InventoryItemTraits.None, true)]
    [InlineData("@bait", "FishingBait", ItemKind.Other, InventoryItemTraits.Bait, true)]
    [InlineData("@bait", "FishingBait", ItemKind.Other, InventoryItemTraits.None, false)]
    [InlineData("@fish", "FishingBait", ItemKind.Other, InventoryItemTraits.Bait | InventoryItemTraits.Mead | InventoryItemTraits.Ammo, false)]
    [InlineData("@", "Wood", ItemKind.Other, InventoryItemTraits.None, false)]
    [InlineData("@mead", "Wood", ItemKind.Other, InventoryItemTraits.Equipped, false)]
    public void NeverListEntriesMatchByPrefabOrGroup(string entry, string prefab, ItemKind kind, InventoryItemTraits traits, bool expected)
    {
        DepositExclusions exclusions = NeverList(entry);
        PlayerInventoryStack stack = At(3, 2, S(prefab, 1, 1), traits);
        Assert.Equal(expected, exclusions.IsNeverItem(stack, kind));
        DepositExclusion want = expected ? DepositExclusion.NeverList : DepositExclusion.None;
        if (traits == InventoryItemTraits.Equipped)
        {
            want |= DepositExclusion.Equipped;
        }

        Assert.Equal(want, DepositPlanner.ExclusionOf(stack, exclusions, kind));
        Assert.Equal(new[] { entry }, exclusions.NeverItems);
    }

    // GW-U-1054 (AC-4.9): all four classes on one stack give all four flags; a plain stack gives None; an over-max stack is InvalidStack.
    [Fact]
    public void ReasonsCombineAndAnOverMaxStackIsInvalid()
    {
        DepositExclusions exclusions = DefaultExclusions(new InventorySlot(2, 0));
        PlayerInventoryStack all = At(2, 0, S("Hammer", 1, 1), InventoryItemTraits.Equipped);
        Assert.Equal(
            DepositExclusion.Hotbar | DepositExclusion.Equipped | DepositExclusion.Favourite | DepositExclusion.NeverList,
            DepositPlanner.ExclusionOf(all, exclusions, ItemKind.Tool));

        Assert.Equal(DepositExclusion.None, DepositPlanner.ExclusionOf(At(2, 1, S("Wood", 50)), exclusions, ItemKind.Other));
        Assert.Equal(DepositExclusion.InvalidStack, DepositPlanner.ExclusionOf(At(2, 1, S("Wood", 60)), exclusions, ItemKind.Other));
        Assert.Equal(DepositExclusion.Hotbar | DepositExclusion.InvalidStack, DepositPlanner.ExclusionOf(At(2, 0, S("Wood", 60)), NoExclusions(), ItemKind.Other));
    }

    // GW-U-1055 (AC-4.9, 6.4): each class alone, in a plan: the excluded stack is in no move and no no-room entry and keeps its count,
    // while its depositable twin of another item moves.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EachClassAloneKeepsItsStackInThePlan(int which)
    {
        DepositExclusion expected = which switch
        {
            0 => DepositExclusion.Hotbar,
            1 => DepositExclusion.Equipped,
            2 => DepositExclusion.Favourite,
            _ => DepositExclusion.NeverList,
        };
        PlayerInventoryStack kept = which switch
        {
            0 => At(0, 0, S("Stone", 30)),
            1 => At(0, 1, S("Stone", 30), InventoryItemTraits.Equipped),
            2 => At(0, 1, S("Stone", 30)),
            _ => At(0, 1, S("Stone", 30)),
        };
        DepositExclusions exclusions = which switch
        {
            2 => new DepositExclusions(new FavouriteSlots(new[] { new InventorySlot(0, 1) }), Array.Empty<string>()),
            3 => NeverList("Stone"),
            _ => NoExclusions(),
        };

        DepositPlan plan = Run(Inventory(kept, At(1, 1, S("Wood", 50))), exclusions, D());

        Assert.Equal(new[] { (0, expected) }, ExcludedOf(plan));
        Assert.Equal(1, plan.DepositableStacks);
        Assert.Equal(expected, plan.FindExcluded(0)!.Reasons);
        Assert.Same(kept, plan.FindExcluded(0)!.Source);
        Assert.Null(plan.FindExcluded(1));
        Assert.All(plan.Plan.Moves, move => Assert.Equal(1, move.SourceStackIndex));
        Assert.Empty(plan.Plan.NoRoom);
        Assert.Equal(50, plan.Plan.Moves.Sum(move => move.Count));
        Assert.Equal("Stone x30 q1 max50", PlanAssert.Join(plan.Plan.Layouts[0].Stacks));
    }

    // GW-U-1056 (AC-4.9, D5, 6.4): a depositable stack that is the same item as an excluded one is kept back; a different quality is not.
    [Fact]
    public void AStackOfTheSameItemAsAnExcludedStackIsKeptBack()
    {
        DepositPlan same = Run(
            Inventory(At(2, 0, S("Wood", 10)), At(0, 1, S("Wood", 50)), At(1, 1, S("Stone", 30)), At(2, 1, S("Wood", 5, 50, 2))),
            NoExclusions(),
            D());

        Assert.Equal(new[] { (0, DepositExclusion.Hotbar), (1, DepositExclusion.SameItemAsExcluded) }, ExcludedOf(same));
        Assert.Equal(2, same.DepositableStacks);
        Assert.DoesNotContain(same.Plan.Moves, move => move.PrefabName == "Wood" && move.Quality == 1);
        Assert.Contains(same.Plan.Moves, move => move.SourceStackIndex == 2 && move.PrefabName == "Stone" && move.Count == 30);
        Assert.Contains(same.Plan.Moves, move => move.SourceStackIndex == 3 && move.PrefabName == "Wood" && move.Quality == 2 && move.Count == 5);
        Assert.Equal("Wood x10 q1 max50; Wood x50 q1 max50", PlanAssert.Join(same.Plan.Layouts[0].Stacks));

        // The same-item rule follows the other rules only: two depositable Wood stacks are both deposited.
        DepositPlan twins = Run(Inventory(At(0, 1, S("Wood", 10)), At(1, 1, S("Wood", 10))), NoExclusions(), D());
        Assert.Empty(twins.Excluded);
        Assert.Equal(20, twins.Plan.Moves.Sum(move => move.Count));
    }
}
