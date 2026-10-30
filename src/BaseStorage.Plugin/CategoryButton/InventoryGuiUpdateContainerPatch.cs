namespace BaseStorage.Plugin.CategoryButton;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Patch on InventoryGui.UpdateContainer: after vanilla shows or hides the container panel for the frame, creates the Category button
/// once and keeps its label, enabled state and visibility in step with the open chest (contract S-E004-004 section 4.5). The body is
/// one fail-closed, allocation-free call; a failure leaves the panel vanilla.
/// </summary>
[HarmonyPatch(typeof(InventoryGui), PatchTargetNames.InventoryGui.UpdateContainer)]
internal static class InventoryGuiUpdateContainerPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.CategoryButton.InventoryGuiUpdateContainerPatch.Postfix";

    private static void Postfix(InventoryGui __instance) =>
        PatchSafety.Run(PatchKey, __instance, static gui => ChestCategoryButton.Refresh(gui));
}
