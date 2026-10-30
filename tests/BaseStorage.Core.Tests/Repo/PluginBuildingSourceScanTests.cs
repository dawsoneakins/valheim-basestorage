namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;
using System.Xml.Linq;

/// <summary>
/// GW-U-1631 to GW-U-1641 (AC-3, AC-5, AC-7, AC-9, AC-11; build-from-base contract 1, 5.4, 7 BR1-BR9 and section 1): source scans of
/// the Plugin building folder, the entry point and the Core.Tests project. Files are CRLF-normalised and comment-stripped
/// (ADR-GWIN-132, ADR-GWIN-124). GW-U-1640 is also pinned here, with the Compile items of the Core.Tests project.
/// </summary>
public sealed class PluginBuildingSourceScanTests
{
    private const string BuildingFolder = "src/BaseStorage.Plugin/Building/";
    private const string CoreTestsProject = "tests/BaseStorage.Core.Tests/BaseStorage.Core.Tests.csproj";

    private static readonly string[] GameFree =
    {
        "BuildEnd.cs", "BuildGateReason.cs", "BuildSelection.cs", "BuildMessages.cs", "IBuildGame.cs", "BuildSession.cs",
    };

    private static readonly string[] ContractedFiles =
    {
        "BuildEnd.cs", "BuildGateReason.cs", "BuildSelection.cs", "BuildMessages.cs", "IBuildGame.cs", "BuildSession.cs",
        "PluginBuilding.cs", "GameBuildGame.cs", "PlayerHaveRequirementsPiecePatch.cs", "PlayerTryPlacePiecePatch.cs", "PlayerPlacePiecePatch.cs",
    };

    private static readonly string[] GameTokens =
    {
        "Hud", "InventoryGui", "CraftingStation", "PieceTable", "ItemDrop", "Inventory", "MessageHud", "ZDO", "ZNet", "ZNetView", "ZDOMan", "MonoBehaviour",
    };

    /// <summary>Every .cs file under src, except bin and obj, as a forward-slash relative path mapped to its comment-stripped text.</summary>
    private static Dictionary<string, string> SrcSources()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(RepoRoot.Combine("src"), "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(RepoRoot.FullPath, file).Replace('\\', '/');
            if (relative.Split('/').Any(segment => segment is "bin" or "obj"))
            {
                continue;
            }

            result[relative] = PluginSourceScan.StripComments(RepoRoot.ReadText(relative));
        }

