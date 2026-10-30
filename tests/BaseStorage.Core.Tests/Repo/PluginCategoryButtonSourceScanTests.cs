namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;
using BaseStorage.Core.Tests.Ui;

/// <summary>
/// GW-U-1310..1317 (contract S-E004-004 section 6.2, ADR-GWIN-104..107): the Category button folder holds exactly its six files, the one
/// patch has the contracted shape, the button is one factory clone placed by the Core layout and refreshed without allocation, the hotkey
/// reads the BepInEx shortcut only through the settings binding behind vanilla's input gate, the catalog is loaded in one place, Boot and
/// Update are wired, and nothing in the folder shows notices, writes the key or carries user-facing text. Sources are comment-stripped.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginCategoryButtonSourceScanTests
{
    private const string Folder = "src/BaseStorage.Plugin/CategoryButton/";
    private const string ButtonFile = Folder + "ChestCategoryButton.cs";
    private const string RelayFile = Folder + "CategoryRightClickRelay.cs";
    private const string HotkeyFile = Folder + "CategoryHotkey.cs";
    private const string CatalogFile = Folder + "PluginCategoryCatalog.cs";
    private const string MessagesFile = Folder + "CategoryCatalogMessages.cs";
    private const string PatchFile = Folder + "InventoryGuiUpdateContainerPatch.cs";
    private const string HotkeySourceFile = "src/BaseStorage.Plugin/Settings/IHotkeySource.cs";

    private static readonly string[] FolderFiles = { MessagesFile, HotkeyFile, RelayFile, ButtonFile, PatchFile, CatalogFile };

    // GW-U-1310 (CS1): the folder holds exactly the six files and the game-free interface sits in Settings/.
    [Fact]
    public void TheFolderHoldsExactlyTheSixFilesAndTheHotkeyInterfaceIsGameFree()
    {
        Dictionary<string, string> sources = Sources();

        Assert.Equal(FolderFiles.OrderBy(f => f, StringComparer.Ordinal).ToArray(), FilesIn(sources));
        string hotkeySource = sources[HotkeySourceFile];
        Assert.Contains("internal interface IHotkeySource", hotkeySource, StringComparison.Ordinal);
        Assert.Matches(@"bool\s+IsHotkeyDown\(string key\);", hotkeySource);
        Assert.DoesNotMatch(@"\b(UnityEngine|BepInEx|HarmonyLib|KeyboardShortcut|KeyCode)\b", hotkeySource);
    }

    // GW-U-1311 (CS1): the one patch class has the contracted shape and is the only [HarmonyPatch in the folder.
    [Fact]
    public void ThePatchClassHasTheContractedShape()
    {
        Dictionary<string, string> sources = Sources();
        string text = Flat(sources[PatchFile]);

        Assert.Equal(new[] { PatchFile }, FilesIn(sources).Where(file => sources[file].Contains("[HarmonyPatch", StringComparison.Ordinal)).ToArray());
        Assert.Contains("internal static class InventoryGuiUpdateContainerPatch", text, StringComparison.Ordinal);
        Assert.Contains("[HarmonyPatch(typeof(InventoryGui), PatchTargetNames.InventoryGui.UpdateContainer)]", text, StringComparison.Ordinal);
        Assert.Contains(
            "internal const string PatchKey = \"BaseStorage.Plugin.CategoryButton.InventoryGuiUpdateContainerPatch.Postfix\";",
            text,
            StringComparison.Ordinal);
        Assert.Contains(
            "private static void Postfix(InventoryGui __instance) => PatchSafety.Run(PatchKey, __instance, static gui => ChestCategoryButton.Refresh(gui));",
            text,
            StringComparison.Ordinal);
        Assert.DoesNotMatch("\\[HarmonyPatch\\([^\\n]*,\\s*\"", sources[PatchFile]);
    }

    // GW-U-1312 (CS2, CB1-CB3): one factory clone of the Take All button, labelled through the table, placed by the Core layout.
    [Fact]
    public void TheButtonIsOneFactoryCloneLabelledFromTheTableAndPlacedByCore()
    {
        string text = Sources()[ButtonFile];

        Assert.Contains("internal const string ControlId = \"chest.category.cycle-button\";", text, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(text, @"VanillaButtonFactory\.Create\("));
        Assert.Contains("VanillaButtonFactory.Create(template, template.transform.parent, ControlId, StringKeys.ChestCategoryAuto, OnLeftClick)", text, StringComparison.Ordinal);
        Assert.Contains("gui.m_takeAllButton", text, StringComparison.Ordinal);
        Assert.Contains("gui.m_stackAllButton", text, StringComparison.Ordinal);
        Assert.Contains("VanillaButtonFactory.SetLabel(control, StringKeys.ChestCategoryButton, ChestCategoryReader.Label(ChestCategoryReader.Read(raw), StringTable.English));", text, StringComparison.Ordinal);
        Assert.Contains("CategoryButtonLayout.GrowsLeft(", text, StringComparison.Ordinal);
        Assert.Contains("CategoryButtonLayout.OuterX(", text, StringComparison.Ordinal);
        Assert.Contains("CategoryButtonLayout.AnchoredX(", text, StringComparison.Ordinal);
        Assert.Contains("CategoryButtonLayout.Gap", text, StringComparison.Ordinal);
        Assert.Contains("AddComponent<CategoryRightClickRelay>()", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Instantiate(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("AddComponent<Button>", text, StringComparison.Ordinal);
        Assert.DoesNotContain("new GameObject(", text, StringComparison.Ordinal);
    }

    // GW-U-1313 (CS2, CB3, CB5, CB6, spec AC-9.2): the refresh compares before it writes and allocates nothing per frame.
    [Fact]
    public void TheRefreshComparesBeforeWritingAndAllocatesNothingPerFrame()
    {
        string text = Sources()[ButtonFile];
        string refresh = PluginSourceScan.MethodBody(text, @"internal static void Refresh\(InventoryGui gui\)");

        Assert.Contains("ReferenceEquals(chest, cachedChest)", refresh, StringComparison.Ordinal);
        Assert.Contains("string.Equals(raw, cachedRaw, StringComparison.Ordinal)", refresh, StringComparison.Ordinal);
        Assert.Matches(@"if \(control\.interactable != interactable\)\s*\{\s*control\.interactable = interactable;", refresh);
        Assert.True(SetActiveIsGuardedByActiveSelf(refresh), "SetActive( must sit inside a branch guarded by activeSelf.");
        Assert.DoesNotContain("=>", refresh, StringComparison.Ordinal);

        string[] forbidden = { "using System.Linq", "$\"", "string.Format", "string.Concat", "+ \"", "new List<", "new Dictionary<", "new HashSet<", ".ToArray(", ".ToList(", "foreach" };
        string[] hits = forbidden.Where(token => text.Contains(token, StringComparison.Ordinal)).ToArray();
        Assert.Empty(hits);
    }

    // GW-U-1314 (CB4, CB5): left click and right click go through the writer behind the fail-closed wrapper; the relay honours interactable.
    [Fact]
    public void ClicksCycleThroughTheWriterAndTheRelayHonoursInteractable()
    {
        Dictionary<string, string> sources = Sources();
        string button = sources[ButtonFile];
        string relay = sources[RelayFile];

        Assert.Contains("PatchSafety.Run(LeftClickKey, static () => OpenRules());", button, StringComparison.Ordinal);
        Assert.Contains("PatchSafety.Run(RightClickKey, static () => CycleOpenChest(forward: false));", button, StringComparison.Ordinal);
        string cycle = PluginSourceScan.MethodBody(button, @"internal static void Cycle\(Container chest, bool forward\)");
        Assert.Contains("PluginCategoryCatalog.Current", cycle, StringComparison.Ordinal);
        Assert.Contains("ChestCategoryWriter.Read(chest)", cycle, StringComparison.Ordinal);
        Assert.Contains("forward ? CategoryCycle.Next(current, catalog) : CategoryCycle.Previous(current, catalog)", cycle, StringComparison.Ordinal);
        Assert.Contains("ChestCategoryWriter.TrySet(chest, desired);", cycle, StringComparison.Ordinal);
        Assert.Contains("driver.IsPending(id)", button, StringComparison.Ordinal);

        Assert.Contains("internal sealed class CategoryRightClickRelay : MonoBehaviour, IPointerClickHandler", relay, StringComparison.Ordinal);
        Assert.Contains("PointerEventData.InputButton.Right", relay, StringComparison.Ordinal);
        Assert.Contains("!button.interactable", relay, StringComparison.Ordinal);
        Assert.Contains("ChestCategoryButton.OnRightClick();", relay, StringComparison.Ordinal);
        Assert.Equal(new[] { RelayFile }, FilesIn(sources).Where(file => sources[file].Contains("MonoBehaviour", StringComparison.Ordinal)).ToArray());
    }

    // GW-U-1315 (CS3, CS6, CB7, CB8): the hotkey gate order, no key code outside the binding, and the binding's IsDown query.
    [Fact]
    public void TheHotkeyReadsTheBindingBehindVanillasInputGateInContractOrder()
    {
        Dictionary<string, string> sources = Sources();
        string hotkey = sources[HotkeyFile];
        string binding = sources[PluginSourceScan.SettingsFile];

        Assert.Contains("internal const string TickKey = \"BaseStorage.Plugin.CategoryButton.CategoryHotkey.Tick\";", hotkey, StringComparison.Ordinal);
        Assert.Matches(@"PatchSafety\.Run\(TickKey,\s*static\s*\(\)\s*=>\s*Poll\(\)\);", hotkey);
        string poll = PluginSourceScan.MethodBody(hotkey, @"private static void Poll\(\)");
        string[] anchors =
        {
            "IsHotkeyDown(SettingKeys.CycleCategoryKey)",
            "Player.m_localPlayer",
            "TakeInput()",
            "GetHoverObject()",
            "GetComponentInParent<Container>()",
            "ChestCategoryButton.Cycle(chest, forward: true)",
        };
        int previous = -1;
        foreach (string anchor in anchors)
        {
            int at = poll.IndexOf(anchor, StringComparison.Ordinal);
            Assert.True(at > previous, $"'{anchor}' is missing from Poll or out of order.");
            previous = at;
        }

        Assert.DoesNotMatch(@"\b(KeyCode|KeyboardShortcut|ZInput|Input)\b", hotkey);
        Assert.Matches(@"internal\s+sealed\s+class\s+BepInExSettingsSource\s*:\s*ISettingsSource,\s*IHotkeySource\b", binding);
        string isDown = PluginSourceScan.MethodBody(binding, @"public\s+bool\s+IsHotkeyDown\(string key\)");
        Assert.Contains(".IsDown()", isDown, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"\bthrow\b", isDown);
        Assert.Equal(new[] { PluginSourceScan.SettingsFile }, PluginSourceScan.FilesMatching(sources, @"\.IsDown\(\)"));
    }

    // GW-U-1316 (CS4, CB9): the catalog is loaded in one place, from the config file, with the default as the fallback.
    [Fact]
    public void TheCatalogIsLoadedOnceFromTheConfigFileWithTheDefaultFallback()
    {
        Dictionary<string, string> sources = Sources();
        string catalog = sources[CatalogFile];

        Assert.Equal(new[] { CatalogFile }, FilesIn(sources).Where(file => Regex.IsMatch(sources[file], @"\bFile\.|CategoryFile\.Parse\(|DefaultCategories\.Catalog")).ToArray());
        Assert.Single(Regex.Matches(catalog, @"CategoryFile\.Parse\("));
        Assert.Contains("DefaultCategoryFileWriter.PathFor(folder)", catalog, StringComparison.Ordinal);
        Assert.Contains("parsed.Catalog.CategoryNames.Count > 0", catalog, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(catalog, @"return DefaultCategories\.Catalog;").Count);
        Assert.Equal(2, Regex.Matches(catalog, @"LogPolicy\.Write\(sink, LogCategory\.Detail,").Count);
        Assert.Matches(@"internal\s+static\s+CategoryCatalog\s+Current", catalog);
        Assert.Matches(@"internal\s+static\s+void\s+Initialize\(string configFolder, ILogSink sink\)", catalog);
    }

    // Integration of CB9 with config-sync N11 (S-E004-004 + S-E006-004): while a host payload is applied, Current returns the host's
    // catalog read through PluginConfigSync.Categories (when it defines a category), before the cached local catalog.
    [Fact]
    public void TheCatalogPrefersTheHostSyncedCatalogWhileAHostIsApplied()
    {
        string catalog = Sources()[CatalogFile];
        string current = catalog.Substring(catalog.IndexOf("internal static CategoryCatalog Current", StringComparison.Ordinal));

        Assert.Contains("PluginConfigSync.Categories", catalog, StringComparison.Ordinal);
        Assert.Contains("sync.Settings.HasHost", catalog, StringComparison.Ordinal);
        Assert.Contains("catalog.CategoryNames.Count > 0", catalog, StringComparison.Ordinal);
        int host = current.IndexOf("HostCatalog()", StringComparison.Ordinal);
        int cached = current.IndexOf("CategoryCatalog? loaded = current;", StringComparison.Ordinal);
        Assert.True(host >= 0 && cached > host, "Current must check the host catalog before the cached local catalog.");
    }

    // GW-U-1317 (CS5, CS7): Boot and Update wiring; nothing in the folder shows notices, writes the key, waits or carries table text.
    [Fact]
    public void BootAndUpdateAreWiredAndTheFolderShowsNoNoticeWritesNoKeyAndCarriesNoText()
    {
        Dictionary<string, string> sources = Sources();
        string entry = sources[PluginSourceScan.EntryPoint];
        string boot = PluginSourceScan.MethodBody(entry, @"void\s+Boot\s*\(\s*\)");
        string update = Regex.Replace(PluginSourceScan.MethodBody(entry, @"void\s+Update\s*\(\s*\)"), @"\s+", string.Empty);

        int writer = boot.IndexOf("ChestCategoryWriter.Initialize(", StringComparison.Ordinal);
        int hotkey = boot.IndexOf("CategoryHotkey.Initialize(settingsSource);", StringComparison.Ordinal);
        int catalog = boot.IndexOf("PluginCategoryCatalog.Initialize(Paths.ConfigPath, sink);", StringComparison.Ordinal);
        int harmony = boot.IndexOf("new Harmony(PluginGuid)", StringComparison.Ordinal);
        Assert.True(writer >= 0 && writer < hotkey && hotkey < catalog && catalog < harmony, "Boot order must be ChestCategoryWriter.Initialize, CategoryHotkey.Initialize, PluginCategoryCatalog.Initialize, new Harmony.");
        Assert.Contains("ChestCategoryWriter.Tick();CategoryHotkey.Tick();", update, StringComparison.Ordinal);

        var offenders = new List<string>();
        foreach (string file in FilesIn(sources))
        {
            string code = sources[file];
            foreach (string token in new[] { "PersistenceKeys", "Notices", ".ShowMessage(", "Instantiate(", "AddComponent<Button>", "new GameObject(", "Thread", "Task.", "Sleep(", "DateTime", "Stopwatch", "SetOwner", "ClaimOwnership", "InvokeRPC", "SetInUse", "ZDO", "KeyCode", "ConfigEntry" })
            {
                if (code.Contains(token, StringComparison.Ordinal))
                {
                    offenders.Add(file + " contains " + token);
                }
            }

            if (Regex.IsMatch(code, @"\b(void|bool)\s+(Update|LateUpdate|FixedUpdate|OnGUI)\s*\("))
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
        foreach (string file in FolderFiles.Append(HotkeySourceFile).Append(PluginSourceScan.SettingsFile))
        {
            Assert.True(sources.ContainsKey(file), "Scan did not read " + file);
        }

        return sources;
    }

    private static string[] FilesIn(Dictionary<string, string> sources) =>
        sources.Keys.Where(key => key.StartsWith(Folder, StringComparison.Ordinal)).OrderBy(key => key, StringComparer.Ordinal).ToArray();

    private static string Flat(string text) => Regex.Replace(text, @"\s+", " ");

    /// <summary>True when every SetActive( call has an activeSelf guard within the three lines above it.</summary>
    private static bool SetActiveIsGuardedByActiveSelf(string text)
    {
        string[] lines = text.Split('\n');
        bool found = false;
        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains("SetActive(", StringComparison.Ordinal))
            {
                continue;
            }

            found = true;
            bool guarded = false;
            for (int j = Math.Max(0, i - 3); j <= i; j++)
            {
                guarded |= lines[j].Contains("activeSelf", StringComparison.Ordinal);
            }

            if (!guarded)
            {
                return false;
            }
        }

        return found;
    }
}
