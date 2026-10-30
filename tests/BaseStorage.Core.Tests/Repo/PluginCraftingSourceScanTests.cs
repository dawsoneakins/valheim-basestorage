namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;
using System.Xml.Linq;

/// <summary>
/// GW-U-1140 to GW-U-1149 and GW-U-1146 (AC-2 to AC-5; contract 1 and 7.3 CR1-CR9): source scans of the Plugin crafting folder,
/// the Plugin entry point and the Core project. Files are CRLF-normalised and comment-stripped (ADR-GWIN-124).
/// </summary>
public sealed class PluginCraftingSourceScanTests
{
    private const string CraftingFolder = "src/BaseStorage.Plugin/Crafting";
    private const string CoreTestsProject = "tests/BaseStorage.Core.Tests/BaseStorage.Core.Tests.csproj";

    private static readonly string[] GameFree =
    {
        "CraftPhase.cs", "CraftEnd.cs", "CraftGateReason.cs", "CraftSelection.cs", "RecipeLine.cs", "CraftRequirementLines.cs",
        "CraftMessages.cs", "ICraftGame.cs", "ICraftWorld.cs", "CraftContainerLayer.cs", "CraftSession.cs",
    };

    private static readonly string[] GameBound = { "PluginCrafting.cs", "GameCraftGame.cs", "GameCraftWorld.cs" };

    private static readonly string[] PatchFiles = { "PlayerHaveRequirementItemsPatch.cs", "PlayerConsumeResourcesPatch.cs", "InventoryGuiDoCraftingPatch.cs" };

    // GW-U-1140 (CR3): Update ticks the crafting session right after the reservations.
    [Fact]
    public void UpdateTicksTheCraftingSessionRightAfterTheReservations()
    {
        string entry = PluginSourceScan.Sources()[PluginSourceScan.EntryPoint];

        string update = Regex.Replace(PluginSourceScan.MethodBody(entry, @"void\s+Update\s*\(\s*\)"), @"\s+", string.Empty);

        Assert.Contains("PluginReservations.Tick();PluginCrafting.Tick();", update, StringComparison.Ordinal);
    }

    // GW-U-1140 (CR3): Boot initializes the crafting session after the writer and before the patches.
    [Fact]
    public void BootInitializesCraftingAfterTheWriterAndBeforeThePatches()
    {
        string entry = PluginSourceScan.Sources()[PluginSourceScan.EntryPoint];

        string boot = PluginSourceScan.MethodBody(entry, @"void\s+Boot\s*\(\s*\)");

        int writer = boot.IndexOf("ChestCategoryWriter.Initialize(", StringComparison.Ordinal);
        int crafting = boot.IndexOf("PluginCrafting.Initialize(", StringComparison.Ordinal);
        int apply = boot.IndexOf("PatchApplier.ApplyAll(", StringComparison.Ordinal);
        Assert.True(writer >= 0 && writer < crafting && crafting < apply, "Boot must call PluginCrafting.Initialize after ChestCategoryWriter.Initialize and before PatchApplier.ApplyAll.");
    }

    // GW-U-1141 (CR4): the folder decides materials through Core and never through the vanilla helpers.
    [Fact]
    public void TheFolderDecidesMaterialsThroughCore()
    {
        string all = string.Join("\n", FolderFiles().Select(file => file.Text));

        Assert.Contains("BaseAvailability.FindFirstMissing(", all, StringComparison.Ordinal);
        Assert.Contains("ConsumptionPlanner.Plan(", all, StringComparison.Ordinal);
        Assert.Contains("new ConsumptionExecutor(", all, StringComparison.Ordinal);
        Assert.Contains(".GetAmount(qualityLevel)", all, StringComparison.Ordinal);
        Assert.Contains("CraftRequirementLines.Build(", all, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"CraftRequirement\.ForLevel", all);
        Assert.DoesNotMatch(@"Inventory\.CountItems", all);
        Assert.DoesNotContain(FilesMatching(@"[Ss]ettings\.CraftFromBase"), name => name != "GameCraftGame.cs");
    }

    // GW-U-1142 (CR7): no forbidden ownership, network, threading, timing or per-frame token appears in the folder.
    [Fact]
    public void TheFolderUsesNoForbiddenTokens()
    {
        string[] forbidden =
        {
            @"SetOwner", @"ForceSendZDO", @"ClaimOwnership", @"SetInUse\(", @"InvokeRPC", @"\.Hide\(", @"CloseContainer",
            @"\bThread\b", @"Task\.", @"Sleep\(", @"DateTime", @"Stopwatch", @"MonoBehaviour",
            @"void\s+(Update|LateUpdate|FixedUpdate|OnGUI)\s*\(",
        };

        foreach (string pattern in forbidden)
        {
            Assert.Empty(FilesMatching(pattern));
        }
    }

