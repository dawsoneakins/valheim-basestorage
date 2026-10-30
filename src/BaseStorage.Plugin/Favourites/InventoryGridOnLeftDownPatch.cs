namespace BaseStorage.Plugin.Favourites;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Patch on InventoryGrid.OnLeftDown: an Alt+click on an occupied player inventory slot toggles its favourite flag and skips the
/// vanilla pickup (contract S-E004-014 section 5.1). The body is one fail-closed call; a failure lets vanilla run.
/// </summary>
[HarmonyPatch(typeof(InventoryGrid), PatchTargetNames.InventoryGrid.OnLeftDown)]
internal static class InventoryGridOnLeftDownPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Favourites.InventoryGridOnLeftDownPatch.Prefix";

    private static bool Prefix(InventoryGrid __instance, UIInputHandler clickHandler) =>
        PatchSafety.RunPrefix(PatchKey, () => FavouriteClicks.RunVanilla(__instance, clickHandler));
}
