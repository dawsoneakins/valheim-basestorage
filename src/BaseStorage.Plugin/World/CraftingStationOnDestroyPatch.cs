namespace BaseStorage.Plugin.World;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Patch on CraftingStation.OnDestroy: unregisters the station (contract 5). The body is one fail-closed call that forwards to the plugin world; vanilla always continues.
/// </summary>
[HarmonyPatch(typeof(CraftingStation), PatchTargetNames.CraftingStation.OnDestroy)]
internal static class CraftingStationOnDestroyPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.World.CraftingStationOnDestroyPatch.Prefix";

    private static void Prefix(CraftingStation __instance) =>
        PatchSafety.Run(PatchKey, __instance, static v => PluginWorld.Current?.OnStationDestroy(v));
}
