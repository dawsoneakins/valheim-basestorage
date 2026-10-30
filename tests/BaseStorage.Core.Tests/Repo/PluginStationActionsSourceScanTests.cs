namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BaseStorage.Core.Ui;

/// <summary>
/// GW-U-1778..1788 (AC-1..AC-5, contract 9.5): source scans of the station-actions folder of the Plugin. They pin the six files, the one
/// Harmony postfix, the factory use, the clone hygiene, write-on-difference, the zero-allocation frame, the forbidden tokens, the
/// game-free files, the boot order and the factory addition. Texts are comment-stripped and CRLF-normalised (PluginSourceScan), so
/// a commented-out line cannot satisfy a rule. Red until S-E004-012 implement adds the folder.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginStationActionsSourceScanTests
{
    private const string Folder = "src/BaseStorage.Plugin/StationActions/";
    private const string PanelFile = Folder + "StationActionsPanel.cs";
    private const string PatchFile = Folder + "InventoryGuiUpdateRecipePatch.cs";
    private const string FactoryFile = "src/BaseStorage.Plugin/Ui/VanillaButtonFactory.cs";

    private static readonly string[] FolderNames =
    {
        "InventoryGuiUpdateRecipePatch.cs",
        "StationActionsFacts.cs",
        "StationActionsMemo.cs",
        "StationActionsMessages.cs",
        "StationActionsPanel.cs",
        "StationPanelGeometry.cs",
    };

    private static readonly string[] GameFreeNames =
    {
        "StationActionsFacts.cs",
        "StationActionsMemo.cs",
        "StationActionsMessages.cs",
        "StationPanelGeometry.cs",
    };

    // GW-U-1778 (9.5): the folder holds exactly the six files, all internal, in namespace BaseStorage.Plugin.StationActions.
    [Fact]
    public void TheFolderHoldsExactlyTheSixInternalFilesOfTheContractedNamespace()
    {
        Dictionary<string, string> files = FolderSources();

        string[] expected = FolderNames.Select(name => Folder + name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        Assert.Equal(expected, files.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray());
        foreach (KeyValuePair<string, string> pair in files)
        {
            Assert.Contains("namespace BaseStorage.Plugin.StationActions;", pair.Value, StringComparison.Ordinal);
            Assert.Matches(@"internal\s+(static\s+|sealed\s+|readonly\s+)*(class|struct|enum|interface|record)\b", pair.Value);
            Assert.DoesNotMatch(@"\bpublic\b", pair.Value);
        }
    }

    // GW-U-1779 (AC-5, 9.5): the one HarmonyPatch is the UpdateRecipe postfix, with one single-statement Postfix and no other patch kind.
    [Fact]
    public void OnlyTheUpdateRecipeFileCarriesTheSingleStatementPostfix()
    {
        Dictionary<string, string> files = FolderSources();
        Assert.Equal(new[] { PatchFile }, PluginSourceScan.FilesMatching(files, @"\[HarmonyPatch"));

        string patch = files[PatchFile];
        Assert.Contains("[HarmonyPatch(typeof(InventoryGui), PatchTargetNames.InventoryGui.UpdateRecipe, typeof(Player), typeof(float))]", patch, StringComparison.Ordinal);
        Assert.Matches(
            @"private\s+static\s+void\s+Postfix\(InventoryGui\s+__instance\)\s*=>\s*PatchSafety\.Run\(PatchKey,\s*__instance,\s*static\s+gui\s*=>\s*StationActionsPanel\.Refresh\(gui\)\);",
            patch);
        Assert.Single(Regex.Matches(patch, @"\bPostfix\("));
        Assert.DoesNotMatch(@"\b(Prefix|Finalizer|Transpiler)\b", patch);
    }

    // GW-U-1950 (AC-1, 9.5; the edited GW-U-1780 of deposit-button contract 6): two Create calls, Organize then Deposit, with the same template and
    // parent, one CreateText for the status line from the station name, and the Deposit id declared once and the layout's DepositButton read.
    // Edited by S-E004-015 (ADR-GWIN-143).
    [Fact]
    public void ThePanelCreatesTheOrganizeAndDepositButtonsAndTheStatusLineThroughTheFactory()
    {
        string panel = Text(PanelFile);

        MatchCollection creates = Regex.Matches(panel, @"VanillaButtonFactory\.Create\(");
        Assert.Equal(2, creates.Count);
        Assert.Single(Regex.Matches(panel, @"VanillaButtonFactory\.CreateText\("));
        Assert.Matches(
            @"VanillaButtonFactory\.Create\(\s*[\w.]*m_craftButton\s*,\s*[\w.]*m_crafting\s*,\s*OrganizeButtonId\s*,\s*StringKeys\.StationActionsOrganizeButton\b",
            panel);
        Assert.Matches(
            @"VanillaButtonFactory\.Create\(\s*[\w.]*m_craftButton\s*,\s*[\w.]*m_crafting\s*,\s*DepositButtonId\s*,\s*StringKeys\.StationActionsDepositButton\b",
            panel);
        Assert.True(creates[0].Index < creates[1].Index, "The Organize clone must be created before the Deposit clone.");
        Assert.Single(Regex.Matches(panel, @"string\s+DepositButtonId\s*=\s*""station\.actions\.deposit-button"""));
        Assert.Matches(@"VanillaButtonFactory\.CreateText\(\s*[\w.]*m_craftingStationName\s*,[^;]*StatusLineId\b", panel);
        Assert.Matches(@"\.DepositButton\b", panel);
    }

    // GW-U-1951 (AC-2, 9.5; the edited GW-U-1781 of deposit-button contract 6): the station start calls the Core decisions and both runner entry
    // points, and never the planner, the position start, the lock, a store write, a notice or a popup. The Deposit's Debug line is the one
    // LogPolicy.Write the panel may hold, so the store-write token excludes it. Edited by S-E004-015 (ADR-GWIN-143).
    [Fact]
    public void TheStationStartCallsTheCoreDecisionsAndNothingThatPlansLocksOrPrompts()
    {
        string panel = Text(PanelFile);

        foreach (string call in new[]
                 {
                     "StationPanelLayout.Compute(",
                     "new StationPanelInput(",
                     "BaseStart.FromStation(",
                     "OrganizeRunner.StartAt(",
                     "OrganizeRunner.StartDepositAt(",
                     "DepositMessages.Clicked(",
                     "StationActionsFacts.",
                     "StationPanelGeometry.",
                     "GetCurrentCraftingStation()",
                 })
        {
            Assert.Contains(call, panel, StringComparison.Ordinal);
        }

        foreach (string token in new[]
                 {
                     "BaseStart.FromPosition(",
                     "OrganizePlanner.",
                     "DepositPlanner.",
                     ".Clear(",
                     "Notices.Show(",
                     ".ShowMessage(",
                     "UnifiedPopup",
                     "YesNoPopup",
                 })
        {
            Assert.DoesNotContain(token, panel, StringComparison.Ordinal);
        }

        Assert.DoesNotMatch(@"(?<!LogPolicy)\.Write\(", panel);
        Assert.DoesNotMatch(@"\bOrganizeLock\b", panel);
    }

    // GW-U-1782 (AC-1, SA5, 9.5): the gamepad hint is named only in the panel, and the cloned labels keep their size and do not wrap.
    [Fact]
    public void TheGamepadHintIsOnlyInThePanelAndTheClonesAreHygienic()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        Assert.Equal(new[] { "src/BaseStorage.Plugin/BaseMenu/BaseMenuPanel.cs", PanelFile }, PluginSourceScan.FilesMatching(sources, @"\bUIGamePad\b"));

        string panel = sources[PanelFile];
        Assert.Matches(@"UIGamePad[\s\S]*?enabled\s*=\s*false[\s\S]*?m_hint", panel);
        Assert.Matches(@"ignoreLayout\s*=\s*true", panel);
        Assert.Contains("TextWrappingModes.NoWrap", panel, StringComparison.Ordinal);
        Assert.Contains("TextOverflowModes.Ellipsis", panel, StringComparison.Ordinal);
        Assert.Equal(0, PluginSourceScan.CountMatches(sources, Regex.Escape("enableWordWrapping")));
    }

    // GW-U-1783 (AC-4, SA6, 9.5): SetActive, interactable and the label text are written only when they differ.
    [Fact]
    public void ActiveStateInteractableAndTextAreWrittenOnlyOnDifference()
    {
        string panel = Text(PanelFile);

        string[] lines = panel.Split('\n');
        int setActiveCalls = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains("SetActive(", StringComparison.Ordinal))
            {
                continue;
            }

            setActiveCalls++;
            int from = Math.Max(0, i - 3);
            int to = Math.Min(lines.Length - 1, i + 3);
            Assert.True(
                Enumerable.Range(from, to - from + 1).Any(j => lines[j].Contains("activeSelf", StringComparison.Ordinal)),
                "SetActive on line " + (i + 1) + " has no activeSelf guard within three lines.");
        }

        Assert.True(setActiveCalls > 0, "The panel shows and hides its controls with SetActive.");
        Assert.Matches(@"if\s*\([^)]*\binteractable\s*!=[^)]*\)[\s\S]{0,120}?\binteractable\s*=[^=]", panel);

        MatchCollection writes = Regex.Matches(panel, @"\.(?:m_)?text\s*=[^=]");
        Assert.NotEmpty(writes);
        foreach (Match write in writes)
        {
            string window = panel.Substring(Math.Max(0, write.Index - 200), Math.Min(200, write.Index));
            Assert.True(
                window.Contains("string.Equals(", StringComparison.Ordinal) && window.Contains("StringComparison.Ordinal", StringComparison.Ordinal),
                "A text write at offset " + write.Index + " is not behind an ordinal string.Equals guard.");
        }
    }

    // GW-U-1784 (AC-1, SA11, ADR-GWIN-135, 9.5): the per-frame path (Refresh and its private helpers) has no lambda, collection, interpolation, string build or Linq. ReadFacts is the one exempt step.
    [Fact]
    public void TheSteadyFrameAllocatesNothingAndOnlyReadFactsMayAllocate()
    {
        string panel = Text(PanelFile);
        Assert.Contains("ReadFacts(", panel, StringComparison.Ordinal);

        var bodies = new List<string>
        {
            BodyOf(panel, @"static\s+void\s+Refresh\s*\(\s*InventoryGui\s+gui\s*\)"),
        };
        foreach (Match helper in Regex.Matches(panel, @"private\s+static\s+[\w<>\[\]?.,]+\s+(\w+)\s*\("))
        {
            string name = helper.Groups[1].Value;
            if (name == "ReadFacts")
            {
                continue;
            }

            bodies.Add(BodyOf(panel, @"private\s+static\s+[\w<>\[\]?.,]+\s+" + Regex.Escape(name) + @"\s*\("));
        }

        string[] forbidden = { "=>", "new List<", "new Dictionary<", "$\"", "string.Format", "string.Concat", "+ \"", "System.Linq", ".Select(", ".Where(", ".ToArray(", ".ToList(" };
        foreach (string body in bodies)
        {
            foreach (string token in forbidden)
            {
                Assert.DoesNotContain(token, body, StringComparison.Ordinal);
            }
        }
    }

    // GW-U-1785 (AC-3, 9.5): no folder file creates or moves game objects, claims ownership, sends RPCs, uses threads or time, derives a Unity frame method, or carries strings-table text.
    [Fact]
    public void TheFolderHasNoForbiddenGameWriteThreadTimeOrStringTableToken()
    {
        string[] forbidden =
        {
            "Instantiate(",
            "new GameObject(",
            "AddComponent<Button>",
            "ClaimOwnership",
            "SetOwner",
            "InvokeRPC",
            "SetInUse",
            "Thread",
            "Task.",
            "Sleep(",
            "DateTime",
            "Stopwatch",
            "MonoBehaviour",
            "void Update(",
            "LateUpdate",
            "FixedUpdate",
            "OnGUI",
            "StringTable.",
            "Notices.",
            ".ShowMessage(",
        };

        Dictionary<string, string> files = FolderSources();
        Assert.Equal(FolderNames.Length, files.Count);
        foreach (KeyValuePair<string, string> pair in files)
        {
            foreach (string token in forbidden)
            {
                Assert.DoesNotContain(token, pair.Value, StringComparison.Ordinal);
            }

            foreach (string english in EnglishTexts())
            {
                Assert.DoesNotContain(english, pair.Value, StringComparison.Ordinal);
            }
        }
    }

    // GW-U-1786 (AC-1, 9.5): the four game-free files have no game, Harmony, BepInEx or TMPro using and no game type name outside comments.
    [Fact]
    public void TheFourGameFreeFilesHaveNoGameUsingAndNoGameTypeName()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        foreach (string name in GameFreeNames)
        {
            string key = Folder + name;
            Assert.True(sources.ContainsKey(key), key + " does not exist yet (S-E004-012, implement phase).");
            string text = sources[key];
            Assert.DoesNotMatch(@"using\s+(UnityEngine|HarmonyLib|BepInEx|TMPro)\b", text);
            Assert.DoesNotMatch(@"\b(InventoryGui|Player|CraftingStation|Container|ZDO|ZNetView|RectTransform|Canvas|UITooltip)\b", text);
        }
    }

    // GW-U-1787 (AC-2, 9.5, Boot order): Boot starts the station actions after the runner and before Harmony; Update is unchanged.
    [Fact]
    public void BootStartsTheStationActionsAfterTheRunnerAndBeforeHarmonyAndUpdateIsUnchanged()
    {
        string entry = PluginSourceScan.Sources()[PluginSourceScan.EntryPoint];
        string boot = PluginSourceScan.MethodBody(entry, @"void\s+Boot\s*\(\s*\)");

        Assert.Single(Regex.Matches(boot, Regex.Escape("StationActionsPanel.Initialize(")));
        int runner = boot.IndexOf("OrganizeRunner.Initialize(", StringComparison.Ordinal);
        int station = boot.IndexOf("StationActionsPanel.Initialize(", StringComparison.Ordinal);
        int harmony = boot.IndexOf("new Harmony(", StringComparison.Ordinal);
        Assert.True(runner >= 0 && runner < station && station < harmony, "Boot must call StationActionsPanel.Initialize after OrganizeRunner.Initialize and before new Harmony(.");

        string update = Regex.Replace(PluginSourceScan.MethodBody(entry, @"void\s+Update\s*\(\s*\)"), @"\s+", string.Empty);
        Assert.Contains("ChestCategoryWriter.Tick();CategoryHotkey.Tick();OrganizeRunner.Tick();", update, StringComparison.Ordinal);
        Assert.DoesNotContain("StationActions", update, StringComparison.Ordinal);
    }

    // GW-U-1788 (AC-1, contract 4, 9.5): the factory declares CreateText with the contracted signature, and only the id is a string parameter.
    [Fact]
    public void TheFactoryDeclaresCreateTextWithTheContractedSignature()
    {
        string factory = PluginSourceScan.StripComments(RepoRoot.ReadText(FactoryFile));

        Assert.Matches(@"public\s+static\s+TMP_Text\?\s+CreateText\(TMP_Text\?\s+template,\s*Transform\s+parent,\s*string\s+id\)", factory);
    }

    private static Dictionary<string, string> FolderSources() =>
        PluginSourceScan.Sources()
            .Where(pair => pair.Key.StartsWith(Folder, StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    private static string Text(string relative)
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        Assert.True(sources.ContainsKey(relative), relative + " does not exist yet (S-E004-012, implement phase).");
        return sources[relative];
    }

    /// <summary>The body of the member whose header matches. An expression body runs to its semicolon; a block body is read by brace depth.</summary>
    private static string BodyOf(string text, string headerPattern)
    {
        Match header = Regex.Match(text, headerPattern);
        Assert.True(header.Success, "No member matches " + headerPattern + ".");

        int after = header.Index + header.Length;
        int arrow = after;
        while (arrow < text.Length && char.IsWhiteSpace(text[arrow]))
        {
            arrow++;
        }

        if (string.CompareOrdinal(text, arrow, "=>", 0, 2) == 0)
        {
            int end = text.IndexOf(';', arrow);
            Assert.True(end > arrow, "The expression body of " + headerPattern + " is not terminated.");
            return text.Substring(arrow, end - arrow);
        }

        return PluginSourceScan.MethodBody(text, headerPattern);
    }

    // GW-U-2258 (AC-2, station-actions 12.3): the facts file computes the fill through BaseFill, and imports the Core namespace for it.
    [Fact]
    public void StationActionsFactsComputesTheFillThroughBaseFill()
    {
        string facts = Text(Folder + "StationActionsFacts.cs");

        Assert.Contains("BaseFill.FromContainers(", facts, StringComparison.Ordinal);
        Assert.Contains("using BaseStorage.Core.Bases.Fill;", facts, StringComparison.Ordinal);
    }

    // GW-U-2258 (AC-2, station-actions 12.3): the panel reads the percentage through StationActionsFacts.FillPercent.
    [Fact]
    public void TheStationPanelReadsTheFillThroughStationActionsFacts()
    {
        Assert.Contains("StationActionsFacts.FillPercent(", Text(PanelFile), StringComparison.Ordinal);
    }

    // GW-U-2258 (AC-2, station-actions 12.3): BaseFill is the only percentage source, so the facts file uses it and no file in the
    // station-actions folder does percentage arithmetic of its own.
    [Fact]
    public void NoStationActionsFileDoesPercentageArithmetic()
    {
        Assert.Contains("BaseFill.FromContainers(", Text(Folder + "StationActionsFacts.cs"), StringComparison.Ordinal);

        foreach (KeyValuePair<string, string> pair in FolderSources())
        {
            foreach (string token in new[] { "Math.Round", "MidpointRounding", "* 100", "100 *" })
            {
                Assert.DoesNotContain(token, pair.Value, StringComparison.Ordinal);
            }
        }
    }

    // GW-U-2259 (AC-1, AC-2, station-actions 12.2): the panel memo compares and stores the fill, passes it as the ninth constructor argument, and
    // calls FillPercent and EligibleChestCount exactly once each, both in ReadFacts.
    [Fact]
    public void ThePanelMemoAndConstructorCarryTheFill()
    {
        string panel = Text(PanelFile);

        foreach (string token in new[] { "factFill == inFill", "inFill = factFill", "factEligible, factFill, factHolder, factButtons)" })
        {
            Assert.Contains(token, panel, StringComparison.Ordinal);
        }

        string readFacts = BodyOf(panel, @"private static void ReadFacts\(");
        Assert.Equal(1, CountOf(readFacts, "StationActionsFacts.FillPercent("));
        Assert.Equal(1, CountOf(readFacts, "StationActionsFacts.EligibleChestCount("));
        Assert.Equal(1, CountOf(panel, "StationActionsFacts.FillPercent("));
        Assert.Equal(1, CountOf(panel, "StationActionsFacts.EligibleChestCount("));
    }

    private static int CountOf(string text, string token)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }

    /// <summary>The English strings-table texts of 8 characters or more, read from Core through reflection (EnglishStrings is internal).</summary>
    private static IEnumerable<string> EnglishTexts()
    {
        Type? table = typeof(StringTable).Assembly.GetType("BaseStorage.Core.Ui.EnglishStrings", throwOnError: false);
        Assert.True(table is not null, "BaseStorage.Core.Ui.EnglishStrings is missing.");
        FieldInfo? pairs = table!.GetField("Pairs", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(pairs is not null, "EnglishStrings.Pairs is missing.");

        foreach (object? pair in (System.Collections.IEnumerable)pairs!.GetValue(null)!)
        {
            string? text = pair!.GetType().GetField("Item2")!.GetValue(pair) as string;
            if (text is not null && text.Length >= 8)
            {
                yield return text;
            }
        }
    }
}
