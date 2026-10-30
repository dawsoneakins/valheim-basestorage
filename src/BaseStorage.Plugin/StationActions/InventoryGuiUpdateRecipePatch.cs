namespace BaseStorage.Plugin.StationActions;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Patch on InventoryGui.UpdateRecipe: after vanilla has updated the crafting panel for the frame, creates the Organize base button and
/// the base status line once and keeps them in step with the station in use (contract S-E004-012 section 3.5). The body is one
/// fail-closed call; a failure leaves the panel vanilla.
/// </summary>
[HarmonyPatch(typeof(InventoryGui), PatchTargetNames.InventoryGui.UpdateRecipe, typeof(Player), typeof(float))]
internal static class InventoryGuiUpdateRecipePatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.StationActions.InventoryGuiUpdateRecipePatch.Postfix";

    private static void Postfix(InventoryGui __instance) => PatchSafety.Run(PatchKey, __instance, static gui => StationActionsPanel.Refresh(gui));
}
