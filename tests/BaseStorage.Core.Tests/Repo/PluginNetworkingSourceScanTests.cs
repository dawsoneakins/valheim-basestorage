namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;
using BaseStorage.Core.Tests.Ui;

/// <summary>
/// S-E006-002 source rules for src/BaseStorage.Plugin/Networking/ (contract version-rpc-contracts.md 5, rules N1-N8; ADR-GWIN-102):
/// the adapter is thin (no decision of its own), the two patches have the contracted shape, every RPC name and game member comes
/// from the Core constants and the shared target list, the vanilla error path is used exactly once each way, and no literal holds
/// strings-table text. Rows GW-U-1263..1266.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginNetworkingSourceScanTests
{
    private const string Folder = "src/BaseStorage.Plugin/Networking/";
    private const string Holder = Folder + "PluginVersionHandshake.cs";
    private const string ConnectionPatch = Folder + "ZNetOnNewConnectionPatch.cs";
    private const string ErrorPatch = Folder + "FejdStartupShowConnectErrorPatch.cs";

    // GW-U-1263 (N1): exactly the three files, in the contracted namespace; the holder is an internal static class with the one table.
    [Fact]
    public void TheFolderHoldsTheThreeContractFiles()
    {
        Dictionary<string, string> sources = Sources();

        Assert.Equal(new[] { ErrorPatch, Holder, ConnectionPatch }, FolderFiles(sources));
        foreach (string file in FolderFiles(sources))
        {
            Assert.Matches(@"(?m)^namespace BaseStorage\.Plugin\.Networking;", sources[file]);
        }

        string holder = sources[Holder];
        Assert.Matches(@"internal\s+static\s+class\s+PluginVersionHandshake\b", holder);
        Assert.Matches(@"internal\s+static\s+PeerVersionTable\?\s+Current\s*\{\s*get;\s*private\s+set;\s*\}", holder);
        Assert.Single(Regex.Matches(holder, @"new PeerVersionTable\(clock, ModVersion\.Current\)"));
    }

    // GW-U-1263 (N2, contract 4.2 wiring): Boot initialises the handshake after the reservations and before the patches; Update ticks it last.
    [Fact]
    public void BootAndUpdateWireTheHandshake()
    {
        string entry = Sources()[PluginSourceScan.EntryPoint];
        string boot = PluginSourceScan.MethodBody(entry, @"void\s+Boot\s*\(\s*\)");
        string update = Regex.Replace(PluginSourceScan.MethodBody(entry, @"void\s+Update\s*\(\s*\)"), @"\s+", string.Empty);

        Assert.Single(Regex.Matches(boot, Regex.Escape("PluginVersionHandshake.Initialize(new GameClock(), Settings, sink)")));
        int reservations = boot.IndexOf("PluginReservations.Initialize(", StringComparison.Ordinal);
        int handshake = boot.IndexOf("PluginVersionHandshake.Initialize(", StringComparison.Ordinal);
        int apply = boot.IndexOf("PatchApplier.ApplyAll(", StringComparison.Ordinal);
        Assert.True(reservations >= 0 && reservations < handshake && handshake < apply, "Boot order must be PluginReservations.Initialize, PluginVersionHandshake.Initialize, PatchApplier.ApplyAll.");
        Assert.EndsWith("PluginVersionHandshake.Tick();", update, StringComparison.Ordinal);
    }

    // GW-U-1264 (N3): the two patch files carry one attribute each, named by PatchTargetNames constants, and a one-statement postfix.
    [Fact]
    public void TheTwoPatchFilesHaveTheContractedShape()
    {
        Dictionary<string, string> sources = Sources();
        string[] patchFiles = FolderFiles(sources).Where(file => sources[file].Contains("[HarmonyPatch", StringComparison.Ordinal)).ToArray();
        Assert.Equal(new[] { ErrorPatch, ConnectionPatch }, patchFiles);

        string connection = sources[ConnectionPatch];
        Assert.Contains("[HarmonyPatch(typeof(ZNet), PatchTargetNames.ZNet.OnNewConnection)]", connection, StringComparison.Ordinal);
        Assert.Contains("internal const string PatchKey = \"BaseStorage.Plugin.Networking.ZNetOnNewConnectionPatch.Postfix\";", connection, StringComparison.Ordinal);
        Assert.Matches(@"private\s+static\s+void\s+Postfix\(ZNetPeer peer\)\s*=>\s*PatchSafety\.Run\(PatchKey,\s*peer,\s*static\s+p\s*=>\s*PluginVersionHandshake\.OnNewConnection\(p\)\);", connection);

        string error = sources[ErrorPatch];
        Assert.Contains("[HarmonyPatch(typeof(FejdStartup), PatchTargetNames.FejdStartup.ShowConnectError)]", error, StringComparison.Ordinal);
        Assert.Contains("internal const string PatchKey = \"BaseStorage.Plugin.Networking.FejdStartupShowConnectErrorPatch.Postfix\";", error, StringComparison.Ordinal);
        Assert.Matches(@"private\s+static\s+void\s+Postfix\(FejdStartup __instance\)\s*=>\s*PatchSafety\.Run\(PatchKey,\s*__instance,\s*static\s+f\s*=>\s*PluginVersionHandshake\.OnConnectErrorShown\(f\)\);", error);

        foreach (string file in patchFiles)
        {
            Assert.Single(Regex.Matches(sources[file], @"\[HarmonyPatch\("));
            Assert.DoesNotMatch(@"\[HarmonyPatch\([^\]]*""", sources[file]);
            Assert.DoesNotMatch(@"\b(Prefix|Transpiler|Finalizer|TargetMethods?)\b", sources[file]);
        }
    }

    // GW-U-1264 (N4): the per-frame tick and the RPC handler are wrapped; nothing in the folder waits or runs per frame on its own.
    [Fact]
    public void TheTickAndTheHandlerAreWrappedAndNothingWaits()
    {
        Dictionary<string, string> sources = Sources();
        string holder = sources[Holder];

        Assert.Contains("internal const string TickKey = \"BaseStorage.Plugin.Networking.PluginVersionHandshake.Tick\";", holder, StringComparison.Ordinal);
        Assert.Matches(@"PatchSafety\.Run\(TickKey,\s*static\s*\(\)\s*=>\s*TickCore\(\)\);", holder);
        Assert.Equal(2, Regex.Matches(holder, @"PatchSafety\.Run\(TickKey,").Count);
        foreach (string file in FolderFiles(sources))
        {
            foreach (string token in new[] { "Thread", "Task.", "Sleep(", "DateTime", "Stopwatch", "MonoBehaviour", "Time.time" })
            {
                Assert.False(sources[file].Contains(token, StringComparison.Ordinal), file + " contains " + token);
            }

            Assert.DoesNotMatch(@"\b(void|bool)\s+(Update|LateUpdate|FixedUpdate|OnGUI)\s*\(", sources[file]);
        }
    }

    // GW-U-1265 (N5, AC-9.4): RPC names come only from VersionRpc; the handler is registered once and the payload sent once, in the holder.
    [Fact]
    public void RpcNamesComeFromTheCoreConstantsAndTheRpcIsRegisteredAndSentOnce()
    {
        Dictionary<string, string> sources = Sources();
        string holder = sources[Holder];

        Assert.Single(Regex.Matches(holder, @"\.Register<ZPackage>\(VersionRpc\.Name,"));
        Assert.Single(Regex.Matches(holder, @"\.Invoke\(VersionRpc\.Name, payload\)"));
        Assert.Single(Regex.Matches(holder, @"\.Invoke\(VersionRpc\.VanillaErrorName, \(int\)ZNet\.ConnectionStatus\.ErrorVersion\)"));
        Assert.Contains("payload.Write(VersionHandshake.EncodePayloadBytes(ModVersion.Current));", holder, StringComparison.Ordinal);
        Assert.Contains("package.ReadByteArray()", holder, StringComparison.Ordinal);
        Assert.Equal(new[] { "src/BaseStorage.Plugin/BaseMenu/ChestHighlight.cs", Holder }, PluginSourceScan.FilesMatching(sources, @"\.Register<"));
        Assert.Equal(new[] { Holder }, PluginSourceScan.FilesMatching(sources, @"VersionRpc\."));
        foreach (string file in FolderFiles(sources))
        {
            Assert.DoesNotMatch("\"RPC_|\"Error\"|\"BaseStorage_", sources[file]);
            Assert.DoesNotContain("InvokeRPC(", sources[file], StringComparison.Ordinal);
            Assert.DoesNotContain("ZRoutedRpc", sources[file], StringComparison.Ordinal);
        }
    }

    // GW-U-1265 (N6): the vanilla error path, exactly once each way and only in the holder; no other Plugin file touches it.
    [Fact]
    public void TheVanillaErrorPathIsUsedOnceEachWayAndOnlyInTheHolder()
    {
        Dictionary<string, string> sources = Sources();
        string holder = sources[Holder];

        Assert.Single(Regex.Matches(holder, @"ZNet\.instance\.Disconnect\(peer\)"));
        Assert.Single(Regex.Matches(holder, @"ZNet\.SetExternalError\(ZNet\.ConnectionStatus\.ErrorVersion\)"));
        Assert.Equal(new[] { Holder }, PluginSourceScan.FilesMatching(sources, @"\.Disconnect\("));
        Assert.Equal(new[] { Holder }, PluginSourceScan.FilesMatching(sources, @"SetExternalError\("));
        Assert.Equal(new[] { Holder }, PluginSourceScan.FilesMatching(sources, @"m_connectionFailedError"));
        foreach (string file in FolderFiles(sources))
        {
            foreach (string token in new[] { "SendDisconnect", "Kick", "m_connectionStatus", "m_externalError", "Application.Quit", "Logout(" })
            {
                Assert.False(sources[file].Contains(token, StringComparison.Ordinal), file + " contains " + token);
            }
        }
    }

    // GW-U-1266 (N7): every decision is the table's: the holder switches only on PeerVersionAction and never compares versions itself.
    [Fact]
    public void TheHolderMakesNoDecisionOfItsOwn()
    {
        string holder = Sources()[Holder];

        Assert.DoesNotContain("VersionCheck.", holder, StringComparison.Ordinal);
        Assert.DoesNotContain("VersionCheckStatus", holder, StringComparison.Ordinal);
        Assert.DoesNotContain("DecodePayload", holder, StringComparison.Ordinal);
        Assert.DoesNotContain("IsAccepted", holder, StringComparison.Ordinal);
        Assert.DoesNotContain("TimeSpan", holder, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"==\s*ModVersion|ModVersion\.Current\s*==|string\.Equals\(", holder);
        Assert.Contains("table.Receive(id, bytes, RequireSameVersion())", holder, StringComparison.Ordinal);
        Assert.Contains("table.Tick(RequireSameVersion())", holder, StringComparison.Ordinal);
        Assert.Matches(@"private static bool RequireSameVersion\(\) => settings == null \|\| settings\.RequireSameVersion;", holder);
    }

    // GW-U-1266 (N8, U6): notices go through Notices.Show with the outcome key; the log line through LogPolicy at ConfigSync; no English literal.
    [Fact]
    public void NoticesAndLogLinesGoThroughTheContractedHelpersWithNoLiteralText()
    {
        Dictionary<string, string> sources = Sources();
        string holder = sources[Holder];

        Assert.Single(Regex.Matches(holder, @"Notices\.Show\(NoticePlacement\.Center, key, outcome\.Result\.HostVersion, outcome\.Result\.ClientVersion\)"));
        Assert.Single(Regex.Matches(holder, @"LogPolicy\.Write\(sink, LogCategory\.ConfigSync, line\)"));
        Assert.Contains("PeerVersionMessages.Format(outcome)", holder, StringComparison.Ordinal);
        Assert.Contains("mismatch.FormatMessage(StringTable.English)", holder, StringComparison.Ordinal);
        Assert.DoesNotContain("LogCategory.Detail", holder, StringComparison.Ordinal);
        Assert.DoesNotContain("$\"", holder, StringComparison.Ordinal);

        var offenders = new List<string>();
        foreach (string file in FolderFiles(sources))
        {
            foreach (Match literal in Regex.Matches(sources[file], "\"(?:[^\"\\\\]|\\\\.)*\""))
            {
                string inner = literal.Value.Substring(1, literal.Value.Length - 2);
                foreach ((string _, string english) in UiFixtures.EnglishTable)
                {
                    if (inner == english || (english.Length >= 8 && inner.Contains(english, StringComparison.Ordinal)))
                    {
                        offenders.Add(file + ": " + literal.Value);
                    }
                }

                if (inner.Contains("version", StringComparison.OrdinalIgnoreCase) && !inner.StartsWith("BaseStorage.Plugin.Networking.", StringComparison.Ordinal))
                {
                    offenders.Add(file + ": " + literal.Value);
                }
            }
        }

        Assert.Empty(offenders);
    }

    private static Dictionary<string, string> Sources()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        foreach (string file in new[] { Holder, ConnectionPatch, ErrorPatch })
        {
            Assert.True(sources.ContainsKey(file), "Scan did not read " + file);
        }

        return sources;
    }

    private static string[] FolderFiles(Dictionary<string, string> sources) =>
        sources.Keys.Where(key => key.StartsWith(Folder, StringComparison.Ordinal)).OrderBy(key => key, StringComparer.Ordinal).ToArray();
}
