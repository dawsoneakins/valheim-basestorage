namespace BaseStorage.Plugin.CharacterMenu;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Patch on Humanoid.DropInvalidItems: the worn row counts as inside the inventory for exactly as long as this check runs, or the game
/// drops worn gear on every spawn. The finalizer restores the height even when vanilla throws. A failure leaves the vanilla check.
/// </summary>
[HarmonyPatch(typeof(Humanoid), PatchTargetNames.Humanoid.DropInvalidItems)]
internal static class HumanoidDropInvalidItemsPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.CharacterMenu.HumanoidDropInvalidItemsPatch.Prefix";

    /// <summary>The patch key of the finalizer.</summary>
    internal const string FinalizerKey = "BaseStorage.Plugin.CharacterMenu.HumanoidDropInvalidItemsPatch.Finalizer";

    private static void Prefix(Humanoid __instance, out int __state) =>
        __state = PatchSafety.Run(PatchKey, __instance, WornItems.NotWidened, static (who, _) => WornItems.WidenForDropCheck(who));

    private static void Finalizer(Humanoid __instance, int __state) =>
        PatchSafety.Run(FinalizerKey, __instance, __state, static (who, height) => WornItems.RestoreAfterDropCheck(who, height));
}
