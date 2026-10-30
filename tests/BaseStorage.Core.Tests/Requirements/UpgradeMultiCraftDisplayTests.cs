namespace BaseStorage.Core.Tests.Requirements;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Crafting;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Crafting;
using BaseStorage.Plugin.Requirements;
using Xunit;

/// <summary>
/// GW-U-1720 to GW-U-1722 (AC-1, AC-3, AC-5, AC-6; contract upgrade-multicraft 4.5 DP1-DP7 and the parity invariant): the crafting panel
/// display for upgrade and multi-craft amounts, through RequirementDisplaySession over FakeRequirementDisplayWorld.
/// </summary>
public sealed class UpgradeMultiCraftDisplayTests
{
    /// <summary>The upgrade panel rows DP2 to DP4 and DP1 (the Bronze row), with the expected text, short flag and chest flag.</summary>
    /// <returns>The rows: chest five Bronze, chest seven Bronze, quality, prefab, need, text, short, shows chests.</returns>
    public static TheoryData<int, int, int, string, int, string, bool, bool> UpgradePanelRows() => new()
    {
        { 7, 4, 4, "Bronze", 16, "5 (+11) / 16", false, true },
        { 7, 3, 4, "Bronze", 16, "5 (+10) / 16", true, true },
        { 3, 0, 3, "Bronze", 8, "5 (+3) / 8", false, true },
        { 2, 0, 3, "Bronze", 8, "5 (+2) / 8", true, true },
    };

    /// <summary>The multi-craft panel rows DP5 to DP7, one per multiplier, with the Wood and Feathers texts and shorts.</summary>
    /// <returns>The rows: multiplier, Wood text, Wood short, Feathers text, Feathers short.</returns>
    public static TheoryData<int, string, bool, string, bool> MultiCraftPanelRows() => new()
    {
        { 5, "10 (+38) / 40", false, "4 (+8) / 10", false },
        { 6, "10 (+38) / 48", false, "4 (+8) / 12", false },
        { 7, "10 (+38) / 56", true, "4 (+8) / 14", true },
    };

    /// <summary>The eight display rows DP1, DP1b, DP2, DP3, DP4, DP5, DP6 and DP7 of contract 4.5.</summary>
    /// <returns>The row ids.</returns>
    public static IEnumerable<object[]> ParityRows() =>
        new[] { "DP1", "DP1b", "DP2", "DP3", "DP4", "DP5", "DP6", "DP7" }.Select(row => new object[] { row });

    // GW-U-1720 (AC-1, AC-6, DP1 to DP4): the Bronze upgrade line shows inventory and chests over the level 4 or level 3 need, and is short exactly when one short.
    [Theory]
    [MemberData(nameof(UpgradePanelRows))]
    public void TheUpgradePanelShowsTheQualityScaledNeedAndTheShortFlag(int chestFive, int chestSeven, int quality, string prefab, int need, string text, bool isShort, bool showsChests)
    {
        var world = new FakeRequirementDisplayWorld(UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldU(chestFive, chestSeven)));
        var session = new RequirementDisplaySession(world);
        session.EnterCraftingPanel(quality, 1);

        RequirementDisplay? display = session.Resolve(prefab, need, need.ToString(CultureInfo.InvariantCulture));

