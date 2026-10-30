namespace BaseStorage.PatchTargets.Tests;

using System;
using System.IO;
using System.Reflection;
using BaseStorage.PatchTargets.Tests.Checking;
using BaseStorage.Shared;

/// <summary>
/// S-E006-002 AC-9.4 (contract version-rpc-contracts.md 6.1, GW-C-1260..1264): the 32 version-handshake constants and the two
/// HarmonyPatched ids hold the contracted values, every new owner class has a TypeName, the shape rules stay clean, and the new
/// constants carry summaries. The game-backed resolution of each row is GW-U-109 in GameTargetContractTests.
/// </summary>
public sealed class PatchTargetNamesNetworkingTests
{
    private const string SharedTypeName = "BaseStorage.Shared.PatchTargetNames";

    // GW-C-1260: every new constant holds its contracted value.
    [Theory]
    [InlineData("ZNet", "OnNewConnection", "OnNewConnection")]
    [InlineData("ZNet", "OnNewConnectionParametersPeer", "ZNetPeer")]
    [InlineData("ZNet", "Disconnect", "Disconnect")]
    [InlineData("ZNet", "GetPeers", "GetPeers")]
    [InlineData("ZNet", "SetExternalError", "SetExternalError")]
    [InlineData("ZNet", "GetConnectionStatus", "GetConnectionStatus")]
    [InlineData("ZNet", "RpcError", "RPC_Error")]
    [InlineData("ZNet", "GetInstance", "get_instance")]
    [InlineData("ZNetPeer", "TypeName", "ZNetPeer")]
    [InlineData("ZNetPeer", "FieldRpc", "m_rpc")]
    [InlineData("ZNetPeer", "FieldRpcType", "ZRpc")]
    [InlineData("ZNetPeer", "FieldServer", "m_server")]
    [InlineData("ZNetPeer", "FieldServerType", "System.Boolean")]
    [InlineData("ZRpc", "TypeName", "ZRpc")]
    [InlineData("ZRpc", "Register", "Register")]
    [InlineData("ZRpc", "RegisterParametersHandler", "System.String,System.Action`2[ZRpc,T]")]
    [InlineData("ZRpc", "Invoke", "Invoke")]
    [InlineData("ZPackage", "TypeName", "ZPackage")]
    [InlineData("ZPackage", "Write", "Write")]
    [InlineData("ZPackage", "WriteParametersBytes", "System.Byte[]")]
    [InlineData("ZPackage", "ReadByteArray", "ReadByteArray")]
    [InlineData("ZPackage", "ReadByteArrayParametersNone", "")]
    [InlineData("FejdStartup", "TypeName", "FejdStartup")]
    [InlineData("FejdStartup", "ShowConnectError", "ShowConnectError")]
    [InlineData("FejdStartup", "ShowConnectErrorParametersStatus", "ZNet+ConnectionStatus")]
    [InlineData("FejdStartup", "FieldConnectionFailedError", "m_connectionFailedError")]
    [InlineData("FejdStartup", "FieldConnectionFailedErrorType", "TMPro.TMP_Text")]
    [InlineData("FejdStartup", "FieldConnectionFailedPanel", "m_connectionFailedPanel")]
    [InlineData("FejdStartup", "FieldConnectionFailedPanelType", "UnityEngine.GameObject")]
    [InlineData("ConnectionStatus", "TypeName", "ZNet+ConnectionStatus")]
    [InlineData("ConnectionStatus", "FieldErrorVersion", "ErrorVersion")]
    [InlineData("ConnectionStatus", "FieldErrorVersionType", "ZNet+ConnectionStatus")]
    [InlineData("HarmonyPatched", "ZNetOnNewConnection", "ZNet.OnNewConnection(ZNetPeer)")]
    [InlineData("HarmonyPatched", "FejdStartupShowConnectError", "FejdStartup.ShowConnectError(ZNet+ConnectionStatus)")]
    // Edited by S-E003-006, ADR-GWIN-132 (GW-U-1654): the three building ids of contract 8.1 join the set of eighteen.
    [InlineData("HarmonyPatched", "PlayerHaveRequirementsPiece", "Player.HaveRequirements(Piece,Player+RequirementMode)")]
    [InlineData("HarmonyPatched", "PlayerPlacePiece", "Player.PlacePiece(Piece,UnityEngine.Vector3,UnityEngine.Quaternion,System.Boolean,System.Boolean)")]
    [InlineData("HarmonyPatched", "PlayerTryPlacePiece", "Player.TryPlacePiece(Piece)")]
    public void VersionHandshakeConstantsHoldTheContractedValues(string owner, string member, string expected)
    {
        Type? holder = typeof(PatchTargetNames).GetNestedType(owner, BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(holder is not null, $"{SharedTypeName} has no nested class {owner}.");
        FieldInfo? field = holder!.GetField(member, BindingFlags.Public | BindingFlags.Static);
        Assert.True(field is not null, $"{SharedTypeName}.{owner} has no constant {member}.");

        Assert.Equal(expected, (string?)field!.GetValue(null));
    }

    // GW-C-1261: the five new owner classes classify as expected and the real list keeps a clean shape.
    [Fact]
    public void TheNewOwnersClassifyAsContractedAndTheShapeStaysClean()
    {
        var entries = TargetList.Classify(typeof(PatchTargetNames));

        Assert.Equal(TargetKind.MethodOverloads, entries.Single(e => e.Owner == "ZNet" && e.Const == "OnNewConnection").Kind);
        Assert.Equal(TargetKind.OverloadCompanion, entries.Single(e => e.Owner == "ZNet" && e.Const == "OnNewConnectionParametersPeer").Kind);
        Assert.Equal(TargetKind.MethodNameOnly, entries.Single(e => e.Owner == "ZNet" && e.Const == "GetPeers").Kind);
        Assert.Equal(TargetKind.MethodOverloads, entries.Single(e => e.Owner == "ZRpc" && e.Const == "Register").Kind);
        Assert.Equal(TargetKind.OverloadCompanion, entries.Single(e => e.Owner == "ZRpc" && e.Const == "RegisterParametersHandler").Kind);
        Assert.Equal(TargetKind.MethodOverloads, entries.Single(e => e.Owner == "ZPackage" && e.Const == "ReadByteArray").Kind);
        Assert.Equal(TargetKind.FieldTypeCompanion, entries.Single(e => e.Owner == "ConnectionStatus" && e.Const == "FieldErrorVersionType").Kind);
        Assert.Equal(TargetKind.FieldTypeCompanion, entries.Single(e => e.Owner == "FejdStartup" && e.Const == "FieldConnectionFailedErrorType").Kind);
        Assert.Equal(32, entries.Count(e => e.Owner is "ZNetPeer" or "ZRpc" or "ZPackage" or "FejdStartup" or "ConnectionStatus")
            + entries.Count(e => e.Owner == "ZNet" && e.Const is "OnNewConnection" or "OnNewConnectionParametersPeer" or "Disconnect" or "GetPeers" or "SetExternalError" or "GetConnectionStatus" or "RpcError" or "GetInstance"));
        Assert.Empty(ShapeRules.Check(typeof(PatchTargetNames)));
    }

    // GW-C-1262: the new constants carry summaries and the file stays LF without a BOM; the new owners sit before HarmonyPatched.
    [Fact]
    public void NewConstantsHaveSummariesAndTheNewOwnersSitBeforeHarmonyPatched()
    {
        string path = Path.Combine(RepoRootPath(), "src", "Shared", "PatchTargetNames.cs");
        byte[] bytes = File.ReadAllBytes(path);
        string text = File.ReadAllText(path);
        string[] lines = text.Split('\n');

        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "PatchTargetNames.cs has a BOM.");
        Assert.DoesNotContain('\r', text);
        foreach (string member in new[]
        {
            "OnNewConnection", "OnNewConnectionParametersPeer", "Disconnect", "GetPeers", "SetExternalError", "GetConnectionStatus", "RpcError",
            "FieldRpc", "FieldRpcType", "FieldServer", "FieldServerType", "Register", "RegisterParametersHandler", "Invoke",
            "WriteParametersBytes", "ReadByteArray", "ReadByteArrayParametersNone", "ShowConnectError", "ShowConnectErrorParametersStatus",
            "FieldConnectionFailedError", "FieldConnectionFailedErrorType", "FieldConnectionFailedPanel", "FieldConnectionFailedPanelType",
            "FieldErrorVersion", "FieldErrorVersionType", "ZNetOnNewConnection", "FejdStartupShowConnectError",
        })
        {
            int index = Array.FindIndex(lines, line => line.TrimStart().StartsWith($"public const string {member} ", StringComparison.Ordinal));
            Assert.True(index > 0, $"{member} is not declared.");
            Assert.StartsWith("/// <summary>", lines[index - 1].TrimStart(), StringComparison.Ordinal);
        }

        int ship = Array.FindIndex(lines, line => line.Contains("public static class Ship", StringComparison.Ordinal));
        int harmony = Array.FindIndex(lines, line => line.Contains("public static class HarmonyPatched", StringComparison.Ordinal));
        foreach (string owner in new[] { "ZNetPeer", "ZRpc", "ZPackage", "FejdStartup", "ConnectionStatus" })
        {
            int index = Array.FindIndex(lines, line => line.Contains($"public static class {owner}", StringComparison.Ordinal));
            Assert.True(index > ship && index < harmony, $"{owner} must be declared after Ship and before HarmonyPatched.");
        }
    }

    private static string RepoRootPath()
    {
        string dir = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(dir, "BaseStorage.sln")))
        {
            dir = Path.GetDirectoryName(dir) ?? throw new InvalidOperationException("BaseStorage.sln was not found above the test binaries.");
        }

        return dir;
    }
}
