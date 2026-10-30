namespace BaseStorage.Core.Tests.Repo;

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

/// <summary>
/// GW-U-1510 and GW-U-1525 to GW-U-1536 (AC-4 to AC-6; contract 1, 3.4 and 5.6 rows RD1-RD11): source scans of the requirement display,
/// the Core formatter folder and the Plugin Requirements folder. Files are CRLF-normalised and comment-stripped (ADR-GWIN-124, ADR-GWIN-127).
/// </summary>
public sealed class PluginRequirementDisplaySourceScanTests
{
    private const string PluginFolder = "src/BaseStorage.Plugin/Requirements";
    private const string CoreUiFolder = "src/BaseStorage.Core/Ui";
    private const string CoreTestsProject = "tests/BaseStorage.Core.Tests/BaseStorage.Core.Tests.csproj";
    private const string SessionFile = "RequirementDisplaySession.cs";
    private const string WorldFile = "GameRequirementDisplayWorld.cs";
    private const string GameBoundFile = "PluginRequirementDisplay.cs";

    private static readonly string[] GameFree = { "RequirementDisplayContext.cs", "IRequirementDisplayWorld.cs", "RequirementDisplaySession.cs" };
    private static readonly string[] GameBound = { WorldFile, GameBoundFile };
    private static readonly string[] PatchFiles = { "HudSetupPieceInfoPatch.cs", "InventoryGuiSetupRequirementListPatch.cs", "InventoryGuiSetupRequirementPatch.cs" };

