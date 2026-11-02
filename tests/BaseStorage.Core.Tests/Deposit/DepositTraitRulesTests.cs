namespace BaseStorage.Core.Tests.Deposit;

using System;
using System.Globalization;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Plugin.Deposit;

/// <summary>
/// GW-U-1920..1922 (AC-4, NFR exclusions; deposit-button contract 3.1 and 4.3): the game-free Deposit trait rules. Each row is replayed
/// under en-US, de-DE, tr-TR, ar-SA and the invariant culture, so a culture-sensitive comparison cannot pass. The file is linked into
/// Core.Tests (ADR-GWIN-142), so it uses no game type. Red until the implement phase adds <c>DepositTraitRules</c>.
/// </summary>
public sealed class DepositTraitRulesTests
{
    // GW-U-1920 (row 1): plain stacks, the equipped flag, food and a non-food consumable.
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    [InlineData("ar-SA")]
    [InlineData("")]
    public void BasicTraitsFollowTheFlagsAndTheFoodDecision(string culture)
    {
        InCulture(culture, () =>
        {
            Assert.Equal(InventoryItemTraits.None, DepositTraitRules.Of(false, false, false, false, "Wood"));
            Assert.Equal(InventoryItemTraits.Equipped, DepositTraitRules.Of(true, false, false, false, "SwordIron"));
            Assert.Equal(InventoryItemTraits.None, DepositTraitRules.Of(false, true, false, true, "CookedMeat"));
            Assert.Equal(InventoryItemTraits.Mead, DepositTraitRules.Of(false, true, false, false, "MeadHealthMinor"));
            Assert.Equal(InventoryItemTraits.None, DepositTraitRules.Of(false, true, false, false, "Pukeberries"));
        });
    }

    // GW-U-1921 (row 2): ammo, bait by the prefix match, the combined flags, and the ordinal match of the bait prefix.
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    [InlineData("ar-SA")]
    [InlineData("")]
    public void AmmoAndBaitTraitsFollowThePrefixAndTheFlags(string culture)
    {
        InCulture(culture, () =>
        {
            Assert.Equal(InventoryItemTraits.Ammo, DepositTraitRules.Of(false, false, true, false, "ArrowWood"));
            Assert.Equal(InventoryItemTraits.Bait, DepositTraitRules.Of(false, false, false, false, "FishingBaitForest"));
            Assert.Equal(InventoryItemTraits.Ammo | InventoryItemTraits.Bait, DepositTraitRules.Of(false, false, true, false, "FishingBait"));
            Assert.Equal(InventoryItemTraits.None, DepositTraitRules.Of(false, false, false, false, "fishingbait"));
            Assert.Equal(InventoryItemTraits.Equipped | InventoryItemTraits.Ammo, DepositTraitRules.Of(true, false, true, false, "ArrowFire"));
        });
    }

    // GW-U-1922 (row 3): a null prefab name is refused by name and nothing else throws.
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    [InlineData("ar-SA")]
    [InlineData("")]
    public void ANullPrefabNameThrowsForThatParameter(string culture)
    {
        InCulture(culture, () =>
        {
            ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => DepositTraitRules.Of(false, false, false, false, null!));
            Assert.Equal("prefabName", error.ParamName);
        });
    }

    // Runs an assertion under one culture, and restores the thread's culture afterwards.
    private static void InCulture(string culture, Action assertion)
    {
        CultureInfo savedCulture = CultureInfo.CurrentCulture;
        CultureInfo savedUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo chosen = culture.Length == 0 ? CultureInfo.InvariantCulture : new CultureInfo(culture);
            CultureInfo.CurrentCulture = chosen;
            CultureInfo.CurrentUICulture = chosen;
            assertion();
        }
        finally
        {
            CultureInfo.CurrentCulture = savedCulture;
            CultureInfo.CurrentUICulture = savedUiCulture;
        }
    }
}
