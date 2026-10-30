namespace BaseStorage.Plugin.Networking;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Postfix on FejdStartup.ShowConnectError: appends the pending BaseStorage version mismatch to the vanilla connection-error panel
/// when it shows the incompatible-version error (contract version-rpc-contracts.md 5.4). The body is one fail-closed call that
/// forwards to the handshake adapter; the vanilla text always survives.
/// </summary>
[HarmonyPatch(typeof(FejdStartup), PatchTargetNames.FejdStartup.ShowConnectError)]
internal static class FejdStartupShowConnectErrorPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Networking.FejdStartupShowConnectErrorPatch.Postfix";

    private static void Postfix(FejdStartup __instance) =>
        PatchSafety.Run(PatchKey, __instance, static f => PluginVersionHandshake.OnConnectErrorShown(f));
}
