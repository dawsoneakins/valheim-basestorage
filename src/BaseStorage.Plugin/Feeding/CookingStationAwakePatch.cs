namespace BaseStorage.Plugin.Feeding;

using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.World;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on CookingStation.Awake: records a loaded cooking station so it can be fed. A failure leaves it vanilla.</summary>
[HarmonyPatch(typeof(CookingStation), PatchTargetNames.CookingStation.Awake)]
internal static class CookingStationAwakePatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Feeding.CookingStationAwakePatch.Postfix";

    private static void Postfix(CookingStation __instance) =>
        PatchSafety.Run(PatchKey, __instance, static station => PluginWorld.Current?.OnFeedStationAwake(station));
}
