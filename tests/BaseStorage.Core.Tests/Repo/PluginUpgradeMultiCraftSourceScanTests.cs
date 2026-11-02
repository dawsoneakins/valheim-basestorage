namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;

/// <summary>
/// GW-U-1723 (AC-1, AC-3, AC-7, AC-9; contract upgrade-multicraft 7.4 UMS1-UMS6): the Plugin source reads the quality and the multiplier
/// the way vanilla does, forwards them to the check and the craft, never calls CraftRequirement.ForLevel, and returns SettingOff before
/// it reads either value. Files are CRLF-normalised and comment-stripped by PluginSourceScan (ADR-GWIN-124).
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginUpgradeMultiCraftSourceScanTests
{
    private const string GameCraftFile = "src/BaseStorage.Plugin/Crafting/GameCraftGame.cs";
    private const string PluginCraftingFile = "src/BaseStorage.Plugin/Crafting/PluginCrafting.cs";
    private const string DisplayFile = "src/BaseStorage.Plugin/Requirements/PluginRequirementDisplay.cs";

    // GW-U-1723 (UMS1, AC-1, U5): the craft reads the upgrade quality as quality + 1 and the multi-craft amount from the vanilla flag.
    [Fact]
    public void TheCraftReadsTheUpgradeQualityAndTheMultiCraftAmountAsVanillaDoes()
    {
        string text = PluginSourceScan.Sources()[GameCraftFile];

        Assert.Matches(@"\.m_quality\s*\+\s*1\s*:\s*1", text);
        Assert.Matches(@"m_multiCrafting\s*\?\s*\w+\.m_multiCraftAmount\s*:\s*1", text);
    }

    // An upgrade runs from the Upgrade tab, where the game's InCraftTab is false; asking only that cancelled every chest-funded upgrade.
    [Fact]
    public void APendingUpgradeChecksTheUpgradeTab()
    {
        string text = PluginSourceScan.Sources()[GameCraftFile];

        Assert.Matches(@"UpgradeItem\s*!=\s*null\s*\?\s*gui\.InUpradeTab\(\)\s*:\s*gui\.InCraftTab\(\)", text);
    }

    // GW-U-1723 (UMS2, AC-1, AC-3): the check forwards vanilla's quality and amount, and the craft forwards the selection's quality and multiplier.
    [Fact]
    public void TheCheckAndTheCraftForwardTheQualityAndTheMultiplier()
    {
        string text = PluginSourceScan.Sources()[PluginCraftingFile];

        Assert.Matches(@"EvaluateGate\(\s*player\s*,\s*recipe\s*,\s*discover\s*,\s*qualityLevel\s*,\s*amount\s*,", text);
        Assert.Matches(@"EvaluateGate\([^;]*read\.QualityLevel\s*,\s*read\.Multiplier\s*,", text);
    }

    // GW-U-1723 (UMS3, AC-1, AC-3, UM2, UM4): the per-craft amount is the game's GetAmount at the quality, and the list is built with the multiplier.
    [Fact]
    public void TheRequirementListIsTheGameAmountAtTheQualityTimesTheMultiplier()
    {
        string text = PluginSourceScan.Sources()[GameCraftFile];

        Assert.Matches(@"\.GetAmount\(\s*qualityLevel\s*\)", text);
        Assert.Matches(@"CraftRequirementLines\.Build\([^;]*,\s*multiplier\s*\)", text);
    }

    // GW-U-1723 (UMS4, AC-3, UM9): the crafting panel need is GetAmount(quality) times the craft multiplier.
    [Fact]
    public void TheCraftingPanelNeedIsTheGameAmountTimesTheCraftMultiplier()
    {
        string text = PluginSourceScan.Sources()[DisplayFile];

        Assert.Matches(@"GetAmount\(\s*quality\s*\)\s*\*\s*craftMultiplier", text);
    }

    // GW-U-1723 (UMS5, AC-9, UM2, 3.4): no Plugin file calls CraftRequirement.ForLevel, which differs from vanilla from level 4.
    [Fact]
    public void NoPluginFileCallsForLevel()
    {
        var sources = PluginSourceScan.Sources();

        Assert.Empty(PluginSourceScan.FilesMatching(sources, @"ForLevel\("));
    }

    // GW-U-1723 (UMS6, AC-7, UM6): in EvaluateGate the first return is SettingOff, and it precedes every read of the quality or the multiplier.
    [Fact]
    public void TheSettingGateReturnsBeforeAnyQualityOrMultiplierIsRead()
    {
        string body = PluginSourceScan.MethodBody(PluginSourceScan.Sources()[GameCraftFile], @"internal\s+CraftGateReason\s+EvaluateGate\s*\(");

        Match firstReturn = Regex.Match(body, @"\breturn\b[^;]*;");
        Assert.True(firstReturn.Success, "EvaluateGate has no return statement.");
        Assert.Contains("CraftGateReason.SettingOff", firstReturn.Value, System.StringComparison.Ordinal);

        Match firstRead = Regex.Match(body, @"\b(qualityLevel|multiplier)\b");
        Assert.True(firstRead.Success, "EvaluateGate never reads the quality or the multiplier.");
        Assert.True(firstReturn.Index < firstRead.Index, "The SettingOff return must precede every read of the quality or the multiplier.");
    }
}
