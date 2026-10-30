namespace BaseStorage.Plugin.Reservations;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Prefix on Container.RPC_OpenResponse: consumes the response of a reservation that sent the request, or of a late one, so
/// the vanilla chest UI does not open for the requester (contract S-E005-002 section 4.4, RA6). Any failure runs vanilla.
/// </summary>
[HarmonyPatch(typeof(Container), PatchTargetNames.Container.RpcOpenResponse)]
internal static class ContainerOpenResponsePatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Reservations.ContainerOpenResponsePatch.Prefix";

    private static bool Prefix(Container __instance, bool granted) =>
        PatchSafety.RunPrefix(PatchKey, () => PluginReservations.OnOpenResponse(__instance, granted));
}
