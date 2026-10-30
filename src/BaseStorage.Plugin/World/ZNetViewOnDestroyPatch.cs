namespace BaseStorage.Plugin.World;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Patch on ZNetView.OnDestroy: unregisters a container whose view is destroyed (contract 5). The body is one fail-closed call that forwards to the plugin world; vanilla always continues.
/// </summary>
[HarmonyPatch(typeof(ZNetView), PatchTargetNames.ZNetView.OnDestroy)]
internal static class ZNetViewOnDestroyPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.World.ZNetViewOnDestroyPatch.Prefix";

    private static void Prefix(ZNetView __instance) =>
        PatchSafety.Run(PatchKey, __instance, static v => PluginWorld.Current?.OnNetViewDestroy(v));
}
