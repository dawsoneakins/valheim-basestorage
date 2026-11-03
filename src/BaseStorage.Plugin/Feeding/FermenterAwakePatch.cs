namespace BaseStorage.Plugin.Feeding;

using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.World;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>Patch on Fermenter.Awake: records a loaded fermenter so it can be fed. A failure leaves it vanilla.</summary>
[HarmonyPatch(typeof(Fermenter), PatchTargetNames.Fermenter.Awake)]
internal static class FermenterAwakePatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Feeding.FermenterAwakePatch.Postfix";

    private static void Postfix(Fermenter __instance) =>
        PatchSafety.Run(PatchKey, __instance, static station => PluginWorld.Current?.OnFeedStationAwake(station));
}
