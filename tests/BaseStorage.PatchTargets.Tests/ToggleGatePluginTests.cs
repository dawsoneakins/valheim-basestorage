namespace BaseStorage.PatchTargets.Tests;

using BaseStorage.PatchTargets.Tests.Checking;
using BaseStorage.PatchTargets.Tests.Infrastructure;

/// <summary>
/// GW-U-1820 (AC-1, AC-2; contract craft-from-base-toggle 7 and 8.2): the checker on the built Plugin. Skipped with BST2002 to
/// BST2004 when the game or the built Plugin is missing; the verify machine runs it unskipped (GW-C-1800).
/// </summary>
public sealed class ToggleGatePluginTests
{
    private static readonly string[] NineClasses =
    {
        "BaseStorage.Plugin.Building.PlayerHaveRequirementsPiecePatch",
        "BaseStorage.Plugin.Building.PlayerPlacePiecePatch",
        "BaseStorage.Plugin.Building.PlayerTryPlacePiecePatch",
        "BaseStorage.Plugin.Crafting.InventoryGuiDoCraftingPatch",
        "BaseStorage.Plugin.Crafting.PlayerConsumeResourcesPatch",
        "BaseStorage.Plugin.Crafting.PlayerHaveRequirementItemsPatch",
        "BaseStorage.Plugin.Requirements.HudSetupPieceInfoPatch",
        "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementListPatch",
        "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementPatch",
    };

    // GW-U-1820 (AC-1, AC-2): the built Plugin has no toggle diagnostic, every E3 class is found, and at least 12 gated methods are checked.
    [RequiresPluginFact]
    public void TheBuiltPluginHasNoToggleDiagnosticAndEveryE3ClassIsFound()
    {
        ToggleGateReport report = ToggleGateChecker.Check(BuildValues.PluginAssemblyPath, ToggleGateRules.Plugin);

        Assert.Empty(report.Diagnostics);
        Assert.All(NineClasses, name => Assert.Contains(name, report.E3Classes));
        Assert.True(report.CheckedMethods.Count >= 12, "The Plugin has 12 gated patch methods (contract 2.3).");
    }
}
