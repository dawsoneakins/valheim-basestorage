namespace BaseStorage.Plugin.Feeding;

using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.World;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on Fireplace.Awake: records a loaded fire so it can be fed. A failure leaves it vanilla.</summary>
[HarmonyPatch(typeof(Fireplace), PatchTargetNames.Fireplace.Awake)]
internal static class FireplaceAwakePatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Feeding.FireplaceAwakePatch.Postfix";

    private static void Postfix(Fireplace __instance) =>
        PatchSafety.Run(PatchKey, __instance, static station => PluginWorld.Current?.OnFeedStationAwake(station));
}
