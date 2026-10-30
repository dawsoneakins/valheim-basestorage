namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

/// <summary>
/// GW-U-1806 to GW-U-1810 and GW-U-1816 (AC-1 to AC-4; contract craft-from-base-toggle 1, 2, 3.2, 3.3, 4 and 5): source scans of
/// the gate file, the nine E3 patch files, the in-flight probes, the Boot statement and the three game-bound gates.
/// Files are CRLF-normalised and comment-stripped (ADR-GWIN-124).
/// </summary>
public sealed class PluginCraftFromBaseToggleSourceScanTests
{
    private const string PluginPrefix = "src/BaseStorage.Plugin/";
    private const string GateFile = "src/BaseStorage.Plugin/Patching/CraftFromBaseGate.cs";
    private const string CraftingProbeFile = "src/BaseStorage.Plugin/Crafting/PluginCrafting.cs";
    private const string BuildingProbeFile = "src/BaseStorage.Plugin/Building/PluginBuilding.cs";
    private const string InFlightLine = "internal static bool InFlight => Current != null && Current.Phase != CraftPhase.Idle;";
    private const string BootStatement = "CraftFromBaseGate.Initialize(static () => Settings != null && Settings.CraftFromBase, static () => PluginCrafting.InFlight || PluginBuilding.InFlight);";
    private const string BoolWrapper = @"static bool Prefix\([^)]*\)\s*=>\s*!CraftFromBaseGate\.IsOpen\(\)\s*\|\|\s*PrefixCore\(";
    private const string VoidWrapper = @"static void (Prefix|Postfix)\([^)]*\)\s*\{\s*if \(CraftFromBaseGate\.IsOpen\(\)\)\s*\{\s*\1Core\(";

    // Per file: the number of bool-prefix wrappers (contract 4.1, 4.2) and void wrappers (4.3, 4.4) of the 9 E3 files of 2.3.
    private static readonly Dictionary<string, (int BoolWrappers, int VoidWrappers)> E3Files = new(StringComparer.Ordinal)
    {
        ["Building/PlayerHaveRequirementsPiecePatch.cs"] = (1, 0),
        ["Building/PlayerPlacePiecePatch.cs"] = (1, 0),
        ["Building/PlayerTryPlacePiecePatch.cs"] = (1, 1),
        ["Crafting/InventoryGuiDoCraftingPatch.cs"] = (1, 0),
        ["Crafting/PlayerConsumeResourcesPatch.cs"] = (1, 0),
        ["Crafting/PlayerHaveRequirementItemsPatch.cs"] = (1, 0),
        ["Requirements/HudSetupPieceInfoPatch.cs"] = (0, 2),
        ["Requirements/InventoryGuiSetupRequirementListPatch.cs"] = (0, 2),
        ["Requirements/InventoryGuiSetupRequirementPatch.cs"] = (0, 1),
    };

    // GW-U-1806 (AC-2, G6): the gate file has the contracted shape, and the Core.Tests project links it directly after PatchSafety.cs.
    [Fact]
    public void TheGateFileHasTheContractedShapeAndIsLinkedAfterPatchSafety()
    {
        string raw = RepoRoot.ReadText(GateFile);
        string code = PluginSourceScan.StripComments(raw);

        Assert.Matches(@"(?m)^namespace BaseStorage\.Plugin\.Patching;$", code);
        Assert.Equal(new[] { "using System;", "using System.Threading;" }, Regex.Matches(code, @"(?m)^using [^;]+;$").Select(match => match.Value).ToArray());
        Assert.Matches(@"(?m)^internal static class CraftFromBaseGate$", code);
        foreach (string token in new[] { "BaseStoragePlugin", "Unity", "Valheim", "BepInEx", "Harmony" })
        {
            Assert.DoesNotContain(token, code, StringComparison.Ordinal);
        }

        string[] lines = raw.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (!Regex.IsMatch(lines[i], @"^\s*internal "))
            {
                continue;
            }

            int previous = i - 1;
            while (previous >= 0 && string.IsNullOrWhiteSpace(lines[previous]))
            {
                previous--;
            }

            Assert.True(previous >= 0 && lines[previous].TrimStart().StartsWith("///", StringComparison.Ordinal), "CraftFromBaseGate.cs line " + (i + 1) + " has no XML doc (CS1591).");
        }

