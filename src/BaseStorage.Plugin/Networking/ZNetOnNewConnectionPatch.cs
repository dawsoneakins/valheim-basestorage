namespace BaseStorage.Plugin.Networking;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Postfix on ZNet.OnNewConnection: registers the version RPC on the new peer and sends the local payload once (contract
/// version-rpc-contracts.md 5.1). The body is one fail-closed call that forwards to the handshake adapter; vanilla always continues.
/// </summary>
[HarmonyPatch(typeof(ZNet), PatchTargetNames.ZNet.OnNewConnection)]
internal static class ZNetOnNewConnectionPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Networking.ZNetOnNewConnectionPatch.Postfix";

    private static void Postfix(ZNetPeer peer) =>
        PatchSafety.Run(PatchKey, peer, static p => PluginVersionHandshake.OnNewConnection(p));
}
