namespace BaseStorage.Plugin.BaseMenu;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on InventoryGui.UpdateRecipeGamepadInput: the recipe list waits while one of the mod's panels has the gamepad. A failure runs vanilla.</summary>
[HarmonyPatch(typeof(InventoryGui), PatchTargetNames.InventoryGui.UpdateRecipeGamepadInput)]
internal static class InventoryGuiRecipeGamepadPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.BaseMenu.InventoryGuiRecipeGamepadPatch.Prefix";

    private static bool Prefix() => PatchSafety.RunPrefix(PatchKey, static () => !PanelGamepad.Active);
}