        return result;
    }

    /// <summary>The comment-stripped sources of the Building folder, keyed by file name.</summary>
    private static Dictionary<string, string> FolderSources() =>
        PluginSourceScan.Sources()
            .Where(pair => pair.Key.StartsWith(BuildingFolder, StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key.Substring(BuildingFolder.Length), pair => pair.Value, StringComparer.Ordinal);

    private static string FolderText() => string.Concat(FolderSources().Values);

    private static string StripLiterals(string text) => Regex.Replace(text, @"""(?:[^""\\\n]|\\.)*""", @"""""");

    // GW-U-1631 (BR2, AC-3): the only HarmonyPatch naming Player.HaveRequirements is the piece overload patch, with its argument types.
    [Fact]
    public void ThePieceOverloadIsTheOnlyHaveRequirementsPatchAndCarriesItsArgumentTypes()
    {
        string[] files = SrcSources()
            .Where(pair => Regex.IsMatch(pair.Value, @"\[HarmonyPatch\((?:(?!\]).)*PatchTargetNames\.Player\.HaveRequirements\b"))
            .Select(pair => pair.Key)
            .ToArray();

        Assert.Equal(new[] { "src/BaseStorage.Plugin/Building/PlayerHaveRequirementsPiecePatch.cs" }, files);
        Assert.Matches(
            @"\[HarmonyPatch\(typeof\(Player\), PatchTargetNames\.Player\.HaveRequirements, typeof\(Piece\), typeof\(Player\.RequirementMode\)\)\]",
            SrcSources()[files[0]]);
    }

    // GW-U-1631 (BR2, AC-3): no attribute names the GetFirstRequiredItem lookup.
    [Fact]
    public void NoPatchNamesTheFirstRequiredItemLookup()
    {
        Assert.DoesNotContain(SrcSources().Values, text => Regex.IsMatch(text, @"\[HarmonyPatch\((?:(?!\]).)*PatchTargetNames\.Player\.GetFirstRequiredItem\b"));
    }

    // GW-U-1632 (BR3, AC-6): Update ticks the building session right after the crafting session.
    [Fact]
    public void UpdateTicksTheBuildingSessionRightAfterTheCraftingSession()
    {
        string entry = PluginSourceScan.Sources()[PluginSourceScan.EntryPoint];

        string update = Regex.Replace(PluginSourceScan.MethodBody(entry, @"void\s+Update\s*\(\s*\)"), @"\s+", string.Empty);

        Assert.Contains("PluginCrafting.Tick();PluginBuilding.Tick();", update, StringComparison.Ordinal);
    }

    // GW-U-1632 (BR3, AC-6): Boot initializes the building session after the crafting session and before the patches.
    [Fact]
    public void BootInitializesBuildingAfterCraftingAndBeforeThePatches()
    {
        string entry = PluginSourceScan.Sources()[PluginSourceScan.EntryPoint];
        string boot = PluginSourceScan.MethodBody(entry, @"void\s+Boot\s*\(\s*\)");

        int crafting = boot.IndexOf("PluginCrafting.Initialize(", StringComparison.Ordinal);
        int building = boot.IndexOf("PluginBuilding.Initialize(", StringComparison.Ordinal);
        int apply = boot.IndexOf("PatchApplier.ApplyAll(", StringComparison.Ordinal);

        Assert.True(crafting >= 0 && crafting < building && building < apply, "Boot must call PluginBuilding.Initialize after PluginCrafting.Initialize and before PatchApplier.ApplyAll.");
        Assert.Single(Regex.Matches(boot, Regex.Escape("PluginBuilding.Initialize(")));
    }

    // GW-U-1633 (BR4, AC-5): the folder hands the materials to Core: the availability, the planner, the executor, the layer, the world and the lines.
    [Fact]
    public void TheFolderCallsTheCoreMaterialsAnswers()
    {
        string text = FolderText();

        foreach (string call in new[]
                 {
                     "BaseAvailability.FindFirstMissing(", "ConsumptionPlanner.Plan(", "new ConsumptionExecutor(", "new CraftContainerLayer<Container>(",
                     "new GameCraftWorld(", "CraftRequirementLines.Build(", ".TryPlacePiece(",
                 })
        {
            Assert.Contains(call, text, StringComparison.Ordinal);
        }
    }

    // GW-U-1633 (BR4, AC-5): the folder never counts or consumes by its own rule; .CountItems( occurs only in GameBuildGame.cs.
    [Fact]
    public void TheFolderNeverConsumesByItsOwnRuleAndCountsOnlyInTheGameFile()
    {
        string text = FolderText();

        foreach (string call in new[] { "ConsumeResources(", "CraftRequirement.ForLevel", ".GetAmount(" })
        {
            Assert.DoesNotContain(call, text, StringComparison.Ordinal);
        }

        string[] countingFiles = FolderSources().Where(pair => pair.Value.Contains(".CountItems(", StringComparison.Ordinal)).Select(pair => pair.Key).ToArray();
        Assert.Equal(new[] { "GameBuildGame.cs" }, countingFiles);
    }

    // GW-U-1634 (BR4, AC-1): the base is the one whose station range contains the player; the station-name lookup is never used.
    [Fact]
    public void TheBaseIsFoundByPositionAndNeverByStation()
    {
        string text = FolderText();

        Assert.Contains("BaseStart.FromPosition(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("BaseStart.FromStation(", text, StringComparison.Ordinal);
    }

    // GW-U-1634 (BR4, AC-2): settings.CraftFromBase is read only in the game-bound file.
    [Fact]
    public void TheCraftFromBaseSettingIsReadOnlyInTheGameFile()
    {
        string[] readers = FolderSources().Where(pair => pair.Value.Contains("settings.CraftFromBase", StringComparison.Ordinal)).Select(pair => pair.Key).ToArray();

        Assert.Equal(new[] { "GameBuildGame.cs" }, readers);
    }

    // GW-U-1635 (BR5, AC-7): exactly one Center notice, in the game file; the strings table is never used and the English text is never written here.
    [Fact]
    public void ExactlyOneCenterNoticeIsShownAndTheStringKeyComesFromTheSession()
    {
        Dictionary<string, string> files = FolderSources();

        Assert.Equal(1, PluginSourceScan.CountMatches(files.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal), @"Notices\.Show\("));
        // Edited by S-E008-001 (ADR-GWIN-153, FA3): the placement of the one notice is derived from FeedbackCatalog.PlacementOf(key).
        Assert.Contains("FeedbackCatalog.PlacementOf(key)", files["GameBuildGame.cs"], StringComparison.Ordinal);
        Assert.All(files.Values, text =>
        {
            Assert.DoesNotContain("StringTable.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Materials moved", text, StringComparison.Ordinal);
        });
        Assert.Equal(new[] { "BuildSession.cs" }, files.Where(pair => pair.Value.Contains("StringKeys.", StringComparison.Ordinal)).Select(pair => pair.Key).ToArray());
        Assert.Contains("StringKeys.MessageCraftMaterialsMoved", files["BuildSession.cs"], StringComparison.Ordinal);
        // Edited by S-E008-001 (ADR-GWIN-153, FA2): the session file also names the build success key.
        Assert.Contains("StringKeys.MessageCraftUsedBase", files["BuildSession.cs"], StringComparison.Ordinal);
    }

    // GW-U-1636 (BR6, AC-5): every write goes through the reused craft world; the folder never removes, adds, clones or drops items itself.
    [Fact]
    public void NoFolderFileWritesItemsItself()
    {
        string text = FolderText();

        foreach (string call in new[] { ".RemoveItem(", ".AddItem(", ".Clone()", "DropItem(" })
        {
            Assert.DoesNotContain(call, text, StringComparison.Ordinal);
        }
    }

    // GW-U-1637 (BR1, AC-9): the folder holds exactly the eleven contracted files; the three patch files carry the 5.4 shapes and keys.
    [Fact]
    public void TheFolderHoldsTheElevenContractedFilesAndTheThreePatchesHaveTheContractedShape()
    {
        string[] names = Directory.GetFiles(RepoRoot.Combine("src", "BaseStorage.Plugin", "Building"), "*.cs")
            .Select(path => Path.GetFileName(path)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ContractedFiles.OrderBy(name => name, StringComparer.Ordinal).ToArray(), names);

        Dictionary<string, string> files = FolderSources();
        string[] patchFiles = files.Where(pair => pair.Value.Contains("[HarmonyPatch", StringComparison.Ordinal)).Select(pair => pair.Key).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { "PlayerHaveRequirementsPiecePatch.cs", "PlayerPlacePiecePatch.cs", "PlayerTryPlacePiecePatch.cs" }, patchFiles);

        Assert.Contains("[HarmonyPatch(typeof(Player), PatchTargetNames.Player.TryPlacePiece)]", files["PlayerTryPlacePiecePatch.cs"], StringComparison.Ordinal);
        Assert.Contains("[HarmonyPatch(typeof(Player), PatchTargetNames.Player.PlacePiece)]", files["PlayerPlacePiecePatch.cs"], StringComparison.Ordinal);
        Assert.Contains("internal const string PatchKey = \"BaseStorage.Plugin.Building.PlayerHaveRequirementsPiecePatch.Prefix\";", files["PlayerHaveRequirementsPiecePatch.cs"], StringComparison.Ordinal);
        Assert.Contains("internal const string PrefixKey = \"BaseStorage.Plugin.Building.PlayerTryPlacePiecePatch.Prefix\";", files["PlayerTryPlacePiecePatch.cs"], StringComparison.Ordinal);
        Assert.Contains("internal const string PostfixKey = \"BaseStorage.Plugin.Building.PlayerTryPlacePiecePatch.Postfix\";", files["PlayerTryPlacePiecePatch.cs"], StringComparison.Ordinal);
        Assert.Contains("internal const string PatchKey = \"BaseStorage.Plugin.Building.PlayerPlacePiecePatch.Prefix\";", files["PlayerPlacePiecePatch.cs"], StringComparison.Ordinal);
    }

    // GW-U-1637 (BR1, AC-9): every Prefix and the one Postfix is one statement calling PatchSafety; no finalizer or transpiler; only the TryPlacePiece patch declares a Postfix.
    [Fact]
    public void EveryPatchBodyIsOneSafetyCallAndOnlyTheTryPlacePatchHasAPostfix()
    {
        Dictionary<string, string> files = FolderSources();

        // Edited by S-E003-008, craft-from-base-toggle 8.3: the wrappers check CraftFromBaseGate first; the PatchSafety call moves to PrefixCore / PostfixCore.
        Assert.Matches(@"private\s+static\s+bool\s+Prefix\(Player __instance, Piece piece, ref bool __result\)\s*=>\s*!CraftFromBaseGate\.IsOpen\(\)\s*\|\|\s*PrefixCore\(", files["PlayerTryPlacePiecePatch.cs"]);
        Assert.Matches(@"private\s+static\s+void\s+Postfix\(ref bool __result\)\s*\{\s*if \(CraftFromBaseGate\.IsOpen\(\)\)\s*\{\s*PostfixCore\(ref __result\);", files["PlayerTryPlacePiecePatch.cs"]);
        Assert.Matches(@"private\s+static\s+bool\s+PrefixCore\(.*\)\s*=>\s*PatchSafety\.RunPrefix\(", files["PlayerTryPlacePiecePatch.cs"]);
        Assert.Matches(@"private\s+static\s+void\s+PostfixCore\(.*\)\s*\{\s*\w+\s*=\s*PatchSafety\.Run\(PostfixKey", files["PlayerTryPlacePiecePatch.cs"]);
        Assert.Matches(@"private\s+static\s+bool\s+Prefix\(Player __instance, Piece piece\)\s*=>\s*!CraftFromBaseGate\.IsOpen\(\)\s*\|\|\s*PrefixCore\(", files["PlayerPlacePiecePatch.cs"]);
        Assert.Matches(@"private\s+static\s+bool\s+PrefixCore\(.*\)\s*=>\s*PatchSafety\.RunPrefix\(PatchKey", files["PlayerPlacePiecePatch.cs"]);
        Assert.Matches(@"private\s+static\s+bool\s+Prefix\(Player __instance, Piece piece, Player\.RequirementMode mode, ref bool __result\)\s*=>\s*!CraftFromBaseGate\.IsOpen\(\)\s*\|\|\s*PrefixCore\(", files["PlayerHaveRequirementsPiecePatch.cs"]);
        Assert.Matches(@"private\s+static\s+bool\s+PrefixCore\(.*\)\s*=>\s*PatchSafety\.RunPrefix\(", files["PlayerHaveRequirementsPiecePatch.cs"]);
        Assert.Equal(new[] { "PlayerTryPlacePiecePatch.cs" }, files.Where(pair => Regex.IsMatch(pair.Value, @"\bPostfix\(")).Select(pair => pair.Key).ToArray());
        Assert.All(files.Values, text =>
        {
            Assert.DoesNotContain("Finalizer", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Transpiler", text, StringComparison.Ordinal);
        });
    }

    // GW-U-1638 (BR7, AC-11): the folder uses none of the forbidden game and threading tokens, and declares no per-frame Unity method.
    [Fact]
    public void TheFolderUsesNoForbiddenTokenAndNoPerFrameMethod()
    {
        string text = FolderText();

        foreach (string token in new[]
                 {
                     "SetOwner", "ForceSendZDO", "ClaimOwnership", "SetInUse(", "InvokeRPC", ".Hide(", "CloseContainer", "Thread", "Task.", "Sleep(",
                     "DateTime", "Stopwatch", "MonoBehaviour",
                 })
        {
            Assert.DoesNotContain(token, text, StringComparison.Ordinal);
        }

        Assert.DoesNotMatch(@"\b(void|bool)\s+(Update|LateUpdate|FixedUpdate|OnGUI)\s*\(", text);
    }

    // GW-U-1639 (BR8, AC-11): the six game-free files reference no game or framework namespace and no game type outside comments and literals.
    [Fact]
    public void TheGameFreeFilesReferenceNoGameNamespaceOrGameType()
    {
        Dictionary<string, string> files = FolderSources();

        foreach (string name in GameFree)
        {
            string code = StripLiterals(files[name]);
            Assert.DoesNotContain("using UnityEngine", code, StringComparison.Ordinal);
            Assert.DoesNotContain("using HarmonyLib", code, StringComparison.Ordinal);
            Assert.DoesNotContain("using BepInEx", code, StringComparison.Ordinal);
            foreach (string token in GameTokens)
            {
                Assert.False(Regex.IsMatch(code, @"\b" + token + @"\b"), $"{name} names the game type {token}.");
            }
        }
    }

    // GW-U-1639 (BR9, W4): the game-bound files use no null-conditional or null-coalescing operator and no is-null test on Unity objects.
    [Fact]
    public void TheGameBoundFilesUseNoNullOperatorOnUnityObjects()
    {
        Dictionary<string, string> files = FolderSources();

        foreach (string name in new[] { "GameBuildGame.cs", "PluginBuilding.cs" })
        {
            string code = files[name];
            Assert.DoesNotContain("?.", code, StringComparison.Ordinal);
            Assert.DoesNotContain("??", code, StringComparison.Ordinal);
            Assert.DoesNotContain("is null", code, StringComparison.Ordinal);
            Assert.DoesNotContain("is not null", code, StringComparison.Ordinal);
        }
    }

    // GW-U-1640 (contract 1, edited pin): Core.Tests links exactly the six Building game-free files after the Crafting entries, in table order.
    [Fact]
    public void CoreTestsLinksTheSixGameFreeBuildingFilesAfterTheCraftingEntriesInTableOrder()
    {
        XDocument project = RepoRoot.LoadXml(CoreTestsProject);
        (string Include, string Link)[] compiles = project.Descendants()
            .Where(element => element.Name.LocalName == "Compile")
            .Select(element => (element.Attribute("Include")?.Value ?? string.Empty, element.Attribute("Link")?.Value ?? string.Empty))
            .ToArray();

        (string Include, string Link)[] building = compiles
            .Where(entry => entry.Include.Contains("Building", StringComparison.Ordinal))
            .ToArray();
        (string Include, string Link)[] expected = GameFree
            .Select(name => (@"..\..\src\BaseStorage.Plugin\Building\" + name, @"Linked\" + name))
            .ToArray();
        Assert.Equal(expected, building);

        int lastCrafting = Array.FindLastIndex(compiles, entry => entry.Include.Contains("Crafting", StringComparison.Ordinal));
        int firstBuilding = Array.FindIndex(compiles, entry => entry.Include.Contains("Building", StringComparison.Ordinal));
        Assert.True(lastCrafting >= 0 && lastCrafting < firstBuilding, "The Building links must follow the Crafting links.");
    }

    // GW-U-1641 (AC-11): src/BaseStorage.Core declares no Building namespace and has no Building folder.
    [Fact]
    public void TheCoreProjectDeclaresNoBuildingNamespaceAndHasNoBuildingFolder()
    {
        Assert.False(Directory.Exists(RepoRoot.Combine("src", "BaseStorage.Core", "Building")));

        string[] coreFiles = Directory.GetFiles(RepoRoot.Combine("src", "BaseStorage.Core"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains("\\obj\\", StringComparison.Ordinal) && !path.Contains("\\bin\\", StringComparison.Ordinal))
            .ToArray();
        Assert.DoesNotContain(coreFiles, path => Regex.IsMatch(RepoRoot.Normalise(File.ReadAllText(path)), @"namespace\s+[\w.]*\bBuilding\b"));
    }
}
