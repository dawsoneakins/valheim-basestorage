namespace BaseStorage.Plugin.Crafting;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Prefix on InventoryGui.DoCrafting: for a craft that needs chest items it skips vanilla and starts the atomic consumption,
/// and the item is created only by the commit call after the materials are paid (contract craft-from-base 5.3).
/// </summary>
[HarmonyPatch(typeof(InventoryGui), PatchTargetNames.InventoryGui.DoCrafting)]
[CraftFromBasePatch]
internal static class InventoryGuiDoCraftingPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Crafting.InventoryGuiDoCraftingPatch.Prefix";

    private static bool Prefix(InventoryGui __instance, Player player) =>
        !CraftFromBaseGate.IsOpen() || PrefixCore(__instance, player);

    private static bool PrefixCore(InventoryGui gui, Player player) =>
        PatchSafety.RunPrefix(PatchKey, () => PluginCrafting.OnDoCrafting(gui, player));
}
