namespace BaseStorage.Core.Tests.PatchGuard;

using System.Reflection;
using BaseStorage.Core.Tests.Repo;

/// <summary>
/// AC-12 (ADR-GWIN-045, GW-I-700): on the built Plugin dll, the guard finds the two favourite patch bodies among exactly the twenty-five (edited by S-E003-004, ADR-GWIN-124: plus the three Crafting prefixes;
/// edited by integration: plus the S-E003-005 Requirements, S-E003-006 Building and S-E004-004 Category button bodies)
/// Plugin patch bodies (ADR-GWIN-055: union with the S-E002-004 World and S-E002-005 Hover patches; ADR-GWIN-059: plus the S-E005-002 Reservations prefixes; ADR-GWIN-104: plus the S-E004-004 Category button postfix) and no violation.
/// The body names carry the kind (Prefix or Postfix). The dll is read with PEReader and never loaded. A missing class fails this test.
/// </summary>
[Trait("Category", "Repo")]
[Collection("MSBuildProcess")]
public sealed class PluginFavouritesPatchGuardTests
{
    private const string Helper = "BaseStorage.Plugin.Patching.PatchSafety";

    // GW-I-700
    [Fact]
    public void TheGuardSeesTheTwoFavouritePatchBodiesAmongExactlyElevenAndNoViolation()
    {
        AssemblyConfigurationAttribute? configuration = typeof(PluginFavouritesPatchGuardTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
        Assert.NotNull(configuration);
        string path = PluginDll.EnsureBuilt(configuration.Configuration ?? string.Empty);

        // Edited by S-E003-008, craft-from-base-toggle 8.3 amendment (ADR-GWIN-136)
        PatchGuardResult result = PatchGuardScanner.Scan(path, new PatchGuardRules("HarmonyLib", Helper, GateMethodFullName: "BaseStorage.Plugin.Patching.CraftFromBaseGate.IsOpen"));

        Assert.Equal(
            new[]
            {
                "BaseStorage.Plugin.Building.PlayerHaveRequirementsPiecePatch.Prefix",
                "BaseStorage.Plugin.Building.PlayerPlacePiecePatch.Prefix",
                "BaseStorage.Plugin.Building.PlayerTryPlacePiecePatch.Postfix",
                "BaseStorage.Plugin.Building.PlayerTryPlacePiecePatch.Prefix",
                "BaseStorage.Plugin.CategoryButton.InventoryGuiUpdateContainerPatch.Postfix",
                // Edited by S-E010-002 (organize-console-command 6.2): the console registration postfix.
                "BaseStorage.Plugin.Commands.TerminalInitTerminalPatch.Postfix",
                "BaseStorage.Plugin.Crafting.InventoryGuiDoCraftingPatch.Prefix",
                "BaseStorage.Plugin.Crafting.PlayerConsumeResourcesPatch.Prefix",
                "BaseStorage.Plugin.Crafting.PlayerHaveRequirementItemsPatch.Prefix",
                "BaseStorage.Plugin.Favourites.InventoryGridOnLeftDownPatch.Prefix",
                "BaseStorage.Plugin.Favourites.InventoryGridUpdateGuiPatch.Postfix",
                "BaseStorage.Plugin.Hover.ContainerHoverTextPatch.Postfix",
                "BaseStorage.Plugin.Hover.CraftingStationHoverTextPatch.Postfix",
                "BaseStorage.Plugin.Networking.FejdStartupShowConnectErrorPatch.Postfix",
                "BaseStorage.Plugin.Networking.ZNetOnNewConnectionPatch.Postfix",
                // Edited by S-E003-005, ADR-GWIN-127: plus the five Requirements display bodies (twenty in all).
                "BaseStorage.Plugin.Requirements.HudSetupPieceInfoPatch.Postfix",
                "BaseStorage.Plugin.Requirements.HudSetupPieceInfoPatch.Prefix",
                "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementListPatch.Postfix",
                "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementListPatch.Prefix",
                "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementPatch.Postfix",
                "BaseStorage.Plugin.Reservations.ContainerInteractPatch.Prefix",
                "BaseStorage.Plugin.Reservations.ContainerOpenResponsePatch.Prefix",
                // Edited by S-E004-012 (ADR-GWIN-136): plus the StationActions UpdateRecipe postfix.
                "BaseStorage.Plugin.StationActions.InventoryGuiUpdateRecipePatch.Postfix",
                "BaseStorage.Plugin.Tooltips.ItemDataGetTooltipPatch.Postfix",
                "BaseStorage.Plugin.World.ContainerAwakePatch.Postfix",
                "BaseStorage.Plugin.World.CraftingStationOnDestroyPatch.Prefix",
                "BaseStorage.Plugin.World.CraftingStationStartPatch.Postfix",
                "BaseStorage.Plugin.World.ZNetViewOnDestroyPatch.Prefix",
            },
            result.PatchBodies);
        Assert.True(result.Violations.Count == 0, PatchGuardScanner.FormatViolations(result));
    }
}
