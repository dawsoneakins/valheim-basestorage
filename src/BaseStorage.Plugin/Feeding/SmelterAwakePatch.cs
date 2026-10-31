namespace BaseStorage.Plugin.Feeding;

using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.World;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on Smelter.Awake: records a loaded station so it can be fed, and registers its selection message. A failure leaves it vanilla.</summary>
[HarmonyPatch(typeof(Smelter), PatchTargetNames.Smelter.Awake)]
internal static class SmelterAwakePatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Feeding.SmelterAwakePatch.Postfix";

    private static void Postfix(Smelter __instance) =>
        PatchSafety.Run(PatchKey, __instance, static station => PluginWorld.Current?.OnSmelterAwake(station));
}
