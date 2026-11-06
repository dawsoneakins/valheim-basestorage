namespace BaseStorage.Plugin.Favourites;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on InventoryGrid.OnRightDown: Shift + right-click puts a stack away instead of using it. A failure lets vanilla run.</summary>
[HarmonyPatch(typeof(InventoryGrid), PatchTargetNames.InventoryGrid.OnRightDown)]
internal static class InventoryGridOnRightDownPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Favourites.InventoryGridOnRightDownPatch.Prefix";

    private static bool Prefix(InventoryGrid __instance, UIInputHandler element) =>
        PatchSafety.RunPrefix(PatchKey, () => QuickPutAway.RunVanilla(__instance, element));
}