        XDocument project = RepoRoot.LoadXml(ContractPaths.CoreTestsProject);
        List<XElement> compiles = project.Descendants().Where(element => element.Name.LocalName == "Compile").ToList();
        int patchSafety = compiles.FindIndex(element => (element.Attribute("Link")?.Value ?? string.Empty) == "Linked\\PatchSafety.cs");
        Assert.True(patchSafety >= 0 && patchSafety + 1 < compiles.Count, "PatchSafety.cs is not linked into Core.Tests.");
        Assert.Equal("..\\..\\src\\BaseStorage.Plugin\\Patching\\CraftFromBaseGate.cs", compiles[patchSafety + 1].Attribute("Include")?.Value);
        Assert.Equal("Linked\\CraftFromBaseGate.cs", compiles[patchSafety + 1].Attribute("Link")?.Value);
    }

    // GW-U-1807 (AC-2, D2, D4): [CraftFromBasePatch] follows [HarmonyPatch( in the nine files and in every HarmonyPatch file of the
    // three E3 folders, and no other file under src/ carries it.
    [Fact]
    public void TheMarkerFollowsHarmonyPatchInTheNineFilesAndNowhereElseInSrc()
    {
        Dictionary<string, string> plugin = PluginSourceScan.Sources();
        string[] nine = NineFiles();

        foreach (string file in nine)
        {
            Assert.Matches(@"\[HarmonyPatch\(.*\)\]\n\[CraftFromBasePatch\]\n", plugin[file]);
        }

        string[] harmonyInE3Folders = PluginSourceScan.FilesMatching(plugin, @"\[HarmonyPatch")
            .Where(file => file.StartsWith(PluginPrefix + "Building/", StringComparison.Ordinal)
                || file.StartsWith(PluginPrefix + "Crafting/", StringComparison.Ordinal)
                || file.StartsWith(PluginPrefix + "Requirements/", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(nine, harmonyInE3Folders);
        Assert.Equal(nine, PluginSourceScan.FilesMatching(SrcSources(), @"\[CraftFromBasePatch\]"));
    }

    // GW-U-1808 (AC-2, section 4): each patch method matches its shape, and CraftFromBaseGate.IsOpen() occurs exactly 12 times in src/.
    [Fact]
    public void EachPatchMethodMatchesItsShapeAndTheGateIsCalledTwelveTimesInSrc()
    {
        Dictionary<string, string> plugin = PluginSourceScan.Sources();
        foreach (KeyValuePair<string, (int BoolWrappers, int VoidWrappers)> pair in E3Files)
        {
            string text = plugin[PluginPrefix + pair.Key];
            Assert.Equal(pair.Value.BoolWrappers, Count(text, BoolWrapper));
            Assert.Equal(pair.Value.VoidWrappers, Count(text, VoidWrapper));
        }

        Dictionary<string, string> src = SrcSources();
        Assert.Equal(12, PluginSourceScan.CountMatches(src, @"CraftFromBaseGate\.IsOpen\(\)"));
        Assert.Equal(NineFiles(), PluginSourceScan.FilesMatching(src, @"CraftFromBaseGate\.IsOpen\(\)"));
    }

    // GW-U-1809 (AC-1, T6, section 3.2): PluginCrafting.cs and PluginBuilding.cs each contain the exact InFlight line once.
    [Fact]
    public void EachInFlightProbeIsTheExactLineOnce()
    {
        foreach (string file in new[] { CraftingProbeFile, BuildingProbeFile })
        {
            string code = PluginSourceScan.StripComments(RepoRoot.ReadText(file));
            Assert.Equal(1, Count(code, Regex.Escape(InFlightLine)));
        }
    }

    // GW-U-1810 (AC-3, AC-4, section 3.3): the Initialize statement is in Boot once, after PluginBuilding.Initialize( and before
    // PluginVersionHandshake.Initialize( and new Harmony(; it is the only CraftFromBaseGate.Initialize( in src/.
    [Fact]
    public void TheBootStatementSitsBetweenBuildingAndVersionHandshakeOnce()
    {
        string entry = PluginSourceScan.StripComments(RepoRoot.ReadText(PluginSourceScan.EntryPoint));
        string boot = PluginSourceScan.MethodBody(entry, @"private void Boot\(\)");

        Assert.Equal(1, Count(entry, Regex.Escape(BootStatement)));
        Assert.Equal(1, PluginSourceScan.CountMatches(SrcSources(), @"CraftFromBaseGate\.Initialize\("));

        int building = boot.IndexOf("PluginBuilding.Initialize(", StringComparison.Ordinal);
        int gate = boot.IndexOf(BootStatement, StringComparison.Ordinal);
        int handshake = boot.IndexOf("PluginVersionHandshake.Initialize(", StringComparison.Ordinal);
        int harmony = boot.IndexOf("new Harmony(", StringComparison.Ordinal);
        Assert.True(building >= 0 && gate > building, "The statement must come after PluginBuilding.Initialize(.");
        Assert.True(handshake > gate, "The statement must come before PluginVersionHandshake.Initialize(.");
        Assert.True(harmony > gate, "The statement must come before new Harmony(.");
    }

    // GW-U-1810 (AC-4, L4): the Boot statement passes two static delegates that are read on every call, not sampled values.
    [Fact]
    public void TheBootStatementPassesReadsNotSampledValues()
    {
        string entry = PluginSourceScan.StripComments(RepoRoot.ReadText(PluginSourceScan.EntryPoint));

        Assert.Matches(@"CraftFromBaseGate\.Initialize\(static \(\) => Settings != null && Settings\.CraftFromBase, static \(\) => PluginCrafting\.InFlight \|\| PluginBuilding\.InFlight\);", entry);
        Assert.DoesNotMatch(@"CraftFromBaseGate\.Initialize\(\s*(true|false|CraftFromBase\b)", entry);
    }

    // GW-U-1816 (AC-1, section 5): the inner SettingOff check is the first if of each game-bound gate (green on arrival, ADR-GWIN-135).
    [Fact]
    public void TheInnerSettingOffGateIsTheFirstIfOfEachGameBoundGate()
    {
        AssertInnerGateIsFirst("src/BaseStorage.Plugin/Crafting/GameCraftGame.cs", @"internal CraftGateReason EvaluateGate\(");
        AssertInnerGateIsFirst("src/BaseStorage.Plugin/Building/GameBuildGame.cs", @"internal BuildGateReason EvaluateGate\(");
        AssertInnerGateIsFirst("src/BaseStorage.Plugin/Requirements/GameRequirementDisplayWorld.cs", @"private BaseSnapshot\? BuildTooltipSnapshot\(\)");
    }

    private static void AssertInnerGateIsFirst(string file, string signature)
    {
        string code = PluginSourceScan.StripComments(RepoRoot.ReadText(file));
        string body = PluginSourceScan.MethodBody(code, signature);
        int firstIf = body.IndexOf("if (", StringComparison.Ordinal);
        Assert.True(firstIf > 0, file + ": no if precedes the setting check.");

        string[] before = body.Substring(0, firstIf)
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToArray();
        Assert.All(before, statement => Assert.Matches(@"^(\w+ = null;|BaseStorageSettings\? settings = BaseStoragePlugin\.Settings;)$", statement));

        string ifLine = body.Substring(firstIf).Split('\n')[0].Trim();
        Assert.Equal("if (settings == null || !settings.CraftFromBase)", ifLine);
    }

    private static string[] NineFiles() =>
        E3Files.Keys.Select(name => PluginPrefix + name).OrderBy(key => key, StringComparer.Ordinal).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static Dictionary<string, string> SrcSources()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(RepoRoot.Combine("src"), "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(RepoRoot.FullPath, file).Replace('\\', '/');
            if (!RepoRoot.IsExcludedPath(relative))
            {
                result[relative] = PluginSourceScan.StripComments(RepoRoot.ReadText(relative));
            }
        }

        return result;
    }
}
