namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-1, AC-2, AC-4, AC-5, AC-6 and AC-8 (contract config-sync-contracts.md 6, N1-N12; ADR-GWIN-013, ADR-GWIN-121): text scans of the
/// PluginConfigSync holder and of its three additive hooks. The Plugin is never loaded by a test. Each scan fails while the holder is absent.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginConfigSyncSourceScanTests
{
    private const string HolderPath = "src/BaseStorage.Plugin/HostSync/PluginConfigSync.cs";
    private const string HandshakePath = "src/BaseStorage.Plugin/Networking/PluginVersionHandshake.cs";
    private const string FirstRunPath = "src/BaseStorage.Plugin/Categories/FirstRunCategoryFile.cs";

    // GW-U-1414 (S5, N3(a), N6): the RPC is registered once, in the handshake, after Peers[id] = peer and before the version Invoke; invoked once, in the holder.
    [Fact]
    public void TheConfigRpcIsRegisteredInTheHandshakeAndInvokedOnlyInTheSendBranch()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        Assert.Equal(new[] { HandshakePath }, PluginSourceScan.FilesMatching(sources, Regex.Escape("Register<ZPackage>(ConfigSyncRpc.Name,")));

        string handshake = sources[HandshakePath];
        int peers = handshake.IndexOf("Peers[id] = peer;", StringComparison.Ordinal);
        int register = handshake.IndexOf("Register<ZPackage>(ConfigSyncRpc.Name,", StringComparison.Ordinal);
        int version = handshake.IndexOf("peer.m_rpc.Invoke(VersionRpc.Name,", StringComparison.Ordinal);
        Assert.True(peers >= 0 && register > peers && version > register, "The config RPC must be registered after Peers[id] = peer; and before the version Invoke.");

        Assert.Equal(new[] { HolderPath }, PluginSourceScan.FilesMatching(sources, Regex.Escape("Invoke(ConfigSyncRpc.Name,")));

        string holder = Holder();
        Assert.DoesNotMatch(@"\[HarmonyPatch", holder);
        Assert.DoesNotMatch(@"\bvoid\s+(Update|LateUpdate|FixedUpdate)\s*\(", holder);
        Assert.DoesNotMatch(@"\bThread\b|\bTask\.|\bSleep\(|\bDateTime\b|\bStopwatch\b|\bTime\.time\b", holder);
        Assert.DoesNotContain("Invoke", PluginSourceScan.MethodBody(holder, @"static void Tick\(\)"));
    }

    // GW-U-1417 (N5): OnPayload, OnVersionOutcome and Tick are each one PatchSafety.Run with the contracted key, return at once without a session, and the read is in a try.
    [Fact]
    public void EachEntryPointIsOneFailClosedPatchSafetyRun()
    {
        string holder = Holder();
        Assert.Contains("internal const string TickKey = \"BaseStorage.Plugin.HostSync.PluginConfigSync.Tick\";", holder);
        Assert.Contains("internal const string ReceiveKey = \"BaseStorage.Plugin.HostSync.PluginConfigSync.Receive\";", holder);
        Assert.Contains("internal const string SendKey = \"BaseStorage.Plugin.HostSync.PluginConfigSync.Send\";", holder);

        string onPayload = PluginSourceScan.MethodBody(holder, @"static void OnPayload\(");
        string onOutcome = PluginSourceScan.MethodBody(holder, @"static void OnVersionOutcome\(");
        string tick = PluginSourceScan.MethodBody(holder, @"static void Tick\(\)");

        Assert.Single(Regex.Matches(onPayload, Regex.Escape("PatchSafety.Run(ReceiveKey,")));
        Assert.Single(Regex.Matches(onOutcome, Regex.Escape("PatchSafety.Run(SendKey,")));
        Assert.Single(Regex.Matches(tick, Regex.Escape("PatchSafety.Run(TickKey,")));

        Assert.True(Regex.IsMatch(onPayload, @"Current\s*(==\s*null|is\s+null)"), "OnPayload does not return at once when there is no session.");
        Assert.True(Regex.IsMatch(onOutcome, @"Current\s*(==\s*null|is\s+null)"), "OnVersionOutcome does not return at once when there is no session.");
        Assert.True(Regex.IsMatch(tick, @"Current\s*(==\s*null|is\s+null)"), "Tick does not return at once when there is no session.");

        Assert.True(Regex.IsMatch(onPayload, @"try\s*\{[^}]*ReadByteArray\(\)[^}]*\}\s*catch", RegexOptions.Singleline), "ReadByteArray is not inside a try.");
    }

    // GW-U-1421 (N7, S6): the holder writes the one guarded line, the Warning goes through a try, the per-setting loop is guarded, and no Info literal appears.
    [Fact]
    public void TheHolderRoutesTheLogLinesThroughOneGuardedWrite()
    {
        string holder = Holder();
        Assert.Single(Regex.Matches(holder, Regex.Escape("LogPolicy.Write(sink, ConfigSyncMessages.CategoryOf(")));
        Assert.Contains("LogPolicy.IsEnabled(sink, ConfigSyncMessages.CategoryOf(", holder);
        Assert.True(Regex.IsMatch(holder, @"try\s*\{[^}]*sink\.Log\(LogSeverity\.Warning, line\)[^}]*\}\s*catch", RegexOptions.Singleline), "The Warning is not written through sink.Log in a try.");
        Assert.Contains("LogPolicy.IsEnabled(sink, LogCategory.Detail)", holder);
        Assert.DoesNotContain("LogSeverity.Info", holder);
        Assert.DoesNotContain("LogCategory.ConfigSync", holder);
    }

    // GW-U-1424 (N1, N2, N3(b)(c), N6, N11): the boot edits, the handshake hooks, the send path, the tick and the catalog read path.
    [Fact]
    public void ThePluginWiringIsTheContractedAdditiveEdits()
    {
        string holder = Holder();
        Assert.Contains("internal static class PluginConfigSync", holder);
        Assert.Contains("internal static ConfigSyncSession? Current { get; private set; }", holder);
        Assert.Contains("internal static CategoryCatalog Categories => Current?.Categories ?? CategoryCatalog.Empty;", holder);
        string send = PluginSourceScan.MethodBody(holder, @"static void OnVersionOutcome\(");
        int build = send.IndexOf("BuildHostPayload(ReadCategoryText())", StringComparison.Ordinal);
        int write = send.IndexOf("package.Write(bytes)", StringComparison.Ordinal);
        int invoke = send.IndexOf("Invoke(ConfigSyncRpc.Name, package)", StringComparison.Ordinal);
        Assert.True(build >= 0 && write > build && invoke > write, "The send path is not BuildHostPayload, then package.Write, then Invoke.");

        Dictionary<string, string> sources = PluginSourceScan.Sources();
        string entry = sources[PluginSourceScan.EntryPoint];
        Assert.Contains("Settings = new BaseStorageSettings(PluginConfigSync.Initialize(settingsSource, sink));", entry);
        Assert.Single(Regex.Matches(entry, Regex.Escape("new BepInExSettingsSource(Config)")));
        Assert.Single(Regex.Matches(entry, Regex.Escape("Settings = new BaseStorageSettings(")));
        int bepIn = entry.IndexOf("new BepInExSettingsSource(Config)", StringComparison.Ordinal);
        int settings = entry.IndexOf("Settings = new BaseStorageSettings(", StringComparison.Ordinal);
        Assert.True(bepIn >= 0 && settings > bepIn, "The BepInEx source must be created before the settings view.");

        int firstRun = entry.IndexOf("firstRunCategoryFile = new FirstRunCategoryFile(", StringComparison.Ordinal);
        int useFolder = entry.IndexOf("PluginConfigSync.UseCategoryFolder(firstRunCategoryFile.ConfigFolder);", StringComparison.Ordinal);
        int loaded = entry.IndexOf("BootMessages.Loaded", StringComparison.Ordinal);
        Assert.True(firstRun >= 0 && useFolder > firstRun && loaded > useFolder, "UseCategoryFolder must follow the FirstRunCategoryFile line and precede BootMessages.Loaded.");
        // Edited by integration (S-E006-004 + S-E004-004): S-E004-004 (ADR-GWIN-107) reads Paths.ConfigPath a second time in Boot for the
        // runtime catalog (pinned at two by GW-U-468); the config-sync folder still comes only from FirstRunCategoryFile.ConfigFolder.
        Assert.Equal(2, Regex.Matches(entry, Regex.Escape("Paths.ConfigPath")).Count);
        Assert.Single(Regex.Matches(entry, Regex.Escape("PluginConfigSync.UseCategoryFolder(")));
        Assert.Contains("internal string ConfigFolder => configFolder;", sources[FirstRunPath]);

        string handshake = sources[HandshakePath];
        int tryGet = handshake.IndexOf("Peers.TryGetValue(outcome.PeerId, out ZNetPeer? peer);", StringComparison.Ordinal);
        int hook = handshake.IndexOf("PluginConfigSync.OnVersionOutcome(outcome, peer);", StringComparison.Ordinal);
        int switchAt = handshake.IndexOf("switch (outcome.Action)", StringComparison.Ordinal);
        Assert.True(tryGet >= 0 && hook > tryGet && switchAt > hook, "The OnVersionOutcome hook must follow TryGetValue and precede the switch.");

        Assert.StartsWith("PluginConfigSync.Tick();", PluginSourceScan.MethodBody(handshake, @"static void TickCore\(\)").TrimStart());
    }

    // GW-U-1425 (AC-2, N10): the holder has no write path, reads files only with File.ReadAllText, and BepInExSettingsSource is built only in BaseStoragePlugin.cs.
    [Fact]
    public void TheHolderHasNoWritePath()
    {
        string holder = Holder();
        string[] tokens =
        {
            @"\bConfig\b",
            @"\bConfigEntry\b",
            @"\bBepInExSettingsSource\b",
            @"\.Value\s*=(?!=)",
            @"File\.Write",
            @"File\.Create",
            @"File\.Delete",
            @"StreamWriter",
            @"\.Save\(",
            @"WriteIfMissing",
        };
        foreach (string token in tokens)
        {
            Assert.False(Regex.IsMatch(holder, token), "PluginConfigSync holds the write token " + token + ".");
        }

        Assert.Empty(Regex.Matches(holder, @"\bFile\.(?!ReadAllText\b)\w+"));
        Assert.Single(Regex.Matches(holder, Regex.Escape("File.ReadAllText(")));
        Assert.Equal(new[] { PluginSourceScan.EntryPoint }, PluginSourceScan.FilesMatching(PluginSourceScan.Sources(), Regex.Escape("new BepInExSettingsSource(")));
    }

    // GW-U-1426 (N4, N8, N9): Mode is the one IsServer read with the contracted ternary; the tick calls Tick(Mode()), writes Cleared and reloads; ReadCategoryText has two call sites.
    [Fact]
    public void TheModeTickAndCategoryTextFollowTheContract()
    {
        string holder = Holder();
        string mode = PluginSourceScan.MethodBody(holder, @"static ConfigSyncMode Mode\(\)");
        Assert.True(
            Regex.IsMatch(Squash(mode), @"return ZNet\.instance == null \? Menu : ZNet\.instance\.IsServer\(\) \? LocalWorld : Client;"),
            "Mode() does not have the contracted ternary.");
        Assert.Single(Regex.Matches(holder, Regex.Escape("IsServer(")));

        string tick = PluginSourceScan.MethodBody(holder, @"static void Tick\(\)");
        Assert.Contains("Current.Tick(Mode())", tick);
        Assert.Contains("ConfigSyncMessages.Cleared()", tick);
        Assert.Contains("LoadLocalCategories(ReadCategoryText())", tick);

        string read = PluginSourceScan.MethodBody(holder, @"static string ReadCategoryText\(\)");
        Assert.Contains("DefaultCategoryFileWriter.PathFor(", read);
        Assert.Contains("ConfigSyncMessages.CategoryFileUnavailable(", read);
        Assert.True(Regex.IsMatch(read, @"return\s+"""";"), "ReadCategoryText does not return the empty text.");
        Assert.Equal(3, Count(holder, @"\bReadCategoryText\(\)"));
    }

    // GW-U-1427 (N12): the holder decides nothing and holds no literal of the other layers; the handshake still has its two TickKey calls.
    [Fact]
    public void TheHolderDecidesNothingAndNamesNoLiteral()
    {
        string holder = Holder();
        string[] forbidden =
        {
            "HostSyncCodec.Decode",
            "CategoryFile.Parse",
            "TryApplyHost",
            "ClearHost",
            "PeerVersionAction",
            "RemoteIsServer",
            ".Result",
            "$\"",
            "\"Config sync",
            "\"BaseStorage_",
        };
        foreach (string token in forbidden)
        {
            Assert.DoesNotContain(token, holder);
        }

        Assert.Equal(2, Count(Handshake(), Regex.Escape("PatchSafety.Run(TickKey,")));
    }

    /// <summary>The holder file, read CRLF-normalised with comments removed. Fails while the file is absent.</summary>
    private static string Holder() => PluginSourceScan.StripComments(RepoRoot.ReadText(HolderPath));

    /// <summary>The handshake file, read with comments removed.</summary>
    private static string Handshake() => PluginSourceScan.StripComments(RepoRoot.ReadText(HandshakePath));

    /// <summary>The number of matches of a pattern.</summary>
    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    /// <summary>Collapses every run of whitespace to one space, so a multi-line expression can be matched on one line.</summary>
    private static string Squash(string text) => Regex.Replace(text, @"\s+", " ").Trim();
}
