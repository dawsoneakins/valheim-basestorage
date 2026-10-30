namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-7, AC-8 and AC-9 (ADR-GWIN-028, contract F1-F9): text scans of the Plugin sources for the first-run step. The ObjectDB read and the
/// Update hook need the game, so they are checked by these scans and by docs/PLAYTEST.md. Sources are comment-stripped and CRLF-normalised.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginCategoryFileSourceScanTests
{
    private const string CategoriesPrefix = "src/BaseStorage.Plugin/Categories/";
    private const string FirstRunFile = "src/BaseStorage.Plugin/Categories/FirstRunCategoryFile.cs";
    private const string ObjectDbFile = "src/BaseStorage.Plugin/Categories/ObjectDbCategoryItems.cs";
    private const string AdapterFile = "src/BaseStorage.Plugin/Categories/ItemKindAdapter.cs";
    private const string WriterFile = "src/BaseStorage.Plugin/Categories/DefaultCategoryFileWriter.cs";
    private const string DefaultsTestFolder = "tests/BaseStorage.Core.Tests/Categories/Defaults";
    private const string WriterTestFile = "tests/BaseStorage.Core.Tests/PluginHelper/DefaultCategoryFileWriterTests.cs";

    private static readonly string[] GameItemTypes =
    {
        "Consumable", "Trophy", "OneHandedWeapon", "TwoHandedWeapon", "TwoHandedWeaponLeft", "Bow", "Torch",
        "Helmet", "Chest", "Legs", "Shoulder", "Utility", "Tool", "Shield", "Ammo",
        "m_food", "m_foodStamina", "m_foodEitr",
    };

    private static readonly string[] JoinPatterns =
    {
        @"\+\s*Path\.(?:Alt)?DirectorySeparatorChar",
        @"Path\.(?:Alt)?DirectorySeparatorChar\s*\+",
        @"\+\s*""\\\\""",
        @"""\\\\""\s*\+",
        @"\+\s*""/""",
        @"""/""\s*\+",
        @"\$""[^""]*\{[^}]*\}(?:\\\\|/)",
    };

    // GW-U-468 (1)
    [Fact]
    public void EntryPointHoldsTheFirstRunFieldTheUpdatePollAndTheBootStepLast()
    {
        string entry = PluginSourceScan.Sources()[PluginSourceScan.EntryPoint];
        Assert.Matches(@"private\s+FirstRunCategoryFile\?\s+firstRunCategoryFile\b", entry);

        string update = PluginSourceScan.MethodBody(entry, @"private\s+void\s+Update\s*\(\s*\)");
        Assert.Matches(@"\.Tick\s*\(\s*\)", update);
        Assert.Matches(@"firstRunCategoryFile\s*=\s*null\s*;", update);

        string boot = PluginSourceScan.MethodBody(entry, @"void\s+Boot\s*\(\s*\)");
        Assert.Matches(
            @"new\s+FirstRunCategoryFile\s*\(\s*Paths\.ConfigPath\s*,\s*sink\s*\)",
            boot);
        int apply = boot.IndexOf("PatchApplier.ApplyAll(", StringComparison.Ordinal);
        int create = boot.IndexOf("new FirstRunCategoryFile(", StringComparison.Ordinal);
        Assert.True(apply >= 0 && create > apply, "The first-run step is not created after PatchApplier.ApplyAll(.");

        string lastStatement = boot.Trim().Split('\n').Last().Trim();
        Assert.Contains("BootMessages.Loaded", lastStatement);
    }

    // GW-U-468 (2); S-E004-004 (ADR-GWIN-107) adds the second read, in the same Boot method, for the runtime catalog.
    [Fact]
    public void PathsConfigPathOccursExactlyTwiceInBootAndNowhereElse()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();

        Assert.Equal(2, PluginSourceScan.CountMatches(sources, @"Paths\.ConfigPath\b"));
        Assert.Equal(new[] { PluginSourceScan.EntryPoint }, PluginSourceScan.FilesMatching(sources, @"Paths\.ConfigPath\b"));
        string boot = PluginSourceScan.MethodBody(sources[PluginSourceScan.EntryPoint], @"void\s+Boot\s*\(\s*\)");
        Assert.Equal(2, Regex.Matches(boot, @"Paths\.ConfigPath\b").Count);
    }

    // GW-U-468 (3)
    [Fact]
    public void FirstRunStepSetsDoneBeforeWritingAndReadsTheObjectDb()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        Assert.True(sources.ContainsKey(FirstRunFile), $"{FirstRunFile} is missing.");

        string text = sources[FirstRunFile];
        int done = text.IndexOf("done = true", StringComparison.Ordinal);
        int write = text.IndexOf("WriteIfMissing(", StringComparison.Ordinal);
        Assert.True(done >= 0 && write > done, "done = true must precede WriteIfMissing(.");
        Assert.Contains("ObjectDB.instance", text);
        Assert.Contains("m_items", text);
    }

    // GW-U-468 (4)
    [Fact]
    public void ObjectDbCollectorUsesTheItemDropComponentValidationAndTheAdapter()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        Assert.True(sources.ContainsKey(ObjectDbFile), $"{ObjectDbFile} is missing.");

        string text = sources[ObjectDbFile];
        Assert.Contains("GetComponent<ItemDrop>", text);
        Assert.Contains("CategoryItem.IsValidPrefabName(", text);
        Assert.Contains("ItemKindAdapter.From(", text);
    }

    // GW-U-468 (5)
    [Fact]
    public void ItemKindAdapterNamesEveryGameTypeOfTheContractTable()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        Assert.True(sources.ContainsKey(AdapterFile), $"{AdapterFile} is missing.");

        string text = sources[AdapterFile];
        foreach (string gameType in GameItemTypes)
        {
            Assert.Matches(@"\b" + gameType + @"\b", text);
        }
    }

    // GW-U-468 (6)
    [Fact]
    public void CategoryTextIsBuiltOnlyByTheGeneratorAndNotInThePlugin()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        Assert.Equal(new[] { WriterFile }, PluginSourceScan.FilesMatching(sources, @"DefaultCategoryFile\.Generate\("));
        Assert.Equal(1, PluginSourceScan.CountMatches(sources, @"DefaultCategoryFile\.Generate\("));

        foreach (var pair in sources.Where(entry => entry.Key.StartsWith(CategoriesPrefix, StringComparison.Ordinal)))
        {
            Assert.DoesNotContain("StringBuilder", pair.Value);
            Assert.DoesNotContain("\": \"", pair.Value);
            Assert.DoesNotContain("# BaseStorage", pair.Value);
        }
    }

    // GW-U-468 (7)
    [Fact]
    public void NoNewHarmonyPatchClassIsAdded()
    {
        // Edited by S-E003-004 (ADR-GWIN-124): plus the three Crafting prefixes.
        // S-E004-002 intent, narrowed by S-E002-004 (contract 7.2), widened by S-E002-005 (ADR-GWIN-032), S-E004-014 (ADR-GWIN-051), S-E005-002 and
        // S-E004-004 (ADR-GWIN-104): the Harmony patch files are the Category button postfix, the two Favourites patches, the two Hover postfixes,
        // the two Reservations prefixes and the four World patches. None is under Categories/.
        // Edited by integration: plus the S-E003-004 Crafting, S-E003-005 Requirements and S-E003-006 Building patches and the S-E006-002 Networking postfixes.
        string[] expected =
        {
            "src/BaseStorage.Plugin/Building/PlayerHaveRequirementsPiecePatch.cs",
            "src/BaseStorage.Plugin/Building/PlayerPlacePiecePatch.cs",
            "src/BaseStorage.Plugin/Building/PlayerTryPlacePiecePatch.cs",
            "src/BaseStorage.Plugin/CategoryButton/InventoryGuiUpdateContainerPatch.cs",
            // Edited by S-E010-002 (organize-console-command 6.2): the console registration patch file.
            "src/BaseStorage.Plugin/Commands/TerminalInitTerminalPatch.cs",
            "src/BaseStorage.Plugin/Crafting/InventoryGuiDoCraftingPatch.cs",
            "src/BaseStorage.Plugin/Crafting/PlayerConsumeResourcesPatch.cs",
            "src/BaseStorage.Plugin/Crafting/PlayerHaveRequirementItemsPatch.cs",
            "src/BaseStorage.Plugin/Favourites/InventoryGridOnLeftDownPatch.cs",
            "src/BaseStorage.Plugin/Favourites/InventoryGridUpdateGuiPatch.cs",
            "src/BaseStorage.Plugin/Hover/ContainerHoverTextPatch.cs",
            "src/BaseStorage.Plugin/Hover/CraftingStationHoverTextPatch.cs",
            "src/BaseStorage.Plugin/Networking/FejdStartupShowConnectErrorPatch.cs",
            "src/BaseStorage.Plugin/Networking/ZNetOnNewConnectionPatch.cs",
            // Edited by S-E003-005, ADR-GWIN-127: plus the three Requirements display patch files.
            "src/BaseStorage.Plugin/Requirements/HudSetupPieceInfoPatch.cs",
            "src/BaseStorage.Plugin/Requirements/InventoryGuiSetupRequirementListPatch.cs",
            "src/BaseStorage.Plugin/Requirements/InventoryGuiSetupRequirementPatch.cs",
            "src/BaseStorage.Plugin/Reservations/ContainerInteractPatch.cs",
            "src/BaseStorage.Plugin/Reservations/ContainerOpenResponsePatch.cs",
            // Edited by S-E004-012 (ADR-GWIN-136): plus the StationActions InventoryGuiUpdateRecipe postfix.
            "src/BaseStorage.Plugin/StationActions/InventoryGuiUpdateRecipePatch.cs",
            "src/BaseStorage.Plugin/Tooltips/ItemDataGetTooltipPatch.cs",
            "src/BaseStorage.Plugin/World/ContainerAwakePatch.cs",
            "src/BaseStorage.Plugin/World/CraftingStationOnDestroyPatch.cs",
            "src/BaseStorage.Plugin/World/CraftingStationStartPatch.cs",
            "src/BaseStorage.Plugin/World/ZNetViewOnDestroyPatch.cs",
        };
        string[] files = PluginSourceScan.FilesMatching(PluginSourceScan.Sources(), @"\[HarmonyPatch");

        Assert.Equal(expected, files);
        Assert.DoesNotContain(files, file => file.Contains("/Categories/", StringComparison.Ordinal));
    }

    // GW-U-469
    [Fact]
    public void FileModeCreateNewOccursExactlyOnceAndOnlyInTheWriter()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        Assert.Equal(1, PluginSourceScan.CountMatches(sources, @"FileMode\.CreateNew\b"));
        Assert.Equal(new[] { WriterFile }, PluginSourceScan.FilesMatching(sources, @"FileMode\.CreateNew\b"));
    }

    // GW-U-469
    [Fact]
    public void NoOverwritingFileApiAppearsInPluginSources()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        string[] forbidden =
        {
            @"File\.WriteAllText\b", @"File\.WriteAllBytes\b", @"File\.WriteAllLines\b", @"File\.AppendAllText\b",
            @"File\.Create\(", @"File\.Open\b", @"File\.Move\b", @"File\.Replace\b", @"File\.Copy\b",
            @"FileMode\.Create\b", @"FileMode\.OpenOrCreate\b", @"FileMode\.Truncate\b", @"FileMode\.Append\b",
        };

        foreach (string pattern in forbidden)
        {
            int count = PluginSourceScan.CountMatches(sources, pattern);
            Assert.True(count == 0, $"{pattern} occurs {count} time(s) in the Plugin sources.");
        }
    }

    // GW-U-469
    [Fact]
    public void FileDeleteOccursOnceInTheWriterCatchGuardedByIncomplete()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        Assert.Equal(1, PluginSourceScan.CountMatches(sources, @"File\.Delete\("));
        Assert.Equal(new[] { WriterFile }, PluginSourceScan.FilesMatching(sources, @"File\.Delete\("));

        string body = PluginSourceScan.MethodBody(
            sources[WriterFile],
            @"internal\s+static\s+void\s+WriteIfMissing\s*\(\s*string\s+configFolder\s*,\s*IReadOnlyCollection<CategoryItem>\s+items\s*,\s*ILogSink\s+sink\s*,\s*Action<Stream,\s*byte\[\]>\s+writeBytes\s*\)");
        Assert.Matches(@"catch\s*\(\s*Exception\s+\w+\s*\)[\s\S]*if\s*\(\s*incomplete\s*\)[\s\S]*File\.Delete\(", body);
    }

    // GW-U-469
    [Fact]
    public void WriteIfMissingChecksExistenceBeforeGeneratingBeforeCreatingTheFolderBeforeCreateNew()
    {
        string body = PluginSourceScan.MethodBody(
            PluginSourceScan.Sources()[WriterFile],
            @"internal\s+static\s+void\s+WriteIfMissing\s*\(\s*string\s+configFolder\s*,\s*IReadOnlyCollection<CategoryItem>\s+items\s*,\s*ILogSink\s+sink\s*,\s*Action<Stream,\s*byte\[\]>\s+writeBytes\s*\)");

        int exists = body.IndexOf("File.Exists(", StringComparison.Ordinal);
        int generate = body.IndexOf("Generate(", StringComparison.Ordinal);
        int createDirectory = body.IndexOf("Directory.CreateDirectory(", StringComparison.Ordinal);
        int createNew = body.IndexOf("FileMode.CreateNew", StringComparison.Ordinal);
        Assert.True(
            exists >= 0 && generate > exists && createDirectory > generate && createNew > createDirectory,
            "WriteIfMissing must check File.Exists, then Generate, then Directory.CreateDirectory, then FileMode.CreateNew.");
    }

    // GW-U-470
    [Fact]
    public void FileNameIsOnlyTheSecondArgumentOfPathCombineInPathFor()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        Assert.Equal(new[] { WriterFile }, PluginSourceScan.FilesMatching(sources, @"DefaultCategoryFile\.FileName\b"));
        Assert.Equal(1, PluginSourceScan.CountMatches(sources, @"DefaultCategoryFile\.FileName\b"));
        Assert.Equal(1, PluginSourceScan.CountMatches(sources, @"Path\.Combine\(\s*[^,()]+,\s*DefaultCategoryFile\.FileName\s*\)"));
    }

    // GW-U-470
    [Fact]
    public void NoPluginSourceHardCodesTheFileNameLiteral()
    {
        Assert.Equal(0, PluginSourceScan.CountMatches(PluginSourceScan.Sources(), Regex.Escape("\"BaseStorage.categories.txt\"")));
    }

    // GW-U-470
    [Fact]
    public void CategoryFilesAndNewTestFilesDoNotJoinPathParts()
    {
        var scanned = new List<string>();
        scanned.AddRange(PluginSourceScan.Sources().Keys.Where(key => key.StartsWith(CategoriesPrefix, StringComparison.Ordinal)));
        scanned.AddRange(Directory.GetFiles(RepoRoot.Combine(DefaultsTestFolder), "*.cs")
            .Select(file => Path.Combine(DefaultsTestFolder, Path.GetFileName(file)!)));
        scanned.Add(WriterTestFile);

        foreach (string relative in scanned)
        {
            string text = PluginSourceScan.StripComments(RepoRoot.ReadText(relative));
            foreach (string pattern in JoinPatterns)
            {
                Assert.False(Regex.IsMatch(text, pattern), $"{relative} joins path parts with {pattern}.");
            }
        }
    }

    // GW-U-471
    [Fact]
    public void CoreTestsLinkTheWriterAndReferenceNoPlugin()
    {
        string csproj = RepoRoot.ReadText(ContractPaths.CoreTestsProject);
        Assert.Matches(@"<Compile\s+Include=""[^""]*src[\\/]BaseStorage\.Plugin[\\/]Categories[\\/]DefaultCategoryFileWriter\.cs""", csproj);
        Assert.DoesNotMatch(@"<ProjectReference[^>]*BaseStorage\.Plugin", csproj);
    }

    // GW-U-471
    [Fact]
    public void WriterImportsOnlyBclAndCoreAndIsInternalStaticInItsNamespace()
    {
        string writer = RepoRoot.ReadText(WriterFile);
        string[] usings = Regex.Matches(writer, @"^using\s+([\w.]+)\s*;", RegexOptions.Multiline)
            .Select(match => match.Groups[1].Value)
            .ToArray();

        Assert.NotEmpty(usings);
        foreach (string import in usings)
        {
            bool allowed = import is "System" or "System.Collections.Generic" or "System.IO" or "System.Text"
                || import.StartsWith("BaseStorage.Core.", StringComparison.Ordinal);
            Assert.True(allowed, $"The writer imports {import}, which is not BCL or Core.");
        }

        Assert.Matches(@"namespace\s+BaseStorage\.Plugin\.Categories\s*;", writer);
        Assert.Matches(@"internal\s+static\s+class\s+DefaultCategoryFileWriter\b", writer);
    }
}
