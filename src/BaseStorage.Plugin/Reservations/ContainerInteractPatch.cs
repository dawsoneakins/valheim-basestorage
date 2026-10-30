namespace BaseStorage.Plugin.Reservations;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Prefix on Container.Interact: runs the reservation probe for a local Shift+E when the Debug logging setting is on. The
/// probe sets the result to true and skips vanilla; every other interaction runs vanilla (contract S-E005-002 section 4.4).
/// </summary>
[HarmonyPatch(typeof(Container), PatchTargetNames.Container.Interact)]
internal static class ContainerInteractPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Reservations.ContainerInteractPatch.Prefix";

    private static bool Prefix(Container __instance, bool hold, bool alt, ref bool __result) =>
        PatchSafety.RunPrefix(PatchKey, ref __result, (ref bool result) => PluginReservations.OnInteract(__instance, hold, alt, ref result));
}
