namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

/// <summary>
/// AC-1 to AC-9 (ADR-GWIN-030, ADR-GWIN-031, contract sections 1, 4, 5, 6 and 7): text scans of src/BaseStorage.Plugin/World/
/// and of BaseStoragePlugin.cs. The scans read comment-stripped text (PluginSourceScan), so a commented-out line cannot satisfy a
/// rule. The game types cannot load in a test process, so wiring, patch shape and per-frame rules are checked here.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginWorldSourceScanTests
{
    private const string WorldFolder = "src/BaseStorage.Plugin/World/";
    private const string PluginWorldFile = WorldFolder + "PluginWorld.cs";
    private const string SnapshotSourceFile = WorldFolder + "GameSnapshotSource.cs";
    private const string GameClockFile = WorldFolder + "GameClock.cs";
    private const string LoadedRegistryFile = WorldFolder + "LoadedRegistry.cs";
    private const string SnapshotMemoFile = WorldFolder + "SnapshotMemo.cs";
    private const string ContainerAwakeFile = WorldFolder + "ContainerAwakePatch.cs";
    private const string ZNetViewDestroyFile = WorldFolder + "ZNetViewOnDestroyPatch.cs";
    private const string StationStartFile = WorldFolder + "CraftingStationStartPatch.cs";
    private const string StationDestroyFile = WorldFolder + "CraftingStationOnDestroyPatch.cs";
    private const string SharedNamesFile = "src/Shared/PatchTargetNames.cs";

    private static readonly string[] PatchFiles = { ContainerAwakeFile, ZNetViewDestroyFile, StationStartFile, StationDestroyFile };

    private static readonly string[] WorldFileNames =
    {
        "ContainerAwakePatch.cs",
        "CraftingStationOnDestroyPatch.cs",
        "CraftingStationStartPatch.cs",
        "GameClock.cs",
        "GameSnapshotSource.cs",
        "LoadedRegistry.cs",
        "PluginWorld.cs",
        "RegistryKind.cs",
        "RegistryMessages.cs",
        "SnapshotMemo.cs",
        "ZNetViewOnDestroyPatch.cs",
    };

    private static readonly Lazy<Dictionary<string, string>> Plugin = new(PluginSourceScan.Sources);

    // GW-U-503
    [Fact]
    public void PluginWorldIsAnInternalSealedClassInTheWorldNamespace()
    {
        string code = Code(PluginWorldFile);

        Assert.Matches(@"namespace BaseStorage\.Plugin\.World;", code);
        Assert.Matches(@"internal\s+sealed\s+class\s+PluginWorld\b", code);
    }

    // GW-U-503
    [Fact]
    public void ContainerRegistryCallsTheCacheBeforeEachRegistryLine()
    {
        string region = RegistryRegion(Code(PluginWorldFile), "new LoadedRegistry<ZNetView, Container>");

        AssertEachEventIsFollowedByItsChange(region, new[] { "Cache.OnContainerPlaced()", "Cache.OnContainerRemoved()" }, "RegistryMessages.Changed(RegistryKind.Container");
    }

    // GW-U-503
    [Fact]
    public void OneBaseCacheIsBuiltOverTheSnapshotSource()
    {
        string code = Code(PluginWorldFile);

        Assert.Single(Regex.Matches(code, @"new\s+BaseCache\s*\("));
        Assert.Matches(@"new\s+BaseCache\s*\(\s*Snapshots\s*,\s*clock\s*\)", code);
    }

    // GW-U-503
    [Fact]
    public void OnContainerAwakeRegistersOnlyAValidViewAndNeverUsesNullConditional()
    {
        string body = PluginSourceScan.MethodBody(Code(PluginWorldFile), @"void\s+OnContainerAwake\s*\(");

        Assert.Contains("m_nview != null", body, StringComparison.Ordinal);
        Assert.Contains("IsValid()", body, StringComparison.Ordinal);
        Assert.Matches(@"Containers\.Add\(\s*\w+\.m_nview\s*,", body);
        Assert.DoesNotContain("?.", body, StringComparison.Ordinal);
    }

    // GW-U-503
    [Fact]
    public void OnNetViewDestroyOnlyRemovesFromTheContainerRegistry()
    {
        string body = PluginSourceScan.MethodBody(Code(PluginWorldFile), @"void\s+OnNetViewDestroy\s*\(");

        Assert.Matches(@"^Containers\.Remove\(", body.Trim());
    }

    // GW-U-504 and GW-U-508 (container patches)
    [Theory]
    [InlineData(ContainerAwakeFile, "ContainerAwakePatch", "Container", "Container.Awake", "Postfix", "OnContainerAwake")]
    [InlineData(ZNetViewDestroyFile, "ZNetViewOnDestroyPatch", "ZNetView", "ZNetView.OnDestroy", "Prefix", "OnNetViewDestroy")]
    public void ContainerPatchClassesUseTheConstantTargetsAndForward(
        string file,
        string className,
        string typeName,
        string target,
        string method,
        string forwarder)
    {
        AssertPatchClass(file, className, typeName, target, method, forwarder);
    }

    // GW-U-505
    [Fact]
    public void BootInitialisesTheWorldOnceBeforePatchesApply()
    {
        string entry = Code(PluginSourceScan.EntryPoint);
        Dictionary<string, string> sources = Plugin.Value;

        Assert.Equal(1, PluginSourceScan.CountMatches(sources, @"PluginWorld\.Initialize\(new GameClock\(\)\)"));
        Assert.Equal(new[] { PluginSourceScan.EntryPoint }, PluginSourceScan.FilesMatching(sources, @"PluginWorld\.Initialize\(new GameClock\(\)\)"));

        string boot = PluginSourceScan.MethodBody(entry, @"void\s+Boot\s*\(\s*\)");
        int settings = boot.IndexOf("Settings = new BaseStorageSettings(", StringComparison.Ordinal);
        int initialise = boot.IndexOf("PluginWorld.Initialize(new GameClock())", StringComparison.Ordinal);
        int apply = boot.IndexOf("PatchApplier.ApplyAll(", StringComparison.Ordinal);
        Assert.True(settings >= 0 && initialise > settings, "PluginWorld.Initialize must follow the Settings assignment in Boot().");
        Assert.True(apply > initialise, "PluginWorld.Initialize must come before PatchApplier.ApplyAll in Boot().");

        string update = PluginSourceScan.MethodBody(entry, @"void\s+Update\s*\(\s*\)");
        Assert.DoesNotContain("PluginWorld", update, StringComparison.Ordinal);
    }

    // GW-U-507
    [Fact]
    public void StationRegistryCallsTheCacheBeforeEachRegistryLine()
    {
        string region = RegistryRegion(Code(PluginWorldFile), "new LoadedRegistry<CraftingStation, CraftingStation>");

        AssertEachEventIsFollowedByItsChange(region, new[] { "Cache.OnStationPlaced()", "Cache.OnStationRemoved()" }, "RegistryMessages.Changed(RegistryKind.Station");
    }

    // GW-U-507
    [Fact]
    public void OnStationStartRegistersAValidStationWithABuildRangeAndWritesTheSummaryOnlyOnAddSuccess()
    {
        string body = PluginSourceScan.MethodBody(Code(PluginWorldFile), @"void\s+OnStationStart\s*\(");

        Assert.Contains("m_nview != null", body, StringComparison.Ordinal);
        Assert.Contains("IsValid()", body, StringComparison.Ordinal);
        Assert.Contains("m_rangeBuild > 0f", body, StringComparison.Ordinal);
        Assert.Matches(@"Stations\.Add\(\s*(\w+)\s*,\s*\1\s*\)", body);
        Assert.Matches(@"if\s*\(\s*Stations\.Add\(\s*(\w+)\s*,\s*\1\s*\)\s*\)\s*\{[\s\S]*?RegistryMessages\.BaseSummary\(", body);
    }

    // GW-U-507
    [Fact]
    public void OnStationDestroyOnlyRemovesFromTheStationRegistry()
    {
        string body = PluginSourceScan.MethodBody(Code(PluginWorldFile), @"void\s+OnStationDestroy\s*\(");

        Assert.Matches(@"^Stations\.Remove\(", body.Trim());
    }

    // GW-U-507
    [Fact]
    public void TheBuildRangeFieldIsUsedOnlyInPluginWorld()
    {
        Assert.Equal(new[] { PluginWorldFile }, PluginSourceScan.FilesMatching(Plugin.Value, @"m_rangeBuild"));
    }

    // GW-U-508 (station patches)
    [Theory]
    [InlineData(StationStartFile, "CraftingStationStartPatch", "CraftingStation", "CraftingStation.Start", "Postfix", "OnStationStart")]
    [InlineData(StationDestroyFile, "CraftingStationOnDestroyPatch", "CraftingStation", "CraftingStation.OnDestroy", "Prefix", "OnStationDestroy")]
    public void StationPatchClassesUseTheConstantTargetsAndForward(
        string file,
        string className,
        string typeName,
        string target,
        string method,
        string forwarder)
    {
        AssertPatchClass(file, className, typeName, target, method, forwarder);
    }

    // GW-U-512
    [Theory]
    [InlineData("GetCreator()")]
    [InlineData("GetComponentInParent<Piece>()")]
    [InlineData("m_wagon")]
    [InlineData("GetComponentInParent<Vagon>()")]
    [InlineData("GetComponentInParent<Ship>()")]
    [InlineData("GetComponent<TombStone>()")]
    [InlineData(".CheckAccess(")]
    [InlineData("m_checkGuardStone")]
    [InlineData("ZDOVars.s_inUse")]
    [InlineData("GetOwner()")]
    [InlineData("ZDOMan.GetSessionID()")]
    [InlineData("GetPlayerList()")]
    [InlineData("m_characterID")]
    [InlineData("m_currentContainer")]
    [InlineData("GetPlayerProfile()")]
    [InlineData("GetPlayerID()")]
    [InlineData("ContainerFlags.PlayerBuilt")]
    [InlineData("ContainerFlags.Vehicle")]
    [InlineData("ContainerFlags.Tombstone")]
    [InlineData("ContainerFlags.AccessDenied")]
    [InlineData("ContainerFlags.InUseByOther")]
    public void ContainerFlagSourcesArePresentInTheSnapshotAdapter(string token)
    {
        Assert.Contains(token, Code(SnapshotSourceFile), StringComparison.Ordinal);
    }

    // GW-U-512
    [Fact]
    public void PrivateAreaAccessIsCheckedWithoutFlashingAWard()
    {
        Assert.Matches(@"PrivateArea\.CheckAccess\([^;]*0f,\s*false,\s*false\)", Code(SnapshotSourceFile));
    }

    // GW-U-513
    [Theory]
    [InlineData("GetAllItems()")]
    [InlineData("GetWidth()")]
    [InlineData("GetHeight()")]
    [InlineData("m_dropPrefab")]
    [InlineData("m_stack")]
    [InlineData("m_quality")]
    [InlineData("m_shared")]
    [InlineData("m_maxStackSize")]
    [InlineData("new ItemStackSnapshot(")]
    [InlineData("new ContainerSnapshot(")]
    [InlineData("new ContainerId(")]
    [InlineData("m_uid")]
    [InlineData("IsNone")]
    [InlineData("float.IsNaN")]
    [InlineData("float.IsInfinity")]
    [InlineData("256")]
    public void ContainerContentsAndOmissionRulesArePresent(string token)
    {
        Assert.Contains(token, Code(SnapshotSourceFile), StringComparison.Ordinal);
    }

    // GW-U-513
    [Fact]
    public void TheAdapterNeverCatchesExceptionsForControlFlow()
    {
        string code = Code(SnapshotSourceFile);

        Assert.DoesNotMatch(@"\btry\b", code);
        Assert.DoesNotMatch(@"\bcatch\b", code);
    }

    // GW-U-514
    [Theory]
    [InlineData("SetInUse")]
    [InlineData("ClaimOwnership")]
    [InlineData("InvokeRPC")]
    [InlineData(".Set(")]
    [InlineData("OnContainerPlaced")]
    [InlineData("OnContainerRemoved")]
    [InlineData("OnStationPlaced")]
    [InlineData("OnStationRemoved")]
    public void TheSnapshotAdapterIsReadOnly(string forbidden)
    {
        Assert.DoesNotContain(forbidden, Code(SnapshotSourceFile), StringComparison.Ordinal);
    }

    // GW-U-514
    [Theory]
    [InlineData(SnapshotSourceFile)]
    [InlineData(PluginWorldFile)]
    [InlineData(GameClockFile)]
    public void NoNullHelpersReachUnityObjects(string file)
    {
        string code = Code(file);

        Assert.DoesNotMatch(@"\?\.", code);
        Assert.DoesNotMatch(@"\?\?", code);
        Assert.DoesNotMatch(@"\bis\s+null\b", code);
        Assert.DoesNotMatch(@"\bis\s+not\s+null\b", code);
    }

    // GW-U-514
    [Theory]
    [InlineData(ContainerAwakeFile)]
    [InlineData(ZNetViewDestroyFile)]
    [InlineData(StationStartFile)]
    [InlineData(StationDestroyFile)]
    public void EachPatchFileHasExactlyOneNullConditionalAndItIsOnPluginWorldCurrent(string file)
    {
        string code = Code(file);

        Assert.Single(Regex.Matches(code, @"\?\."));
        Assert.Single(Regex.Matches(code, @"PluginWorld\.Current\?\."));
    }

    // GW-U-516
    [Fact]
    public void StationMappingUsesTheEffectiveBuildRange()
    {
        string code = Code(SnapshotSourceFile);

        Assert.Contains("GetStationBuildRange()", code, StringComparison.Ordinal);
        Assert.Contains("new StationSnapshot(", code, StringComparison.Ordinal);
        Assert.Contains("new StationId(", code, StringComparison.Ordinal);
        Assert.Contains("transform.position", code, StringComparison.Ordinal);
        Assert.Contains("new WorldPosition(", code, StringComparison.Ordinal);
        Assert.DoesNotContain("m_rangeBuild", code, StringComparison.Ordinal);
    }

    // GW-U-521
    [Fact]
    public void NoWorldFileDeclaresAPerFrameMessage()
    {
        foreach (string file in WorldFiles())
        {
            Assert.DoesNotMatch(@"\b(Update|LateUpdate|FixedUpdate|OnGUI)\s*\(", Code(file));
        }
    }

    // GW-U-521
    [Fact]
    public void NoWorldFileIsAMonoBehaviourOrUsesLinq()
    {
        foreach (string file in WorldFiles())
        {
            string code = Code(file);
            Assert.DoesNotMatch(@"\bMonoBehaviour\b", code);
            Assert.DoesNotMatch(@"System\.Linq", code);
            Assert.DoesNotMatch(@"\.ToList\(", code);
            Assert.DoesNotMatch(@"\.ToArray\(", code);
        }
    }

    // GW-U-521
    [Theory]
    [InlineData(LoadedRegistryFile)]
    [InlineData(SnapshotMemoFile)]
    [InlineData(SnapshotSourceFile)]
    public void EveryBufferAllocationIsAStaticFieldInitialiser(string file)
    {
        string[] lines = Code(file).Split('\n');

        foreach (string line in lines.Where(l => l.Contains("new List<", StringComparison.Ordinal) || l.Contains("new Dictionary<", StringComparison.Ordinal)))
        {
            Assert.Matches(@"^\s*private\s+(static\s+)?readonly\b", line);
        }
    }

    // GW-U-521
    [Fact]
    public void TheSnapshotAdapterReadsTheNameOnceAndSortsWithACachedComparer()
    {
        string code = Code(SnapshotSourceFile);

        Assert.Single(Regex.Matches(code, @"\.name\b"));

        MatchCollection sorts = Regex.Matches(code, @"\.Sort\(([^)]*)\)");
        Assert.NotEmpty(sorts);
        foreach (Match sort in sorts)
        {
            string comparer = sort.Groups[1].Value.Trim();
            Assert.Matches(@"^\w+$", comparer);
            Assert.Matches($@"private\s+static\s+readonly\s+IComparer<[^>]+>\s+{comparer}\b", code);
        }

        Assert.DoesNotMatch(@"\.Sort\([^;]*=>", code);
        Assert.DoesNotMatch(@"\bComparison<", code);
    }

    // GW-U-521
    [Theory]
    [InlineData(ContainerAwakeFile)]
    [InlineData(ZNetViewDestroyFile)]
    [InlineData(StationStartFile)]
    [InlineData(StationDestroyFile)]
    public void EachPatchLambdaIsStatic(string file)
    {
        Assert.Matches(@"PatchSafety\.Run\(\s*PatchKey\s*,\s*__instance\s*,\s*static\s+\w+\s*=>", Code(file));
    }

    // GW-U-522
    [Fact]
    public void OnlyTheSnapshotAdapterBuildsFromTheGame()
    {
        string[] hits = PluginSourceScan.FilesMatching(Plugin.Value, @"\.GetStations\(|\.GetContainers\(");

        // Edited by S-E003-004 (ADR-GWIN-124): contract 5.3 has the two game-bound Crafting files read the world's snapshots.
        // Edited by S-E003-005 (contract requirement-display 5.3): GameRequirementDisplayWorld reads them too.
        // Edited by S-E003-006 (ADR-GWIN-132): contract build-from-base 5.3 has GameBuildGame read the world's snapshots too.
        // Edited by S-E004-015 (ADR-GWIN-143): GameDepositContainerLayer delegates GetContainers to the inner Organize layer (DB13); it reads no game state itself.
        string[] allowed = { SnapshotSourceFile, "src/BaseStorage.Plugin/Building/GameBuildGame.cs", "src/BaseStorage.Plugin/Crafting/GameCraftGame.cs", "src/BaseStorage.Plugin/Crafting/GameCraftWorld.cs", "src/BaseStorage.Plugin/Requirements/GameRequirementDisplayWorld.cs", "src/BaseStorage.Plugin/Deposit/GameDepositContainerLayer.cs" };
        Assert.DoesNotContain(hits, file => !allowed.Contains(file));
    }

    // GW-U-522
    [Fact]
    public void TheOnlyResolveIsTheDebugOnlySummaryInPluginWorld()
    {
        Dictionary<string, string> sources = Plugin.Value;

        // Edited by S-E003-004 (ADR-GWIN-124): the craft gate (G8) and the container lookup resolve the base in GameCraftGame (two calls).
        // Edited by S-E003-005 (contract requirement-display 5.3 B4, 4.2): the build tooltip resolves the base in GameRequirementDisplayWorld, and PluginRequirementDisplay calls the session's Resolve.
        // Edited by S-E003-006 (ADR-GWIN-132): the build gate resolves the base containing the player in GameBuildGame (one call).
        // Edited by integration (S-E003-005 + S-E003-006): the union, 5 + 1 = 6 calls.
        // Edited by S-E004-012 (ADR-GWIN-136): plus the station panel's SA2 call (StationActionsPanel), 6 + 1 = 7 calls.
        Assert.Equal(9, PluginSourceScan.CountMatches(sources, @"\.Resolve\("));
        Assert.Equal(new[] { "src/BaseStorage.Plugin/BaseMenu/BaseMenuPanel.cs", "src/BaseStorage.Plugin/Building/GameBuildGame.cs", "src/BaseStorage.Plugin/Crafting/GameCraftGame.cs", "src/BaseStorage.Plugin/Requirements/GameRequirementDisplayWorld.cs", "src/BaseStorage.Plugin/Requirements/PluginRequirementDisplay.cs", "src/BaseStorage.Plugin/StationActions/StationActionsPanel.cs", "src/BaseStorage.Plugin/Tooltips/ItemCategoryLine.cs", PluginWorldFile }, PluginSourceScan.FilesMatching(sources, @"\.Resolve\("));
        Assert.Matches(@"LogPolicy\.IsEnabled\([^)]*LogCategory\.Detail\)[\s\S]*\.Resolve\(", Code(PluginWorldFile));
    }

    // GW-U-522
    [Fact]
    public void BaseStoragePluginUpdatePollsTheFirstRunStepAndTicksReservations()
    {
        string update = PluginSourceScan.MethodBody(Code(PluginSourceScan.EntryPoint), @"void\s+Update\s*\(\s*\)");

        Assert.Equal(
            // Edited by S-E003-006, ADR-GWIN-132: PluginBuilding.Tick() sits immediately after PluginCrafting.Tick().
            // Edited by integration (S-E004-004 + S-E004-011): CategoryHotkey.Tick() then OrganizeRunner.Tick() follow ChestCategoryWriter.Tick().
            "PluginReservations.Tick();PluginCrafting.Tick();PluginBuilding.Tick();ChestCategoryWriter.Tick();CategoryHotkey.Tick();OrganizeRunner.Tick();if(firstRunCategoryFile!=null&&firstRunCategoryFile.Tick()){firstRunCategoryFile=null;}PluginVersionHandshake.Tick();",
            Regex.Replace(update, @"\s+", string.Empty));
    }

    // GW-U-523
    [Theory]
    [InlineData(ContainerAwakeFile)]
    [InlineData(ZNetViewDestroyFile)]
    [InlineData(StationStartFile)]
    [InlineData(StationDestroyFile)]
    public void PatchAttributesNameOnlyPatchTargetNamesConstants(string file)
    {
        string code = Code(file);
        MatchCollection attributes = Regex.Matches(code, @"\[HarmonyPatch\(\s*typeof\(\w+\)\s*,\s*([^\)\]]+)\)\]");

        Assert.Single(attributes);
        Assert.Matches(@"^PatchTargetNames\.\w+\.\w+$", attributes[0].Groups[1].Value.Trim());
        Assert.DoesNotMatch(@"\bTargetMethods?\b", code);
    }

    // GW-U-523
    [Fact]
    public void SharedPatchTargetNamesStaysLfOnlyWithoutAByteOrderMark()
    {
        byte[] bytes = File.ReadAllBytes(RepoRoot.Combine(SharedNamesFile));

        Assert.DoesNotContain((byte)'\r', bytes);
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "PatchTargetNames.cs has a byte-order mark.");
    }

    // GW-U-526
    [Theory]
    [InlineData(ContainerAwakeFile)]
    [InlineData(ZNetViewDestroyFile)]
    [InlineData(StationStartFile)]
    [InlineData(StationDestroyFile)]
    public void EachPatchBodyIsExactlyOnePatchSafetyRunStatement(string file)
    {
        string code = Code(file);

        Assert.Matches(
            @"private\s+static\s+void\s+(Postfix|Prefix)\s*\(\s*\w+\s+__instance\s*\)\s*=>\s*PatchSafety\.Run\s*\(\s*PatchKey\s*,\s*__instance\s*,\s*static\s+\w+\s*=>\s*PluginWorld\.Current\?\.\w+\(\s*\w+\s*\)\s*\)\s*;",
            code);
        Assert.DoesNotMatch(@"return\s+false", code);
        Assert.DoesNotMatch(@"bool\s+Prefix", code);
        Assert.DoesNotMatch(@"\btry\b", code);
    }

    // GW-U-527 (link half: green on arrival)
    [Fact]
    public void TheFourGameFreeFilesAreLinkedIntoCoreTestsAndNothingElseIsNotAProjectReference()
    {
        XDocument project = RepoRoot.LoadXml(ContractPaths.CoreTestsProject);
        string[] includes = RepoRoot.Named(project, "Compile").Select(e => RepoRoot.Attr(e, "Include")).ToArray();
        string[] links = RepoRoot.Named(project, "Compile").Select(e => RepoRoot.Attr(e, "Link")).ToArray();

        foreach (string name in new[] { "LoadedRegistry", "SnapshotMemo", "RegistryKind", "RegistryMessages" })
        {
            Assert.Contains($@"..\..\src\BaseStorage.Plugin\World\{name}.cs", includes);
            Assert.Contains($@"Linked\{name}.cs", links);
        }

        Assert.DoesNotContain(
            RepoRoot.Named(project, "ProjectReference").Select(e => RepoRoot.Attr(e, "Include")),
            include => include.Contains("BaseStorage.Plugin", StringComparison.Ordinal));
    }

    // GW-U-527
    [Fact]
    public void TheGameFreeFilesHaveNoUnityHarmonyBepInExOrValheimTypeNames()
    {
        var typeName = new Regex(@"(?<![.\w])(Container|ZNetView|CraftingStation|ZDO|Piece|Player|ZNet|GameObject)\b(?!\s*=(?!=))");
        foreach (string file in new[] { LoadedRegistryFile, SnapshotMemoFile, WorldFolder + "RegistryKind.cs", WorldFolder + "RegistryMessages.cs" })
        {
            string code = PluginSourceScan.StripComments(RepoRoot.ReadText(file));

            Assert.DoesNotMatch(@"using\s+UnityEngine", code);
            Assert.DoesNotMatch(@"using\s+HarmonyLib", code);
            Assert.DoesNotMatch(@"using\s+BepInEx", code);
            Assert.False(typeName.IsMatch(code), $"{file} names a game type.");
        }
    }

    // GW-U-527
    [Fact]
    public void TheGameBoundFilesAreNotLinkedIntoCoreTests()
    {
        XDocument project = RepoRoot.LoadXml(ContractPaths.CoreTestsProject);
        string[] includes = RepoRoot.Named(project, "Compile").Select(e => RepoRoot.Attr(e, "Include")).ToArray();

        foreach (string name in new[] { "GameClock", "GameSnapshotSource", "PluginWorld", "ContainerAwakePatch", "ZNetViewOnDestroyPatch", "CraftingStationStartPatch", "CraftingStationOnDestroyPatch" })
        {
            Assert.DoesNotContain(includes, include => include.Contains(name + ".cs", StringComparison.Ordinal));
        }
    }

    // GW-U-528
    [Fact]
    public void WorldFolderHoldsExactlyTheElevenContractFiles()
    {
        string[] names = WorldFiles().Select(file => Path.GetFileName(file)!).OrderBy(name => name, StringComparer.Ordinal).ToArray();

        Assert.Equal(WorldFileNames, names);
    }

    // GW-U-528
    [Fact]
    public void EachWorldFileStartsWithTheFileScopedNamespace()
    {
        foreach (string file in WorldFiles())
        {
            Assert.StartsWith("namespace BaseStorage.Plugin.World;", RepoRoot.ReadText(file).TrimStart());
        }
    }

    // GW-U-528
    [Fact]
    public void EachWorldFileDeclaresExactlyOneTopLevelTypeNamedAfterTheFile()
    {
        var violations = new List<string>();
        foreach (string file in WorldFiles())
        {
            string text = RepoRoot.ReadText(file);
            MatchCollection types = Regex.Matches(text, @"(?m)^(?:internal|public)\s+(?:(?:sealed|static|abstract|readonly|partial)\s+)*(?:class|struct|enum|interface|record)\s+(\w+)");
            string expected = Path.GetFileNameWithoutExtension(file);
            if (types.Count != 1 || types[0].Groups[1].Value != expected)
            {
                violations.Add($"{file}: {types.Count} top-level type(s), expected exactly {expected}.");
            }
        }

        Assert.Empty(violations);
    }

    // GW-U-528
    [Fact]
    public void EveryPublicOrInternalMemberHasASummaryAndEveryDocumentedMethodDocumentsItsParameters()
    {
        var violations = new List<string>();
        foreach (string file in WorldFiles())
        {
            violations.AddRange(DocumentationViolations(file, RepoRoot.ReadText(file)));
        }

        Assert.Empty(violations);
    }

    // GW-U-529
    [Fact]
    public void NoCoreFileMentionsTheWorldLayer()
    {
        string[] banned = { "BaseStorage.Plugin.World", "LoadedRegistry", "GameSnapshotSource" };
        var hits = new List<string>();
        string coreFolder = RepoRoot.Combine("src/BaseStorage.Core");
        foreach (string file in Directory.EnumerateFiles(coreFolder, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(RepoRoot.FullPath, file).Replace('\\', '/');
            if (RepoRoot.IsExcludedPath(relative))
            {
                continue;
            }

            string text = RepoRoot.Normalise(File.ReadAllText(file));
            if (banned.Any(word => text.Contains(word, StringComparison.Ordinal)))
            {
                hits.Add(relative);
            }
        }

        Assert.Empty(hits);
    }

    private static string Code(string relative)
    {
        Assert.True(Plugin.Value.TryGetValue(relative, out string? text), $"{relative} is missing from the Plugin sources.");
        return text!;
    }

    /// <summary>The World files the scan read. Every World-wide rule first requires all eleven contract files (ADR-GWIN-031), so it stays red until they land.</summary>
    private static string[] WorldFiles()
    {
        string[] files = Plugin.Value.Keys
            .Where(key => key.StartsWith(WorldFolder, StringComparison.Ordinal))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(WorldFileNames.Length, files.Length);
        return files;
    }

    private static string RegistryRegion(string code, string constructor)
    {
        int start = code.IndexOf(constructor, StringComparison.Ordinal);
        Assert.True(start >= 0, $"PluginWorld.cs does not contain {constructor}.");

        int next = code.IndexOf("new LoadedRegistry<", start + constructor.Length, StringComparison.Ordinal);
        return next < 0 ? code.Substring(start) : code.Substring(start, next - start);
    }

    private static void AssertEachEventIsFollowedByItsChange(string region, string[] events, string change)
    {
        foreach (string token in events)
        {
            Assert.NotEmpty(Indexes(region, token));
        }

        var marks = events.SelectMany(token => Indexes(region, token).Select(index => (Index: index, Token: token)))
            .Concat(Indexes(region, change).Select(index => (Index: index, Token: change)))
            .OrderBy(mark => mark.Index)
            .ToList();

        bool pending = false;
        int changes = 0;
        foreach (var mark in marks)
        {
            if (mark.Token == change)
            {
                Assert.True(pending, $"{change} at {mark.Index} is not preceded by its cache event.");
                pending = false;
                changes++;
            }
            else
            {
                Assert.False(pending, $"{mark.Token} at {mark.Index} comes before the previous cache event's registry line.");
                pending = true;
            }
        }

        Assert.False(pending, "A cache event is not followed by its registry line.");
        Assert.Equal(events.Sum(token => Indexes(region, token).Count), changes);
    }

    private static List<int> Indexes(string text, string token)
    {
        var found = new List<int>();
        for (int index = text.IndexOf(token, StringComparison.Ordinal); index >= 0; index = text.IndexOf(token, index + token.Length, StringComparison.Ordinal))
        {
            found.Add(index);
        }

        return found;
    }

    private static void AssertPatchClass(string file, string className, string typeName, string target, string method, string forwarder)
    {
        string code = Code(file);
        string key = "BaseStorage.Plugin.World." + className + "." + method;

        Assert.Matches($@"\[HarmonyPatch\(typeof\({typeName}\),\s*PatchTargetNames\.{Regex.Escape(target)}\)\]", code);
        Assert.Matches($@"internal\s+static\s+class\s+{className}\b", code);
        Assert.Contains($"internal const string PatchKey = \"{key}\";", code, StringComparison.Ordinal);
        Assert.Matches($@"private\s+static\s+void\s+{method}\s*\(\s*{typeName}\s+__instance\s*\)", code);
        Assert.Contains("." + forwarder + "(", code, StringComparison.Ordinal);
    }

    private static List<string> DocumentationViolations(string file, string text)
    {
        var violations = new List<string>();
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (!Regex.IsMatch(line, @"^    (internal|public|private)\s"))
            {
                continue;
            }

            bool exposed = Regex.IsMatch(line, @"^    (internal|public)\s");
            string[] docs = DocBlockAbove(lines, i);
            if (docs.Length == 0)
            {
                if (exposed)
                {
                    violations.Add($"{file}:{i + 1} has no /// summary block.");
                }

                continue;
            }

            if (!docs.Any(d => d.Contains("<summary>", StringComparison.Ordinal)))
            {
                violations.Add($"{file}:{i + 1} has a doc block without a summary.");
            }

            foreach (string parameter in ParameterNames(lines, i))
            {
                if (!docs.Any(d => d.Contains($"<param name=\"{parameter}\">", StringComparison.Ordinal)))
                {
                    violations.Add($"{file}:{i + 1} documents no <param> for '{parameter}'.");
                }
            }
        }

        return violations;
    }

    private static string[] DocBlockAbove(string[] lines, int index)
    {
        int j = index - 1;
        while (j >= 0 && lines[j].TrimStart().StartsWith("[", StringComparison.Ordinal))
        {
            j--;
        }

        var docs = new List<string>();
        while (j >= 0 && lines[j].TrimStart().StartsWith("///", StringComparison.Ordinal))
        {
            docs.Insert(0, lines[j].Trim());
            j--;
        }

        return docs.ToArray();
    }

    private static string[] ParameterNames(string[] lines, int index)
    {
        var signature = new System.Text.StringBuilder();
        for (int j = index; j < lines.Length; j++)
        {
            signature.Append(lines[j].Trim()).Append(' ');
            if (lines[j].Contains(')'))
            {
                break;
            }
        }

        string sig = signature.ToString();
        int open = sig.IndexOf('(');
        int arrow = sig.IndexOf("=>", StringComparison.Ordinal);
        int equals = sig.IndexOf('=');
        bool isProperty = arrow >= 0 && arrow < open;
        bool isField = equals >= 0 && equals < open;
        if (open < 0 || isProperty || isField)
        {
            return Array.Empty<string>();
        }

        int close = MatchingClose(sig, open);
        if (close < 0)
        {
            return Array.Empty<string>();
        }

        return SplitTopLevel(sig.Substring(open + 1, close - open - 1))
            .Select(piece => Regex.Match(piece, @"(\w+)\s*(=.*)?$"))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .ToArray();
    }

    private static int MatchingClose(string text, int open)
    {
        int depth = 0;
        for (int i = open; i < text.Length; i++)
        {
            if (text[i] == '(')
            {
                depth++;
            }
            else if (text[i] == ')')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static List<string> SplitTopLevel(string parameters)
    {
        var pieces = new List<string>();
        int depth = 0;
        int start = 0;
        for (int i = 0; i < parameters.Length; i++)
        {
            char c = parameters[i];
            if (c is '<' or '(')
            {
                depth++;
            }
            else if (c is '>' or ')')
            {
                depth--;
            }
            else if (c == ',' && depth == 0)
            {
                pieces.Add(parameters.Substring(start, i - start).Trim());
                start = i + 1;
            }
        }

        string last = parameters.Substring(start).Trim();
        if (last.Length > 0)
        {
            pieces.Add(last);
        }

        return pieces;
    }
}
