namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;
using BaseStorage.Core.Tests.Ui;

/// <summary>
/// GW-U-1380..1384 (contract organize-runner-contracts.md section 5 source-scan rules; ADR-GWIN-108..113): the Plugin Organize folder
/// holds the six game-bound files, the runner is wired into Boot and Update behind the fail-closed wrapper, the lock store is the one
/// writer of the lock key and the one claimer of station ownership, the layer never takes a chest from a player, the hotkey and the
/// popup are confined to their files, and no file patches, waits, resolves a base or carries user-facing text.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginOrganizeSourceScanTests
{
    private const string Folder = "src/BaseStorage.Plugin/Organize/";
    private const string Runner = Folder + "OrganizeRunner.cs";
    private const string Port = Folder + "GameOrganizeRunPort.cs";
    private const string Layer = Folder + "GameOrganizeContainerLayer.cs";
    private const string LockStore = Folder + "GameOrganizeLockStore.cs";
    private const string Hotkey = Folder + "OrganizeHotkey.cs";
    private const string Popup = Folder + "OrganizePreviewPopup.cs";

    private static readonly string[] Files = { Layer, LockStore, Port, Hotkey, Popup, Runner };

    // GW-U-1380: the folder holds exactly the six files, each in the contracted namespace, each internal.
    [Fact]
    public void TheFolderHoldsExactlyTheSixFiles()
    {
        Dictionary<string, string> sources = Sources();

        Assert.Equal(Files.OrderBy(f => f, StringComparer.Ordinal).ToArray(), FolderFiles(sources));
        foreach (string file in Files)
        {
            Assert.Matches(@"(?m)^namespace BaseStorage\.Plugin\.Organize;", sources[file]);
            Assert.Matches(@"\binternal\s+(static\s+|sealed\s+)class\b", sources[file]);
            Assert.DoesNotMatch(@"\bpublic\s+(static\s+|sealed\s+)?class\b", sources[file]);
        }
    }

    // GW-U-1381 (contract 5 wiring): Boot creates the runner after the writer and before the patches; Update ticks it after the writer.
    [Fact]
    public void BootAndUpdateWireTheRunner()
    {
        string entry = Sources()[PluginSourceScan.EntryPoint];
        string boot = PluginSourceScan.MethodBody(entry, @"void\s+Boot\s*\(\s*\)");
        string update = Regex.Replace(PluginSourceScan.MethodBody(entry, @"void\s+Update\s*\(\s*\)"), @"\s+", string.Empty);

        Assert.Single(Regex.Matches(boot, Regex.Escape("OrganizeRunner.Initialize(world, reservations, new GameClock(), Settings, sink);")));
        int writer = boot.IndexOf("ChestCategoryWriter.Initialize(", StringComparison.Ordinal);
        int runner = boot.IndexOf("OrganizeRunner.Initialize(", StringComparison.Ordinal);
        int apply = boot.IndexOf("PatchApplier.ApplyAll(", StringComparison.Ordinal);
        Assert.True(writer >= 0 && writer < runner && runner < apply, "Boot order must be ChestCategoryWriter.Initialize, OrganizeRunner.Initialize, PatchApplier.ApplyAll.");
        // Edited by S-E004-012 (ADR-GWIN-136): StationActionsPanel.Initialize follows OrganizeRunner.Initialize and precedes new Harmony(.
        int station = boot.IndexOf("StationActionsPanel.Initialize(", StringComparison.Ordinal);
        Assert.True(runner < station && station < boot.IndexOf("new Harmony(", StringComparison.Ordinal), "Boot must call StationActionsPanel.Initialize after OrganizeRunner.Initialize and before new Harmony(.");
        // Edited by integration (S-E004-004 + S-E004-011): the S-E004-004 CategoryHotkey.Tick() also follows ChestCategoryWriter.Tick(), so the runner tick comes right after it.
        Assert.Contains("ChestCategoryWriter.Tick();CategoryHotkey.Tick();OrganizeRunner.Tick();", update, StringComparison.Ordinal);
    }

    // GW-U-1381: the runner's shape: the wrapped static tick, the coordinator holder, the entry points.
    [Fact]
    public void TheRunnerHasTheContractedShape()
    {
        string runner = Sources()[Runner];

        Assert.Matches(@"internal\s+static\s+class\s+OrganizeRunner\b", runner);
        Assert.Contains("internal const string TickKey = \"BaseStorage.Plugin.Organize.OrganizeRunner.Tick\";", runner, StringComparison.Ordinal);
        Assert.Matches(@"internal\s+static\s+OrganizeRunCoordinator\?\s+Current\s*\{\s*get;\s*private\s+set;\s*\}", runner);
        Assert.Matches(@"PatchSafety\.Run\(TickKey,\s*static\s*\(\)\s*=>\s*Step\(\)\);", runner);
        Assert.Matches(@"internal\s+static\s+OrganizeStartOutcome\s+StartAt\(BaseStart start\)", runner);
        Assert.Matches(@"internal\s+static\s+void\s+Confirm\(\)", runner);
        Assert.Matches(@"internal\s+static\s+void\s+Cancel\(\)", runner);
        Assert.Contains("new OrganizeRunCoordinator(port, world.Cache, new GameOrganizeLockStore(world), clock, settings, sink)", runner, StringComparison.Ordinal);
        Assert.Contains("BaseStart.FromPosition(", runner, StringComparison.Ordinal);
        Assert.Contains("Player.m_localPlayer", runner, StringComparison.Ordinal);
        Assert.Contains("coordinator.State == OrganizeRunState.Idle && OrganizeHotkey.IsPressed(", runner, StringComparison.Ordinal);
    }

    // GW-U-1382 (RN11): the lock key has one reader and one writer, both in the lock store, which is the only claimer of ownership.
    [Fact]
    public void TheLockStoreIsTheOnlyLockKeyWriterAndTheOnlyOwnershipClaimer()
    {
        Dictionary<string, string> sources = Sources();

        Assert.Equal(new[] { LockStore }, PluginSourceScan.FilesMatching(sources, @"PersistenceKeys\.OrganizeLock\b"));
        Assert.Single(Regex.Matches(sources[LockStore], @"\.Set\(PersistenceKeys\.OrganizeLock, value\);"));
        Assert.Single(Regex.Matches(sources[LockStore], @"GetString\(PersistenceKeys\.OrganizeLock, string\.Empty\)"));
        Assert.Contains("Put(station, string.Empty);", sources[LockStore], StringComparison.Ordinal);
        Assert.Single(Regex.Matches(sources[LockStore], @"view\.ClaimOwnership\(\);"));
        Assert.Matches(@"if \(!view\.IsOwner\(\)\)\s*\{\s*view\.ClaimOwnership\(\);\s*\}", sources[LockStore]);
        foreach (string file in FolderFiles(sources).Where(f => f != LockStore))
        {
            Assert.DoesNotContain("ClaimOwnership", sources[file], StringComparison.Ordinal);
        }

        Assert.Empty(PluginSourceScan.FilesMatching(sources, @"RemoveString|GetStableHashCode|ZDOExtraData"));
    }

    // GW-U-1383 (RN9, RN10): the layer reserves only through the driver, writes only behind CanWrite, and never takes a chest from a player.
    [Fact]
    public void TheLayerGoesThroughTheDriverAndWritesOnlyBehindCanWrite()
    {
        Dictionary<string, string> sources = Sources();
        string layer = sources[Layer];

        Assert.Contains("reservations.Begin(container, OrganizeLayerRules.Operation)", layer, StringComparison.Ordinal);
        Assert.Contains("OrganizeLayerRules.StatusOf(", layer, StringComparison.Ordinal);
        Assert.Contains("reservations.BeginWrite(id);", layer, StringComparison.Ordinal);
        Assert.Contains("reservations.Release(id);", layer, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(layer, @"if \(!reservations\.CanWrite\(id\)\)").Count);
        Assert.Contains("OrganizeLayerRules.CanFit(live.Contents, live.SlotCount, items)", layer, StringComparison.Ordinal);
        Assert.Contains("live.Contents[stackIndex] != expected", layer, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(layer, @"\.AddItem\(item\)"));
        Assert.Single(Regex.Matches(layer, @"\.RemoveItem\(item, count\)"));
        Assert.Contains("snapshots.Snapshot(", layer, StringComparison.Ordinal);
        Assert.Contains("\"organize\"", Strip(RepoRoot.ReadText("src/BaseStorage.Core/Organize/Run/OrganizeLayerRules.cs")), StringComparison.Ordinal);
        Assert.Empty(PluginSourceScan.FilesMatching(sources, "\"organize\""));

        foreach (string file in FolderFiles(sources))
        {
            foreach (string token in new[] { "SetOwner", "ForceSendZDO", "InvokeRPC", "SetInUse", ".Hide(", "CloseContainer", "TakeAll", "RequestStack", ".Resolve(", ".GetStations(", ".GetContainers(", "[HarmonyPatch", "Thread", "Task.", "Sleep(", "DateTime", "Stopwatch", "MonoBehaviour", "KeyboardShortcut", "ConfigEntry" })
            {
                Assert.False(sources[file].Contains(token, StringComparison.Ordinal), file + " contains " + token);
            }

            Assert.DoesNotMatch(@"\b(void|bool)\s+(Update|LateUpdate|FixedUpdate|OnGUI)\s*\(", sources[file]);
        }
    }

    // GW-U-1384 (contract 5): the popup and the hotkey are confined to their files, the port is the only notice caller, and no literal is English text.
    [Fact]
    public void PopupHotkeyAndNoticesAreConfinedAndNoLiteralIsUserFacing()
    {
        Dictionary<string, string> sources = Sources();

        Assert.Equal(new[] { Popup }, FolderFiles(sources).Where(f => Regex.IsMatch(sources[f], @"\b(YesNoPopup|UnifiedPopup\.Push)\b")).ToArray());
        Assert.Contains("UnifiedPopup.IsAvailable()", sources[Popup], StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(sources[Popup], @"UnifiedPopup\.Pop\(\);").Count);
        Assert.Contains("OrganizeRunner.Confirm();", sources[Popup], StringComparison.Ordinal);
        Assert.Contains("OrganizeRunner.Cancel();", sources[Popup], StringComparison.Ordinal);
        Assert.Contains("OrganizeRunner.Cancel();", sources[Port], StringComparison.Ordinal);

        Assert.Equal(new[] { Hotkey }, FolderFiles(sources).Where(f => Regex.IsMatch(sources[f], @"\b(ZInput|KeyCode)\b")).ToArray());
        Assert.Contains("HotkeyChord.Parse(", sources[Hotkey], StringComparison.Ordinal);
        Assert.Contains("ZInput.GetKeyDown(main, false)", sources[Hotkey], StringComparison.Ordinal);
        Assert.Contains("settings.OrganizeKey", sources[Hotkey], StringComparison.Ordinal);
        Assert.DoesNotMatch(@"\bKeyCode\.(?!None\b)\w+", sources[Hotkey]);
        foreach (string gate in new[] { "HasFocus()", "Console.IsVisible()", "TextInput.IsVisible()", "Menu.IsVisible()", "UnifiedPopup.IsVisible()", "Player.m_localPlayer == null" })
        {
            Assert.Contains(gate, sources[Hotkey], StringComparison.Ordinal);
        }

        Assert.Equal(new[] { Port }, FolderFiles(sources).Where(f => sources[f].Contains("Notices.Show(", StringComparison.Ordinal)).ToArray());
        Assert.Contains("ChestCategoryWriter.Read(container)", sources[Port], StringComparison.Ordinal);
        Assert.Contains("reservations.QueryInUse(container)", sources[Port], StringComparison.Ordinal);
        Assert.Contains("PluginCategoryCatalog.Current", sources[Port], StringComparison.Ordinal);
        Assert.Contains("ChestAccessRules.HolderLabel(", sources[Port], StringComparison.Ordinal);

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
            }
        }

        Assert.Empty(offenders);
    }

    private static Dictionary<string, string> Sources()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        foreach (string file in Files)
        {
            Assert.True(sources.ContainsKey(file), "Scan did not read " + file);
        }

        return sources;
    }

    private static string[] FolderFiles(Dictionary<string, string> sources) =>
        sources.Keys.Where(key => key.StartsWith(Folder, StringComparison.Ordinal)).OrderBy(key => key, StringComparer.Ordinal).ToArray();

    private static string Strip(string text) => PluginSourceScan.StripComments(text.Replace("\r\n", "\n", StringComparison.Ordinal));
}
