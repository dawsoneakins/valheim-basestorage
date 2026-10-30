namespace BaseStorage.Plugin.World;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Patch on Container.Awake: registers the container once its view exists (contract 5). The body is one fail-closed call that forwards to the plugin world; vanilla always continues.
/// </summary>
[HarmonyPatch(typeof(Container), PatchTargetNames.Container.Awake)]
internal static class ContainerAwakePatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.World.ContainerAwakePatch.Postfix";

    private static void Postfix(Container __instance) =>
        PatchSafety.Run(PatchKey, __instance, static v => PluginWorld.Current?.OnContainerAwake(v));
}
