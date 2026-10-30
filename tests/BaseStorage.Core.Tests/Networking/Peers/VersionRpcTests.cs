namespace BaseStorage.Core.Tests.Networking.Peers;

using System;
using System.Text.RegularExpressions;
using BaseStorage.Core.Networking.Peers;
using BaseStorage.Core.Tests.Repo;

/// <summary>
/// AC-9.4 (contract version-rpc-contracts.md 2.1 and 6.3): the RPC names are contracted constants, and the vanilla error name
/// agrees with the RPC_Error handler listed in src/Shared/PatchTargetNames.cs, which the PatchTargets suite resolves in the game.
/// </summary>
[Trait("Category", "Repo")]
public sealed class VersionRpcTests
{
    private const string SharedFile = "src/Shared/PatchTargetNames.cs";

    // GW-U-1262 (2.1): the two names, their shape, and no other constant.
    [Fact]
    public void TheNamesAreContracted()
    {
        Assert.Equal("BaseStorage_Version", VersionRpc.Name);
        Assert.Equal("Error", VersionRpc.VanillaErrorName);
        Assert.Matches("^[A-Za-z_]+$", VersionRpc.Name);
        Assert.StartsWith("BaseStorage", VersionRpc.Name, StringComparison.Ordinal);
    }

    // GW-U-1262 (6.3): "RPC_" + VanillaErrorName is the ZNet.RpcError constant of the shared target list.
    [Fact]
    public void TheVanillaErrorNameMatchesTheListedHandler()
    {
        string text = RepoRoot.ReadText(SharedFile);
        Match handler = Regex.Match(text, @"public const string RpcError = ""([^""]+)"";");

        Assert.True(handler.Success, "PatchTargetNames.ZNet has no RpcError constant.");
        Assert.Equal("RPC_" + VersionRpc.VanillaErrorName, handler.Groups[1].Value);
        Assert.Contains("public const string OnNewConnection = \"OnNewConnection\";", text, StringComparison.Ordinal);
        Assert.Contains("public const string ZNetOnNewConnection = \"ZNet.OnNewConnection(ZNetPeer)\";", text, StringComparison.Ordinal);
        Assert.Contains("public const string FejdStartupShowConnectError = \"FejdStartup.ShowConnectError(ZNet+ConnectionStatus)\";", text, StringComparison.Ordinal);
    }
}
