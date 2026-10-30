namespace BaseStorage.Plugin.World;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Patch on CraftingStation.Start: registers the station once its view exists (contract 5). The body is one fail-closed call that forwards to the plugin world; vanilla always continues.
/// </summary>
[HarmonyPatch(typeof(CraftingStation), PatchTargetNames.CraftingStation.Start)]
internal static class CraftingStationStartPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.World.CraftingStationStartPatch.Postfix";

    private static void Postfix(CraftingStation __instance) =>
        PatchSafety.Run(PatchKey, __instance, static v => PluginWorld.Current?.OnStationStart(v));
}