    // GW-U-1143 (CR8): the eleven game-free files name no game assembly and no game type outside comments and literals.
    [Fact]
    public void TheGameFreeFilesNameNoGameType()
    {
        var violations = new List<string>();
        foreach (string name in GameFree)
        {
            string text = FileText(name);
            string withoutLiterals = Regex.Replace(text, @"""(?:\\.|[^""\\\n])*""", "\"\"");
            if (Regex.IsMatch(text, @"using\s+(UnityEngine|HarmonyLib|BepInEx)\b"))
            {
                violations.Add(name + ": game using directive");
            }

            foreach (Match match in Regex.Matches(withoutLiterals, @"\b(InventoryGui|CraftingStation|ItemDrop|Inventory|MessageHud|ZDO|ZNet|ZNetView|ZDOMan|MonoBehaviour)\b"))
            {
                violations.Add(name + ": " + match.Value);
            }
        }

        Assert.Empty(violations);
    }

    // GW-U-1144 (CR9, W4): the game-bound files use no null-conditional, null-coalescing or is-null test on game objects.
    [Fact]
    public void TheGameBoundFilesUseNoUnityNullShortcuts()
    {
        foreach (string name in new[] { "GameCraftGame.cs", "GameCraftWorld.cs", "PluginCrafting.cs" })
        {
            string text = FileText(name);
            Assert.DoesNotMatch(@"\?\.|\?\?|\bis\s+null\b|\bis\s+not\s+null\b", text);
        }
    }

