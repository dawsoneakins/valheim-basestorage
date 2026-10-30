namespace BaseStorage.Plugin.Favourites;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Patch on InventoryGrid.UpdateGui: after vanilla refreshes the slot elements, shows the favourite marker on every favourite slot of
/// the player grid (contract S-E004-014 section 5.1). The body is one fail-closed, allocation-free call; a failure leaves the grid vanilla.
/// </summary>
[HarmonyPatch(typeof(InventoryGrid), PatchTargetNames.InventoryGrid.UpdateGui)]
internal static class InventoryGridUpdateGuiPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Favourites.InventoryGridUpdateGuiPatch.Postfix";

    private static void Postfix(InventoryGrid __instance) =>
        PatchSafety.Run(PatchKey, __instance, static grid => FavouriteMarkers.Refresh(grid));
}
