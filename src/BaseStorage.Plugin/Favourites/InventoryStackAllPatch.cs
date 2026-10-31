namespace BaseStorage.Plugin.Favourites;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Patch on Inventory.StackAll: guards favourites for exactly as long as vanilla Place stacks runs. The finalizer ends the guard even when
/// vanilla throws. A failure leaves vanilla Place stacks.
/// </summary>
[HarmonyPatch(typeof(Inventory), PatchTargetNames.Inventory.StackAll)]
internal static class InventoryStackAllPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Favourites.InventoryStackAllPatch.Prefix";

    /// <summary>The patch key of the finalizer.</summary>
    internal const string FinalizerKey = "BaseStorage.Plugin.Favourites.InventoryStackAllPatch.Finalizer";

    private static void Prefix(Inventory fromInventory) =>
        PatchSafety.Run(PatchKey, fromInventory, static from => FavouritePlaceGuard.Begin(from));

    private static void Finalizer() =>
        PatchSafety.Run(FinalizerKey, static () => FavouritePlaceGuard.End());
}