    // GW-U-1145 (AC-2): Core declares no Crafting namespace and no Crafting folder (the v1 types must not exist).
    [Fact]
    public void CoreHasNoCraftingNamespaceOrFile()
    {
        string[] coreFiles = Directory.GetFiles(RepoRoot.Combine("src/BaseStorage.Core"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains("\\obj\\", StringComparison.Ordinal) && !path.Contains("\\bin\\", StringComparison.Ordinal))
            .ToArray();

        // Narrowed by S-E003-004, ADR-GWIN-124: the pre-existing Availability/CraftRequirement.cs is v2-sanctioned; only a Crafting folder is forbidden.
        Assert.DoesNotContain(coreFiles, path => path.Replace(Path.DirectorySeparatorChar, '/').Contains("/Crafting/", StringComparison.Ordinal));
        Assert.DoesNotContain(coreFiles, path => Regex.IsMatch(RepoRoot.Normalise(File.ReadAllText(path)), @"namespace\s+BaseStorage\.Core\.Crafting\b"));
    }

    // GW-U-1146 (contract 1): the Core.Tests project links the eleven game-free files in table order.
    [Fact]
    public void CoreTestsLinksTheElevenGameFreeFilesInTableOrder()
    {
        XDocument project = RepoRoot.LoadXml(CoreTestsProject);
        (string Include, string Link)[] actual = project.Descendants()
            .Where(element => element.Name.LocalName == "Compile" && (element.Attribute("Include")?.Value ?? string.Empty).Contains("Crafting", StringComparison.Ordinal))
            .Select(element => (element.Attribute("Include")!.Value, element.Attribute("Link")!.Value))
            .ToArray();

        (string Include, string Link)[] expected = GameFree
            .Select(name => (@"..\..\src\BaseStorage.Plugin\Crafting\" + name, @"Linked\" + name))
            .ToArray();
        Assert.Equal(expected, actual);
    }

    // GW-U-1147 (CR2): no HarmonyPatch attribute in src patches HaveRequirements (either overload) or GetFirstRequiredItem.
    [Fact]
    public void NoPatchTargetsThePieceOverloadOrTheFirstRequiredLookup()
    {
        string[] sourceFiles = Directory.GetFiles(RepoRoot.Combine("src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains("\\obj\\", StringComparison.Ordinal) && !path.Contains("\\bin\\", StringComparison.Ordinal))
            .ToArray();

        var hits = sourceFiles
            .Where(path => Regex.IsMatch(RepoRoot.Normalise(File.ReadAllText(path)), @"\[HarmonyPatch\((?:(?!\]).)*PatchTargetNames\.Player\.(?:HaveRequirements\)|GetFirstRequiredItem)"))
            .Select(Path.GetFileName)
            .ToArray();

        Assert.Empty(hits);
    }

    // GW-U-1148 (CR5): one Center notice in GameCraftGame.cs, no string-table text, and the key only in CraftSession.cs.
    [Fact]
    public void TheNoticeIsShownOnceAndTheKeyIsUsedOnlyBySession()
    {
        Assert.Single(FilesMatching(@"Notices\.Show\("), "GameCraftGame.cs");
        Assert.Equal(1, FolderFiles().Sum(file => Regex.Matches(file.Text, @"Notices\.Show\(").Count));
        // Edited by S-E008-001 (ADR-GWIN-153, FA3): the placement of the one notice is derived from FeedbackCatalog.PlacementOf(key).
        Assert.Contains("FeedbackCatalog.PlacementOf(key)", FileText("GameCraftGame.cs"), StringComparison.Ordinal);
        Assert.DoesNotMatch(@"StringTable\.", string.Join("\n", FolderFiles().Select(file => file.Text)));
        Assert.DoesNotContain("Materials moved", string.Join("\n", FolderFiles().Select(file => file.Text)), StringComparison.Ordinal);
        Assert.Equal(new[] { "CraftSession.cs" }, FilesMatching(@"StringKeys\."));
        Assert.Contains("StringKeys.MessageCraftMaterialsMoved", FileText("CraftSession.cs"), StringComparison.Ordinal);
        // Edited by S-E008-001 (ADR-GWIN-153, FA1): the session file also names the craft success key.
        Assert.Contains("StringKeys.MessageCraftUsedBase", FileText("CraftSession.cs"), StringComparison.Ordinal);
    }

    // GW-U-1148 (CR6): the item mutators live only in GameCraftWorld.cs, and every chest write follows the CanWrite guard in the layer.
    [Fact]
    public void TheItemMutatorsLiveOnlyInTheWorldAndEveryChestWriteIsGuarded()
    {
        Assert.Equal(new[] { "GameCraftWorld.cs" }, FilesMatching(@"\.RemoveItem\(|\.AddItem\(|\.Clone\(\)|DropItem\("));

        string layer = FileText("CraftContainerLayer.cs");
        Assert.Contains("driver.CanWrite(", layer, StringComparison.Ordinal);
        foreach (Match write in Regex.Matches(layer, @"world\.(Take|PutBack)\("))
        {
            string before = layer.Substring(0, write.Index);
            Assert.True(before.Contains("CanWrite(", StringComparison.Ordinal) || before.Contains("PlayerInventoryId", StringComparison.Ordinal), "A chest write is not preceded by driver.CanWrite(.");
        }
    }

    // GW-U-1149 (CR1): the folder holds exactly the 17 contracted files.
    [Fact]
    public void TheFolderHoldsExactlyTheSeventeenContractedFiles()
    {
        string[] expected = GameFree.Concat(GameBound).Concat(PatchFiles).OrderBy(name => name, StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, FolderFiles().Select(file => file.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray());
    }

    // GW-U-1149 (CR1): exactly the 3 patch files carry HarmonyPatch, each with the contracted attribute, key and single-statement prefix.
    [Fact]
    public void ExactlyThePatchFilesCarryTheContractedAttributeKeyAndPrefix()
    {
        Assert.Equal(PatchFiles.OrderBy(name => name, StringComparer.Ordinal).ToArray(), FilesMatching(@"\[HarmonyPatch").OrderBy(name => name, StringComparer.Ordinal).ToArray());

        foreach (string name in PatchFiles)
        {
            string text = FileText(name);
            string className = Path.GetFileNameWithoutExtension(name);
            Assert.Matches(@"\[HarmonyPatch\(typeof\(\w+\), PatchTargetNames\.\w+\.\w+\)\]", text);
            Assert.Contains("internal const string PatchKey = \"BaseStorage.Plugin.Crafting." + className + ".Prefix\";", text, StringComparison.Ordinal);
            // Edited by S-E003-008, craft-from-base-toggle 8.3: the wrapper checks CraftFromBaseGate first and the PatchSafety call moves to PrefixCore.
            Assert.Matches(@"static bool Prefix\(.*\)\s*=>\s*!CraftFromBaseGate\.IsOpen\(\)\s*\|\|\s*PrefixCore\(", text);
            Assert.Matches(@"static bool PrefixCore\(.*\)\s*=>\s*PatchSafety\.RunPrefix\(PatchKey,", text);
        }
    }

    // GW-U-1149 (CR1): no folder file declares a Postfix, a Finalizer or a Transpiler.
    [Fact]
    public void NoFolderFileDeclaresAPostfixFinalizerOrTranspiler()
    {
        Assert.Empty(FilesMatching(@"\b(Postfix|Finalizer|Transpiler)\b"));
    }

    private static string FileText(string name) =>
        PluginSourceScan.StripComments(RepoRoot.Normalise(RepoRoot.ReadText(CraftingFolder + "/" + name)));

    private static List<(string Name, string Text)> FolderFiles() =>
        Directory.GetFiles(RepoRoot.Combine(CraftingFolder), "*.cs")
            .Select(path => (Name: Path.GetFileName(path), Text: PluginSourceScan.StripComments(RepoRoot.Normalise(File.ReadAllText(path)))))
            .ToList();

    private static string[] FilesMatching(string pattern) =>
        FolderFiles().Where(file => Regex.IsMatch(file.Text, pattern)).Select(file => file.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
}
