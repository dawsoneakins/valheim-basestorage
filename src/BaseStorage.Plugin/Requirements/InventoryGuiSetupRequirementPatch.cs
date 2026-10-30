namespace BaseStorage.Plugin.Requirements;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;
using UnityEngine;

/// <summary>
/// Postfix on InventoryGui.SetupRequirement: inside the crafting panel or the build tooltip it rewrites the amount of a
/// requirement that base chests contribute to (contract requirement-display 5.4). Failure leaves the vanilla display.
/// </summary>
[HarmonyPatch(typeof(InventoryGui), PatchTargetNames.InventoryGui.SetupRequirement)]
[CraftFromBasePatch]
internal static class InventoryGuiSetupRequirementPatch
{
    /// <summary>The patch key used by the fail-closed wrapper.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementPatch.Postfix";

    private static void Postfix(Transform elementRoot, Piece.Requirement req, bool craft, int quality, int craftMultiplier, bool __result)
    {
        if (CraftFromBaseGate.IsOpen())
        {
            PostfixCore(elementRoot, req, craft, quality, craftMultiplier, __result);
        }
    }

    private static void PostfixCore(Transform elementRoot, Piece.Requirement req, bool craft, int quality, int craftMultiplier, bool result) =>
        PatchSafety.Run(PatchKey, () => PluginRequirementDisplay.OnSetupRequirement(elementRoot, req, craft, quality, craftMultiplier, result));
}
