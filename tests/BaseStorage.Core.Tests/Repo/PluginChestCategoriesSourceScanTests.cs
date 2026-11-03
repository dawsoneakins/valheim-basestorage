namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;
using BaseStorage.Core.Tests.Ui;

/// <summary>
/// GW-U-1231..1234 (contract S-E004-003 section 7.2, ADR-GWIN-097, ADR-GWIN-098): the chest category key has exactly one writer,
/// Auto is the empty string and no key is ever removed, the game-free driver files are game-free and linked, the writer is wired
/// into Boot and Update behind the fail-closed wrapper, and no file in the folder patches, waits or carries user-facing text.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginChestCategoriesSourceScanTests
{
    private const string Folder = "src/BaseStorage.Plugin/ChestCategories/";
    private const string Port = Folder + "IChestCategoryStorage.cs";
    private const string Driver = Folder + "ChestCategoryWriteDriver.cs";
    private const string Storage = Folder + "GameChestCategoryStorage.cs";
    private const string Writer = Folder + "ChestCategoryWriter.cs";
    private const string HoverLines = "src/BaseStorage.Plugin/Hover/HoverLines.cs";

    private static readonly string[] GameFreeFiles = { Port, Driver };
    private static readonly string[] GameBoundFiles = { Storage, Writer };

    // GW-U-1231: the folder holds exactly the four contracted files.
    [Fact]
    public void TheFolderHoldsExactlyTheFourFiles()
    {
        Assert.Equal(new[] { Driver, Writer, Storage, Port }, FolderFiles(Sources()));
    }

    // GW-U-1231 (W1): the two game-free files import no game namespace and name no game type.
    [Fact]
    public void GameFreeFilesNameNoGameType()
    {
        Dictionary<string, string> sources = Sources();
        var offenders = new List<string>();
        foreach (string file in GameFreeFiles)
        {
            string code = sources[file];
            foreach (string token in new[] { "using UnityEngine", "using HarmonyLib", "using BepInEx" })
            {
                if (code.Contains(token, StringComparison.Ordinal))
                {
                    offenders.Add(file + " has " + token);
                }
            }

            foreach (string word in new[] { "ZNetView", "ZDO", "ZNet", "InventoryGui", "MessageHud", "Notices", "PatchSafety" })
            {
                if (Regex.IsMatch(code, @"\b" + word + @"\b"))
                {
                    offenders.Add(file + " names " + word);
                }
            }

            if (Regex.IsMatch(code, @"\bContainer\b(?!Id)"))
            {
                offenders.Add(file + " names Container");
            }
        }

        Assert.Empty(offenders);
    }

    // GW-U-1231: Core.Tests links exactly the two game-free files of the folder and none of the game-bound ones.
    [Fact]
    public void CoreTestsLinkExactlyTheTwoGameFreeFiles()
    {
        string[] links = RepoRoot.Named(RepoRoot.LoadXml(ContractPaths.CoreTestsProject), "Compile")
            .Select(e => RepoRoot.Attr(e, "Include").Replace('\\', '/'))
            .Where(include => include.Contains("/ChestCategories/", StringComparison.Ordinal))
            .OrderBy(include => include, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "../../src/BaseStorage.Plugin/ChestCategories/ChestCategoryWriteDriver.cs",
                "../../src/BaseStorage.Plugin/ChestCategories/IChestCategoryStorage.cs",
            },
            links);
    }

    // GW-U-1232 (AC-1, ADR-GWIN-097): the key is written in exactly one place, by the string Set overload, and never removed.
    [Fact]
    public void TheKeyHasExactlyOneWriterAndIsNeverRemoved()
    {
        Dictionary<string, string> sources = Sources();

        Assert.Equal(new[] { Storage }, PluginSourceScan.FilesMatching(sources, @"\.Set\(\s*PersistenceKeys\.ChestCategory\b"));
        Assert.Equal(1, PluginSourceScan.CountMatches(sources, @"\.Set\(\s*PersistenceKeys\.ChestCategory\b"));
        Assert.Contains("zdo.Set(PersistenceKeys.ChestCategory, value);", sources[Storage], StringComparison.Ordinal);
        Assert.Equal(new[] { "src/BaseStorage.Plugin/Feeding/FermenterFeedTarget.cs" }, PluginSourceScan.FilesMatching(sources, @"RemoveString|GetStableHashCode|ZDOExtraData")); // The game's fermenter call takes the item's hash; no key is touched.
        Assert.Equal(new[] { Storage, HoverLines }, PluginSourceScan.FilesMatching(sources, @"PersistenceKeys\.ChestCategory\b").OrderBy(f => f == Storage ? 0 : 1).ToArray());
        Assert.Contains("GetString(PersistenceKeys.ChestCategory, string.Empty)", sources[Storage], StringComparison.Ordinal);
        Assert.Contains("GetString(PersistenceKeys.ChestCategory, string.Empty)", sources[HoverLines], StringComparison.Ordinal);
    }

    // GW-U-1232: the Auto value written is the reader's StoredValue (the empty string), never a literal of the driver's own.
    [Fact]
    public void TheDriverWritesOnlyTheReaderStoredValueBehindTheWriteGate()
    {
        string driver = Sources()[Driver];

        Assert.Single(Regex.Matches(driver, @"storage\.Write\("));
        Assert.Matches(@"reservations\.CanWrite\(id\)\s*&&\s*storage\.Write\(entry\.Chest,\s*ChestCategoryReader\.StoredValue\(", driver);
        Assert.Contains("reservations.BeginWrite(id);", driver, StringComparison.Ordinal);
        Assert.Equal(3, Regex.Matches(driver, @"reservations\.Release\(id\);").Count);
        Assert.Contains("internal const string Operation = \"category\";", driver, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(driver, "\"category\""));
    }

    // GW-U-1233 (contract 4.3): Boot creates the writer after the reservations and before the patches; Update ticks it right after the reservations.
    [Fact]
    public void BootAndUpdateWireTheWriter()
    {
        string entry = Sources()[PluginSourceScan.EntryPoint];
        string boot = PluginSourceScan.MethodBody(entry, @"void\s+Boot\s*\(\s*\)");
        string update = Regex.Replace(PluginSourceScan.MethodBody(entry, @"void\s+Update\s*\(\s*\)"), @"\s+", string.Empty);

        Assert.Single(Regex.Matches(boot, Regex.Escape("ChestCategoryWriter.Initialize(reservations, new GameClock(), Settings);")));
        int reservations = boot.IndexOf("PluginReservations.Initialize(", StringComparison.Ordinal);
        int writer = boot.IndexOf("ChestCategoryWriter.Initialize(", StringComparison.Ordinal);
        int apply = boot.IndexOf("PatchApplier.ApplyAll(", StringComparison.Ordinal);
        Assert.True(reservations >= 0 && reservations < writer && writer < apply, "Boot order must be PluginReservations.Initialize, ChestCategoryWriter.Initialize, PatchApplier.ApplyAll.");
        // Edited by S-E003-006, ADR-GWIN-132: PluginBuilding.Tick() sits immediately after PluginCrafting.Tick().
        Assert.StartsWith("PluginReservations.Tick();PluginCrafting.Tick();PluginBuilding.Tick();ChestCategoryWriter.Tick();", update, StringComparison.Ordinal);
    }

    // GW-U-1233: the writer is an internal static class with the contracted entry points, the wrapped tick and the one timeout read.
    [Fact]
    public void TheWriterHasTheContractedShape()
    {
        Dictionary<string, string> sources = Sources();
        string writer = sources[Writer];

        Assert.Matches(@"(?m)^namespace BaseStorage\.Plugin\.ChestCategories;", writer);
        Assert.Matches(@"internal\s+static\s+class\s+ChestCategoryWriter\b", writer);
        Assert.Matches(@"internal\s+static\s+ChestCategoryWriteDriver<Container>\?\s+Current\s*\{\s*get;\s*private\s+set;\s*\}", writer);
        Assert.Matches(@"internal\s+static\s+CategoryWriteStatus\s+TrySet\(Container chest, CategoryAssignment\? desired\)", writer);
        Assert.Matches(@"internal\s+static\s+CategoryAssignment\?\s+Read\(Container chest\)", writer);
        Assert.Contains("internal const string TickKey = \"BaseStorage.Plugin.ChestCategories.ChestCategoryWriter.Tick\";", writer, StringComparison.Ordinal);
        Assert.Matches(@"PatchSafety\.Run\(TickKey,\s*static\s*\(\)\s*=>\s*Current\?\.Tick\(\)\);", writer);
        Assert.Single(Regex.Matches(writer, Regex.Escape("settings.ReserveTimeoutMs")));
        Assert.Equal(new[] { Writer }, FolderFiles(sources).Where(file => sources[file].Contains("ReserveTimeoutMs", StringComparison.Ordinal)).ToArray());
        Assert.Equal(new[] { Writer }, FolderFiles(sources).Where(file => sources[file].Contains("Notices.Show(", StringComparison.Ordinal)).ToArray());
        Assert.Matches(@"Notices\.Show\(NoticePlacement\.TopLeft,\s*CategoryWriteRules\.NoticeKey\(", writer);
        Assert.Matches(@"Notices\.Show\(NoticePlacement\.Center,\s*CategoryWriteRules\.NoticeKey\(", writer);
    }

    // GW-U-1234: nothing in the folder patches, runs per frame on its own, waits, or carries strings-table text.
    [Fact]
    public void NothingInTheFolderPatchesWaitsOrCarriesUserFacingText()
    {
        Dictionary<string, string> sources = Sources();
        var offenders = new List<string>();
        foreach (string file in FolderFiles(sources))
        {
            string code = sources[file];
            foreach (string token in new[] { "[HarmonyPatch", "Thread", "Task.", "Sleep(", "DateTime", "Stopwatch", "SetOwner", "ClaimOwnership", "InvokeRPC", "SetInUse", ".ShowMessage(" })
            {
                if (code.Contains(token, StringComparison.Ordinal))
                {
                    offenders.Add(file + " contains " + token);
                }
            }

            if (Regex.IsMatch(code, @"\b(void|bool)\s+(Update|LateUpdate|FixedUpdate|OnGUI)\s*\(") || code.Contains("MonoBehaviour", StringComparison.Ordinal))
            {
                offenders.Add(file + " runs per frame on its own");
            }

            foreach (Match literal in Regex.Matches(code, "\"(?:[^\"\\\\]|\\\\.)*\""))
            {
                string inner = literal.Value.Substring(1, literal.Value.Length - 2);
                foreach ((string _, string english) in UiFixtures.EnglishTable)
                {
                    if (inner == english || (english.Length >= 8 && inner.Contains(english, StringComparison.Ordinal)))
                    {
                        offenders.Add(file + ": " + literal.Value);
                    }
                }
            }
        }

        Assert.Empty(offenders);
    }

    private static Dictionary<string, string> Sources()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        foreach (string file in GameFreeFiles.Concat(GameBoundFiles).Append(HoverLines))
        {
            Assert.True(sources.ContainsKey(file), "Scan did not read " + file);
        }

        return sources;
    }

    private static string[] FolderFiles(Dictionary<string, string> sources) =>
        sources.Keys.Where(key => key.StartsWith(Folder, StringComparison.Ordinal)).OrderBy(key => key, StringComparer.Ordinal).ToArray();
}
