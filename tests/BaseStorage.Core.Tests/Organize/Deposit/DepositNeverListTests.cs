namespace BaseStorage.Core.Tests.Organize.Deposit;

using System;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Settings;
using static BaseStorage.Core.Tests.Organize.Deposit.DepositFixtures;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>
/// AC-4.9: the DepositNeverItems default covers meads, arrows and bolts, hammer, hoe, cultivator, fishing rod and bait
/// (deposit contract 6.5), and the exclusions read the list from the settings once.
/// </summary>
public sealed class DepositNeverListTests
{
    /// <summary>The default list, copied from spec section 7 as a literal.</summary>
    private static readonly string[] DefaultEntries = { "@mead", "@ammo", "Hammer", "Hoe", "Cultivator", "FishingRod", "@bait" };

    // GW-U-1057 (AC-4.9): the parsed default has exactly the 7 entries, in order.
    [Fact]
    public void TheDefaultListHasTheSevenEntries()
    {
        Assert.Equal(DefaultEntries, SettingsCatalog.DepositNeverItemsDefault);
        Assert.Equal(DefaultEntries, DefaultExclusions().NeverItems);
    }

    // GW-U-1058 (AC-4.9): every default entry keeps its representative stack back, and an empty list lets the same stack go.
    [Theory]
    [InlineData("@mead", "MeadHealthMinor", ItemKind.Other, InventoryItemTraits.Mead)]
    [InlineData("@ammo", "ArrowWood", ItemKind.Ammo, InventoryItemTraits.None)]
    [InlineData("@ammo", "BoltIron", ItemKind.Other, InventoryItemTraits.Ammo)]
    [InlineData("Hammer", "Hammer", ItemKind.Tool, InventoryItemTraits.None)]
    [InlineData("Hoe", "Hoe", ItemKind.Tool, InventoryItemTraits.None)]
    [InlineData("Cultivator", "Cultivator", ItemKind.Tool, InventoryItemTraits.None)]
    [InlineData("FishingRod", "FishingRod", ItemKind.Tool, InventoryItemTraits.Equipped)]
    [InlineData("@bait", "FishingBait", ItemKind.Other, InventoryItemTraits.Bait)]
    public void EachDefaultEntryCoversItsItem(string entry, string prefab, ItemKind kind, InventoryItemTraits traits)
    {
        Assert.Contains(entry, DefaultEntries);
        PlayerInventoryStack stack = At(3, 2, S(prefab, 1, 1), traits);
        DepositExclusion expected = DepositExclusion.NeverList | (traits == InventoryItemTraits.Equipped ? DepositExclusion.Equipped : DepositExclusion.None);

        Assert.True(DefaultExclusions().IsNeverItem(stack, kind), entry + " must cover " + prefab);
        Assert.Equal(expected, DepositPlanner.ExclusionOf(stack, DefaultExclusions(), kind));
        Assert.True(NeverList(entry).IsNeverItem(stack, kind), entry + " alone must cover " + prefab);
        Assert.Equal(expected & ~DepositExclusion.NeverList, DepositPlanner.ExclusionOf(stack, NoExclusions(), kind));

        // In a plan over base D the stack is excluded with the default list and in no move or no-room entry.
        DepositPlan plan = Run(Inventory(stack), DefaultExclusions(), D());
        Assert.Equal(new[] { (0, expected) }, ExcludedOf(plan));
        Assert.Empty(plan.Plan.Moves);
        Assert.Empty(plan.Plan.NoRoom);
    }

    // GW-U-1059 (AC-4.9): FromSettings reads DepositNeverItems once and falls back to the default; validation names its parameters.
    [Fact]
    public void FromSettingsReadsTheListOnceAndValidates()
    {
        FakeSettingsSource custom = new FakeSettingsSource().WithText(SettingKeys.DepositNeverItems, " Wood ,@Mead,,Hoe ");
        DepositExclusions exclusions = DepositExclusions.FromSettings(new BaseStorageSettings(custom), FavouriteSlots.Empty);
        Assert.Equal(new[] { "Wood", "@Mead", "Hoe" }, exclusions.NeverItems);
        Assert.Equal(1, custom.TextCalls);
        Assert.True(exclusions.IsNeverItem(At(1, 1, S("Wood", 5)), ItemKind.Other));
        Assert.True(exclusions.IsNeverItem(At(1, 1, S("MeadHealthMinor", 5), InventoryItemTraits.Mead), ItemKind.Other));
        Assert.False(exclusions.IsNeverItem(At(1, 1, S("Hammer", 1, 1)), ItemKind.Tool));
        Assert.Same(FavouriteSlots.Empty, exclusions.Favourites);

        FakeSettingsSource absent = new FakeSettingsSource();
        Assert.Equal(DefaultEntries, DepositExclusions.FromSettings(new BaseStorageSettings(absent), FavouriteSlots.Empty).NeverItems);

        Assert.Equal("settings", Assert.Throws<ArgumentNullException>(() => DepositExclusions.FromSettings(null!, FavouriteSlots.Empty)).ParamName);
        Assert.Equal("favourites", Assert.Throws<ArgumentNullException>(() => DepositExclusions.FromSettings(new BaseStorageSettings(absent), null!)).ParamName);
        Assert.Equal("favourites", Assert.Throws<ArgumentNullException>(() => new DepositExclusions(null!, Array.Empty<string>())).ParamName);
        Assert.Equal("neverItems", Assert.Throws<ArgumentNullException>(() => new DepositExclusions(FavouriteSlots.Empty, null!)).ParamName);
        Assert.Equal("neverItems", Assert.Throws<ArgumentException>(() => new DepositExclusions(FavouriteSlots.Empty, new string?[] { "Wood", null }!)).ParamName);
        Assert.Equal("stack", Assert.Throws<ArgumentNullException>(() => NoExclusions().IsNeverItem(null!, ItemKind.Other)).ParamName);
        Assert.Equal("stack", Assert.Throws<ArgumentNullException>(() => DepositPlanner.ExclusionOf(null!, NoExclusions(), ItemKind.Other)).ParamName);
        Assert.Equal("exclusions", Assert.Throws<ArgumentNullException>(() => DepositPlanner.ExclusionOf(At(0, 1, S("Wood", 1)), null!, ItemKind.Other)).ParamName);
        Assert.Equal("stack", Assert.Throws<ArgumentNullException>(() => new PlayerInventoryStack(new InventorySlot(0, 0), null!, InventoryItemTraits.None)).ParamName);
    }
}
