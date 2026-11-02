namespace BaseStorage.Plugin.BaseMenu;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on InventoryGui.Hide: letters typed into the menu's search box must not close the screen. A failure lets vanilla run.</summary>
[HarmonyPatch(typeof(InventoryGui), PatchTargetNames.InventoryGui.Hide)]
internal static class InventoryGuiHidePatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.BaseMenu.InventoryGuiHidePatch.Prefix";

    private static bool Prefix() =>
        PatchSafety.RunPrefix(PatchKey, static () => !BaseMenuPanel.TypingBlocksClose);
}