        Assert.True(display.HasValue);
        Assert.Equal(text, display!.Value.Text);
        Assert.Equal(isShort, display.Value.IsShort);
        Assert.Equal(showsChests, display.Value.ShowsChests);
        Assert.Equal(new[] { "ReadCraftingPanel(" + quality + ", 1)" }, world.Calls.ToArray());
    }

    // GW-U-1720 (case-count guard): the upgrade panel table has the four rows DP1 to DP4 of contract 4.5 that carry a chest.
    [Fact]
    public void TheUpgradePanelTableHasFourRows()
    {
        Assert.Equal(4, UpgradePanelRows().Count());
    }

    // GW-U-1720 (AC-1, DP1b): a Wood line with no chest stock keeps the vanilla text, the same instance, and is not short.
    [Fact]
    public void AWoodLineWithNoChestStockKeepsTheVanillaTextAsTheSameInstance()
    {
        var world = new FakeRequirementDisplayWorld(UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldU(7, 4)));
        var session = new RequirementDisplaySession(world);
        session.EnterCraftingPanel(4, 1);
        string vanilla = "4";

        RequirementDisplay? display = session.Resolve("Wood", 4, vanilla);

        Assert.True(display.HasValue);
        Assert.True(ReferenceEquals(vanilla, display!.Value.Text));
        Assert.False(display.Value.IsShort);
        Assert.False(display.Value.ShowsChests);
        Assert.Equal(new[] { "ReadCraftingPanel(4, 1)" }, world.Calls.ToArray());
    }

    // GW-U-1721 (AC-3, DP5 to DP7): the Wood and Feathers lines of each multiplier show the multiplied need, short only past the six craft boundary.
    [Theory]
    [MemberData(nameof(MultiCraftPanelRows))]
    public void TheMultiCraftPanelShowsTheMultipliedNeedAndTheShortFlag(int multiplier, string woodText, bool woodShort, string featherText, bool featherShort)
    {
        var world = new FakeRequirementDisplayWorld(UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldA()));
        var session = new RequirementDisplaySession(world);
        session.EnterCraftingPanel(1, multiplier);

        RequirementDisplay? wood = session.Resolve("Wood", 8 * multiplier, (8 * multiplier).ToString(CultureInfo.InvariantCulture));
        RequirementDisplay? feathers = session.Resolve("Feathers", 2 * multiplier, (2 * multiplier).ToString(CultureInfo.InvariantCulture));

        Assert.True(wood.HasValue);
        Assert.True(feathers.HasValue);
        Assert.Equal(woodText, wood!.Value.Text);
        Assert.Equal(woodShort, wood.Value.IsShort);
        Assert.True(wood.Value.ShowsChests);
        Assert.Equal(featherText, feathers!.Value.Text);
        Assert.Equal(featherShort, feathers.Value.IsShort);
        Assert.True(feathers.Value.ShowsChests);
        Assert.Equal(new[] { "ReadCraftingPanel(1, " + multiplier + ")" }, world.Calls.ToArray());
    }

    // GW-U-1721 (case-count guard): the multi-craft panel table has the three rows DP5 to DP7 of contract 4.5.
    [Fact]
    public void TheMultiCraftPanelTableHasThreeRows()
    {
        Assert.Equal(3, MultiCraftPanelRows().Count());
    }

    // GW-U-1722 (AC-3, AC-5, 4.5 parity invariant): each line's short flag equals not met, FindFirstMissing is null exactly when no line is short, and the displayed need equals the requirement amount.
    [Theory]
    [MemberData(nameof(ParityRows))]
    public void EachDisplayLineAgreesWithTheAvailabilityRequirementForItsQualityAndMultiplier(string row)
    {
        (BaseSnapshot snapshot, int quality, int multiplier, RecipeLine[] lines) = ParityRow(row);
        var session = new RequirementDisplaySession(new FakeRequirementDisplayWorld(snapshot));
        session.EnterCraftingPanel(quality, multiplier);
        IReadOnlyList<CraftRequirement> reqs = UpgradeMultiCraftFixtures.Requirements(lines, multiplier);
        AvailabilityResult result = BaseAvailability.Compute(snapshot, reqs);

        for (int index = 0; index < reqs.Count; index++)
        {
            string vanilla = reqs[index].Amount.ToString(CultureInfo.InvariantCulture);
            RequirementDisplay? display = session.Resolve(reqs[index].PrefabName, reqs[index].Amount, vanilla);

            Assert.True(display.HasValue, $"{row} line {index} has no display.");
            Assert.Equal(!result.Requirements[index].IsMet, display!.Value.IsShort);
            Assert.Equal(reqs[index].Amount.ToString(CultureInfo.InvariantCulture), display.Value.Text.Substring(display.Value.Text.LastIndexOf('/') + 1).Trim());
        }

        bool anyShort = result.Requirements.Any(requirement => !requirement.IsMet);
        Assert.Equal(!anyShort, BaseAvailability.FindFirstMissing(snapshot, reqs) is null);
    }

    // GW-U-1722 (case-count guard): the parity table has the eight display rows of contract 4.5 (DP1, DP1b, DP2 to DP4, DP5 to DP7).
    [Fact]
    public void TheParityTableHasEightRows()
    {
        Assert.Equal(8, ParityRows().Count());
    }

    private static (BaseSnapshot Snapshot, int Quality, int Multiplier, RecipeLine[] Lines) ParityRow(string row) => row switch
    {
        "DP1" or "DP1b" => (UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldU(7, 4)), 4, 1, UpgradeMultiCraftFixtures.SwordLines(4)),
        "DP2" => (UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldU(7, 3)), 4, 1, UpgradeMultiCraftFixtures.SwordLines(4)),
        "DP3" => (UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldU(3, 0)), 3, 1, UpgradeMultiCraftFixtures.SwordLines(3)),
        "DP4" => (UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldU(2, 0)), 3, 1, UpgradeMultiCraftFixtures.SwordLines(3)),
        "DP5" => (UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldA()), 1, 5, UpgradeMultiCraftFixtures.ArrowLines()),
        "DP6" => (UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldA()), 1, 6, UpgradeMultiCraftFixtures.ArrowLines()),
        "DP7" => (UpgradeMultiCraftFixtures.Snapshot(UpgradeMultiCraftFixtures.WorldA()), 1, 7, UpgradeMultiCraftFixtures.ArrowLines()),
        _ => throw new ArgumentOutOfRangeException(nameof(row)),
    };
}