    // GW-U-1525 (RD1, first clause): the folder holds exactly the 8 contracted files.
    [Fact]
    public void TheRequirementFolderHoldsExactlyTheEightContractedFiles()
    {
        string[] expected = GameFree.Concat(GameBound).Concat(PatchFiles).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        string[] actual = Directory.EnumerateFiles(RepoRoot.Combine(PluginFolder), "*.cs")
            .Select(path => Path.GetFileName(path))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    // GW-U-1510 (CS1, 3.4): the Core requirement display has no game, rendering or string-table token, and no rich-text literal.
    [Fact]
    public void TheCoreDisplayFilesHaveNoGameOrRenderingToken()
    {
        string all = string.Join("\n", CoreSources().Values);

        foreach (string pattern in new[] { @"\bUnityEngine\b", @"\bValheim", @"\bBepInEx\b", @"\bHarmonyLib\b", @"\bTMPro\b", @"\bColor\b", @"\bMathf\b", @"<color", @"<size", @"<b>", @"\bStringTable\b", @"\bStringKeys\b" })
        {
            Assert.DoesNotMatch(pattern, all);
        }
    }

    // GW-U-1510 (CS2, CS3, 3.4): invariant culture only, no string.Format or interpolation, and no static field other than const.
    [Fact]
    public void TheCoreDisplayFormatsInvariantlyAndHoldsNoStaticState()
    {
        string all = string.Join("\n", CoreSources().Values);

        Assert.Contains("CultureInfo.InvariantCulture", all);
        Assert.DoesNotMatch(@"CultureInfo\.CurrentCulture", all);
        Assert.DoesNotMatch(@"string\.Format\(", all);
        Assert.DoesNotMatch(@"\$""", all);
        Assert.DoesNotMatch(@"(?m)^[^(\n]*\bstatic\b(?![^(\n]*\bconst\b)[^(\n]*[;=]", all);
    }

    // GW-U-1525 (RD1, second clause): exactly the 3 patch files carry HarmonyPatch, each with the contracted target and key, and each body is one PatchSafety.Run call.
    [Fact]
    public void ExactlyTheThreePatchFilesCarryHarmonyPatchWithTheContractedShape()
    {
        Dictionary<string, string> sources = FolderSources();

        Assert.Equal(PatchFiles, PluginSourceScan.FilesMatching(sources, @"\[HarmonyPatch"));

        string list = sources["InventoryGuiSetupRequirementListPatch.cs"];
        Assert.Contains("[HarmonyPatch(typeof(InventoryGui), PatchTargetNames.InventoryGui.SetupRequirementList)]", list, StringComparison.Ordinal);
        Assert.Matches(KeyConstant("PrefixKey", "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementListPatch.Prefix"), list);
        Assert.Matches(KeyConstant("PostfixKey", "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementListPatch.Postfix"), list);
        // Edited by S-E003-008, craft-from-base-toggle 8.3: the void wrappers check CraftFromBaseGate first; the PatchSafety.Run call moves to PrefixCore / PostfixCore.
        Assert.Matches(@"void\s+Prefix\s*\([^)]*\)\s*\{\s*if \(CraftFromBaseGate\.IsOpen\(\)\)\s*\{\s*PrefixCore\(", list);
        Assert.Matches(@"void\s+PrefixCore\s*\([^)]*\)\s*=>\s*PatchSafety\.Run\(PrefixKey,", list);
        Assert.Matches(@"void\s+Postfix\s*\([^)]*\)\s*\{\s*if \(CraftFromBaseGate\.IsOpen\(\)\)\s*\{\s*PostfixCore\(", list);
        Assert.Matches(@"void\s+PostfixCore\s*\([^)]*\)\s*=>\s*PatchSafety\.Run\(PostfixKey,", list);

        string hud = sources["HudSetupPieceInfoPatch.cs"];
        Assert.Contains("[HarmonyPatch(typeof(Hud), PatchTargetNames.Hud.SetupPieceInfo)]", hud, StringComparison.Ordinal);
        Assert.Matches(KeyConstant("PrefixKey", "BaseStorage.Plugin.Requirements.HudSetupPieceInfoPatch.Prefix"), hud);
        Assert.Matches(KeyConstant("PostfixKey", "BaseStorage.Plugin.Requirements.HudSetupPieceInfoPatch.Postfix"), hud);
        // Edited by S-E003-008, craft-from-base-toggle 8.3: the void wrappers check CraftFromBaseGate first; the PatchSafety.Run call moves to PrefixCore / PostfixCore.
        Assert.Matches(@"void\s+Prefix\s*\([^)]*\)\s*\{\s*if \(CraftFromBaseGate\.IsOpen\(\)\)\s*\{\s*PrefixCore\(", hud);
        Assert.Matches(@"void\s+PrefixCore\s*\([^)]*\)\s*=>\s*PatchSafety\.Run\(PrefixKey,", hud);
        Assert.Matches(@"void\s+Postfix\s*\([^)]*\)\s*\{\s*if \(CraftFromBaseGate\.IsOpen\(\)\)\s*\{\s*PostfixCore\(", hud);
        Assert.Matches(@"void\s+PostfixCore\s*\([^)]*\)\s*=>\s*PatchSafety\.Run\(PostfixKey,", hud);

        string setup = sources["InventoryGuiSetupRequirementPatch.cs"];
        Assert.Contains("[HarmonyPatch(typeof(InventoryGui), PatchTargetNames.InventoryGui.SetupRequirement)]", setup, StringComparison.Ordinal);
        Assert.Matches(KeyConstant("PatchKey", "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementPatch.Postfix"), setup);
        // Edited by S-E003-008, craft-from-base-toggle 8.3: the void wrapper checks CraftFromBaseGate first; the PatchSafety.Run call moves to PostfixCore.
        Assert.Matches(@"void\s+Postfix\s*\([^)]*\)\s*\{\s*if \(CraftFromBaseGate\.IsOpen\(\)\)\s*\{\s*PostfixCore\(", setup);
        Assert.Matches(@"void\s+PostfixCore\s*\([^)]*\)\s*=>\s*PatchSafety\.Run\(PatchKey,", setup);
    }

    // GW-U-1525 (RD1, third clause): no file uses a run-prefix, finalizer, transpiler or bool prefix.
    [Fact]
    public void NoFileUsesARunPrefixFinalizerTranspilerOrBoolPrefix()
    {
        Dictionary<string, string> sources = FolderSources();

        foreach (string pattern in new[] { "RunPrefix", "Finalizer", "Transpiler", @"bool\s+Prefix" })
        {
            Assert.Empty(PluginSourceScan.FilesMatching(sources, pattern));
        }
    }

    // GW-U-1527 (AC-5, 5.4 step 8, CD9, ADR-GWIN-126): the postfix ends with the text write followed only by the colour write.
    [Fact]
    public void TheOnSetupRequirementBodyEndsWithTheTextThenTheColourWrite()
    {
        string source = FolderSources()[GameBoundFile];

        string body = PluginSourceScan.MethodBody(source, @"void\s+OnSetupRequirement\s*\(");
        int text = body.IndexOf(".text =", StringComparison.Ordinal);
        Assert.True(text >= 0, "OnSetupRequirement has no .text = write.");

        string afterText = body.Substring(text);
        string tail = afterText.Substring(afterText.IndexOf(';') + 1);
        Assert.Matches(@"^\s*\w+\.color\s*=[^;]*;\s*$", tail);
        Assert.DoesNotMatch(@"\breturn\b", tail);
    }

    // GW-U-1528 (RD6, CD1): the formatter and the availability are called only by the session, and the Plugin never counts or formats numbers.
    [Fact]
    public void CoreDecidesTheTextAndTheNumbers()
    {
        Dictionary<string, string> sources = FolderSources();

        Assert.Equal(new[] { SessionFile }, PluginSourceScan.FilesMatching(sources, Regex.Escape("RequirementDisplayFormatter.Format(")));
        Assert.Equal(new[] { SessionFile }, PluginSourceScan.FilesMatching(sources, Regex.Escape("BaseAvailability.Compute(")));
        Assert.Empty(PluginSourceScan.FilesMatching(sources, "CountItems"));
        Assert.Empty(PluginSourceScan.FilesMatching(sources, Regex.Escape("ToString(")));
        Assert.Equal(1, PluginSourceScan.CountMatches(sources, Regex.Escape(".text =")));
        Assert.Equal(new[] { GameBoundFile }, PluginSourceScan.FilesMatching(sources, Regex.Escape(".text =")));
    }

    // GW-U-1529 (RD7, CD7): the CraftFromBase setting, the gate, the snapshot and the base start are read only by the game world adapter.
    [Fact]
    public void OnlyTheGameWorldAdapterReadsTheGateAndTheBase()
    {
        Dictionary<string, string> sources = FolderSources();

        Assert.Equal(new[] { WorldFile }, PluginSourceScan.FilesMatching(sources, @"[Ss]ettings\.CraftFromBase"));
        Assert.Equal(new[] { WorldFile }, PluginSourceScan.FilesMatching(sources, Regex.Escape("EvaluateGate(")));
        Assert.Equal(new[] { WorldFile }, PluginSourceScan.FilesMatching(sources, Regex.Escape("GetSnapshot(")));
        Assert.Equal(new[] { WorldFile }, PluginSourceScan.FilesMatching(sources, Regex.Escape("BaseStart.FromPosition(")));
    }

    // GW-U-1530 (RD8, 5.1): Boot initializes the display after the crafting session and before the patches.
    [Fact]
    public void BootInitializesTheRequirementDisplayBetweenCraftingAndThePatches()
    {
        string entry = PluginSourceScan.Sources()[PluginSourceScan.EntryPoint];
        string boot = PluginSourceScan.MethodBody(entry, @"void\s+Boot\s*\(\s*\)");

        int crafting = boot.IndexOf("PluginCrafting.Initialize(", StringComparison.Ordinal);
        int display = boot.IndexOf("PluginRequirementDisplay.Initialize(", StringComparison.Ordinal);
        int apply = boot.IndexOf("PatchApplier.ApplyAll(", StringComparison.Ordinal);
        Assert.True(crafting >= 0 && crafting < display && display < apply, "Boot must call PluginRequirementDisplay.Initialize after PluginCrafting.Initialize and before PatchApplier.ApplyAll.");
    }

    // GW-U-1530 (RD8): the per-frame Update does not mention the display, since no Update tick is added.
    [Fact]
    public void UpdateDoesNotMentionTheRequirementDisplay()
    {
        string entry = PluginSourceScan.Sources()[PluginSourceScan.EntryPoint];

        string update = PluginSourceScan.MethodBody(entry, @"void\s+Update\s*\(\s*\)");

        Assert.DoesNotContain("PluginRequirementDisplay", update, StringComparison.Ordinal);
    }

    // GW-U-1531 (RD5, CD4, CD5): the vanilla colour expression appears once, in the game-bound file, and uses the short bit; no rich-text or style token appears.
    [Fact]
    public void TheVanillaColourRuleAppearsOnceAndNoStyleTokenAppears()
    {
        Dictionary<string, string> sources = FolderSources();

        Assert.Equal(new[] { GameBoundFile }, PluginSourceScan.FilesMatching(sources, Regex.Escape("Color.red")));
        Assert.Equal(new[] { GameBoundFile }, PluginSourceScan.FilesMatching(sources, Regex.Escape("Mathf.Sin(Time.time * 10f) > 0f")));
        Assert.Equal(new[] { GameBoundFile }, PluginSourceScan.FilesMatching(sources, Regex.Escape(".color =")));
        Assert.Equal(1, PluginSourceScan.CountMatches(sources, Regex.Escape("Color.red")));
        Assert.Equal(1, PluginSourceScan.CountMatches(sources, Regex.Escape("Mathf.Sin(Time.time * 10f) > 0f")));
        Assert.Equal(1, PluginSourceScan.CountMatches(sources, Regex.Escape(".color =")));
        Assert.Matches(@"\.color\s*=[^;]*IsShort[^;]*;", sources[GameBoundFile]);

        foreach (string token in new[] { "<color", "<size", "ColorUtility", "fontSize", "enableWordWrapping", "overflowMode", "alignment" })
        {
            Assert.Empty(PluginSourceScan.FilesMatching(sources, Regex.Escape(token)));
        }
    }

    // GW-U-1532 (RD2, CD2): no file of the display changes inventory, chests, ownership, RPC, notices or threading.
    [Fact]
    public void NoFileChangesGameStateOrRunsOffTheMainThread()
    {
        Dictionary<string, string> sources = FolderSources();

        string[] forbidden =
        {
            Regex.Escape(".RemoveItem("), Regex.Escape(".AddItem("), Regex.Escape(".Clone()"), "DropItem\\(", Regex.Escape(".Take("),
            Regex.Escape(".PutBack("), "ReturnAllTaken\\(", "ForgetTaken\\(", Regex.Escape(".Commit("), "DoCrafting", "ConsumeResources",
            "ReservationDriver", "PluginReservations", "PluginCrafting", "CraftSession", "SetOwner", "ClaimOwnership", "ForceSendZDO",
            Regex.Escape("SetInUse("), "InvokeRPC", Regex.Escape("Notices.Show("), "ShowNotice\\(", "ChestCategoryWriter\\.Write",
            Regex.Escape(".Save("), @"\bThread\b", Regex.Escape("Task."), "MonoBehaviour",
        };

        foreach (string pattern in forbidden)
        {
            Assert.Empty(PluginSourceScan.FilesMatching(sources, pattern));
        }

        Assert.Empty(PluginSourceScan.FilesMatching(sources, @"void\s+(Update|LateUpdate|FixedUpdate|OnGUI)\s*\("));
    }

    // GW-U-1532 (RD3, CD2): no file writes a log line.
    [Fact]
    public void NoFileWritesALogLine()
    {
        Dictionary<string, string> sources = FolderSources();

        foreach (string pattern in new[] { "LogPolicy", "ILogSink", Regex.Escape("Debug.Log"), @"\bLogger\b", "LogSink" })
        {
            Assert.Empty(PluginSourceScan.FilesMatching(sources, pattern));
        }
    }

    // GW-U-1533 (RD4, RD9, RD10, W1, W4): the game-free files import no game library and name no game type; the game-bound files use no null-conditional or is-null test; no string-table key is used.
    [Fact]
    public void TheGameFreeFilesImportNoGameLibrary()
    {
        Dictionary<string, string> sources = FolderSources();

        foreach (string file in GameFree)
        {
            Assert.DoesNotMatch(@"using\s+(UnityEngine|HarmonyLib|BepInEx|TMPro)\b", sources[file]);
        }
    }

    // GW-U-1533 (RD4): no whole-word game token outside comments and string literals in the game-free files.
    [Fact]
    public void TheGameFreeFilesNameNoGameTypeOutsideCommentsAndLiterals()
    {
        Dictionary<string, string> sources = FolderSources();

        foreach (string file in GameFree)
        {
            string code = Regex.Replace(sources[file], @"""(?:\\.|[^""\\\n])*""", "\"\"");
            Assert.DoesNotMatch(@"\b(InventoryGui|Hud|Piece|Player|Transform|TMP_Text|Color|Mathf|ItemDrop|ZDO|ZNet|ZoneSystem|MonoBehaviour)\b", code);
        }
    }

    // GW-U-1533 (RD9, RD10, W4): the game-bound files use no ?., ??, is null or is not null, and no file uses the string table.
    [Fact]
    public void TheGameBoundFilesUseNoNullConditionalAndNoStringKey()
    {
        Dictionary<string, string> sources = FolderSources();

        foreach (string file in GameBound)
        {
            Assert.DoesNotMatch(@"\?\.|\?\?|\bis\s+(not\s+)?null\b", sources[file]);
        }

        Assert.Empty(PluginSourceScan.FilesMatching(sources, @"StringKeys\."));
        Assert.Empty(PluginSourceScan.FilesMatching(sources, @"StringTable\."));
    }

    // GW-U-1534 (contract 1): the Core.Tests project links the three game-free files after the Crafting lines, in table order, with the Linked name.
    [Fact]
    public void CoreTestsLinksTheThreeRequirementFilesAfterTheCraftingLinesInTableOrder()
    {
        XDocument project = RepoRoot.LoadXml(CoreTestsProject);
        var compiles = project.Descendants()
            .Where(element => element.Name.LocalName == "Compile")
            .Select(element => (Include: element.Attribute("Include")?.Value ?? string.Empty, Link: element.Attribute("Link")?.Value ?? string.Empty))
            .ToArray();

        (string Include, string Link)[] expected = GameFree
            .Select(name => (@"..\..\src\BaseStorage.Plugin\Requirements\" + name, @"Linked\" + name))
            .ToArray();
        (string Include, string Link)[] actual = compiles
            .Where(c => c.Include.Contains("Requirements", StringComparison.Ordinal))
            .Select(c => (c.Include, c.Link))
            .ToArray();
        Assert.Equal(expected, actual);

        int lastCrafting = Array.FindLastIndex(compiles, c => c.Include.Contains("Crafting", StringComparison.Ordinal));
        int firstRequirement = Array.FindIndex(compiles, c => c.Include.Contains("Requirements", StringComparison.Ordinal));
        Assert.True(lastCrafting >= 0 && lastCrafting < firstRequirement, "The Requirements links must follow the Crafting links.");
    }

    // GW-U-1534 (contract 1, 1 section): the linked count is 27 after the three requirement links join the 24.
    // Edited by integration (S-E003-005 + S-E003-006): plus the six Building links of S-E003-006, thirty-three.
    // Edited by S-E004-015 (ADR-GWIN-143): renamed from TheLinkedItemCountIsThirtySeven; plus the two Deposit links, thirty-nine.
    [Fact]
    // Edited by S-E003-008, craft-from-base-toggle 8.3: the gate link (CraftFromBaseGate.cs, after PatchSafety.cs) joins the 33, so thirty-four.
    // Edited by integration (S-E003-008 + S-E004-015): the union of both sides, forty.
    public void TheLinkedItemCountIsForty()
    {
        XDocument project = RepoRoot.LoadXml(CoreTestsProject);
        int linked = project.Descendants()
            .Count(element => element.Name.LocalName == "Compile" && (element.Attribute("Link")?.Value ?? string.Empty).StartsWith("Linked\\", StringComparison.Ordinal));

        // Edited by S-E004-012 (ADR-GWIN-136): plus the four StationActions links (facts, geometry, memo, messages), thirty-seven.
        // Edited by S-E004-015 (ADR-GWIN-143): plus the two game-free Deposit links (DepositTraitRules, DepositMessages), thirty-nine.
        // Edited by integration (S-E003-008 + S-E004-015): plus the CraftFromBaseGate link of S-E003-008, forty.
        Assert.Equal(40, linked);
    }

    // GW-U-1536 (RD11, CD8): no HarmonyPatch in src targets the piece check, the first-requirement lookup, placement or a Piece method; the folder never mentions ConsumeResources.
    // Edited by integration (S-E003-005 + S-E003-006): contract CD8 names S-E003-006 as the owner of the build checks and placement and
    // calls the vanilla piece check an interim until it lands, so the Building folder of S-E003-006 is outside this scan.
    [Fact]
    public void NoHarmonyPatchTargetsThePieceCheckOrPlacement()
    {
        string[] sourceFiles = Directory.GetFiles(RepoRoot.Combine("src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Replace('\\', '/').Split('/').Any(segment => segment is "bin" or "obj"))
            .Where(path => !path.Replace('\\', '/').Contains("/src/BaseStorage.Plugin/Building/", StringComparison.Ordinal))
            .ToArray();

        foreach (string file in sourceFiles)
        {
            string text = PluginSourceScan.StripComments(RepoRoot.Normalise(File.ReadAllText(file)));
            foreach (Match attribute in Regex.Matches(text, @"\[HarmonyPatch\([^\r\n]*\)\]"))
            {
                Assert.DoesNotMatch(@"HaveRequirements|GetFirstRequiredItem|UpdatePlacement|PatchTargetNames\.Piece\.|typeof\(Piece\)", attribute.Value);
            }
        }

        Assert.DoesNotContain("ConsumeResources", string.Join("\n", FolderSources().Values), StringComparison.Ordinal);
    }

    private static Dictionary<string, string> FolderSources()
    {
        string folder = RepoRoot.Combine(PluginFolder);
        Assert.True(Directory.Exists(folder), $"{PluginFolder} does not exist.");

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string path in Directory.EnumerateFiles(folder, "*.cs"))
        {
            string name = Path.GetFileName(path);
            result[name] = PluginSourceScan.StripComments(RepoRoot.Normalise(RepoRoot.ReadText(PluginFolder + "/" + name)));
        }

        return result;
    }

    private static Dictionary<string, string> CoreSources()
    {
        string folder = RepoRoot.Combine(CoreUiFolder);
        Assert.True(Directory.Exists(folder), $"{CoreUiFolder} does not exist.");

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string path in Directory.EnumerateFiles(folder, "RequirementDisplay*.cs"))
        {
            string name = Path.GetFileName(path);
            result[name] = PluginSourceScan.StripComments(RepoRoot.Normalise(RepoRoot.ReadText(CoreUiFolder + "/" + name)));
        }

        Assert.Equal(2, result.Count);
        return result;
    }

    private static string KeyConstant(string name, string value) =>
        "const\\s+string\\s+" + name + "\\s*=\\s*\"" + Regex.Escape(value) + "\";";
}
