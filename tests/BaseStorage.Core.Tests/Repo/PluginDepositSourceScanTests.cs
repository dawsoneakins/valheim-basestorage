namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BaseStorage.Core.Tests.Ui;

/// <summary>
/// GW-U-1941..1949 (AC-1..AC-4; deposit-button contract 9): source scans of the Plugin Deposit folder and of the three Deposit call sites
/// in the Organize runner, the port and the Core coordinator. The texts are comment-stripped and CRLF-normalised (PluginSourceScan), so a
/// commented-out line cannot satisfy a rule. Red until the implement phase adds the folder and the call sites.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginDepositSourceScanTests
{
    private const string Folder = "src/BaseStorage.Plugin/Deposit/";
    private const string TraitRules = Folder + "DepositTraitRules.cs";
    private const string Messages = Folder + "DepositMessages.cs";
    private const string Capture = Folder + "PlayerInventoryCapture.cs";
    private const string Layer = Folder + "GameDepositContainerLayer.cs";
    private const string Hotkey = Folder + "DepositHotkey.cs";
    private const string Runner = "src/BaseStorage.Plugin/Organize/OrganizeRunner.cs";
    private const string Port = "src/BaseStorage.Plugin/Organize/GameOrganizeRunPort.cs";
    private const string Coordinator = "src/BaseStorage.Core/Organize/Run/OrganizeRunCoordinator.cs";

    private static readonly string[] FolderNames =
    {
        "DepositHotkey.cs",
        "DepositMessages.cs",
        "DepositTraitRules.cs",
        "GameDepositContainerLayer.cs",
        "PlayerInventoryCapture.cs",
    };

    private static readonly string[] GameFreeFiles = { TraitRules, Messages };

    // GW-U-1941 (9): the folder holds exactly the five internal files of the contracted namespace, and no patch.
    [Fact]
    public void TheDepositFolderHoldsExactlyTheFiveInternalFilesWithNoPatch()
    {
        Dictionary<string, string> files = FolderSources();

        string[] expected = FolderNames.Select(name => Folder + name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        Assert.Equal(expected, files.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray());
        foreach (KeyValuePair<string, string> pair in files)
        {
            Assert.Contains("namespace BaseStorage.Plugin.Deposit;", pair.Value, StringComparison.Ordinal);
            Assert.Matches(@"internal\s+(static\s+|sealed\s+|readonly\s+)*(class|struct|enum|interface|record)\b", pair.Value);
            Assert.DoesNotMatch(@"\bpublic\b", pair.Value);
        }

        Assert.Empty(PluginSourceScan.FilesMatching(files, @"\[HarmonyPatch"));
        Assert.Empty(PluginSourceScan.FilesMatching(files, @"\b(Prefix|Postfix|Transpiler)\b"));
    }

    // GW-U-1942 (4, NFR game-free): the two linked files use no game namespace and no whole-word game type.
    [Fact]
    public void TheGameFreeFilesUseNoGameNamespaceOrGameType()
    {
        Dictionary<string, string> files = FolderSources();

        foreach (string file in GameFreeFiles)
        {
            string text = Text(files, file);
            Assert.DoesNotMatch(@"using\s+(UnityEngine|HarmonyLib|BepInEx|TMPro)\b", text);
            Assert.DoesNotMatch(@"\b(Player|Inventory|InventoryGui|Container|ItemDrop|ZDO|ZNetView|ZInput|KeyCode)\b", text);
        }
    }

    // GW-U-1943 (2, DB11): ZInput and KeyCode live only in the hotkey file, which reads the settings key and holds all seven gates.
    [Fact]
    public void TheHotkeyScanIsConfinedToTheHotkeyFileWithItsGates()
    {
        Dictionary<string, string> files = FolderSources();
        Assert.Equal(new[] { Hotkey }, PluginSourceScan.FilesMatching(files, @"\b(ZInput|KeyCode)\b"));

        string hotkey = Text(files, Hotkey);
        foreach (string literal in new[]
                 {
                     "settings.DepositKey",
                     "HotkeyChord.Parse(",
                     "ZInput.GetKeyDown(main, false)",
                     "HasFocus()",
                     "Console.IsVisible()",
                     "TextInput.IsVisible()",
                     "Menu.IsVisible()",
                     "InventoryGui.IsVisible()",
                     "UnifiedPopup.IsVisible()",
                     "Player.m_localPlayer == null",
                 })
        {
            Assert.Contains(literal, hotkey, StringComparison.Ordinal);
        }

        Assert.DoesNotMatch(@"KeyCode\.(?!None\b)\w+", hotkey);
        Assert.DoesNotContain("settings.OrganizeKey", hotkey, StringComparison.Ordinal);
    }

    // GW-U-1944 (4, DB13, NFR add-then-remove): the layer guards the write, adds the clone and removes the stack once, and never checks a chest against the game.
    [Fact]
    public void TheDepositLayerAddsThenRemovesOnceThroughTheOrganizeLayerRules()
    {
        string layer = Text(FolderSources(), Layer);

        Assert.Single(Regex.Matches(layer, @"if \(!reservations\.CanWrite\(id\)\)"));
        Assert.Single(Regex.Matches(layer, @"\.AddItem\(item\)"));
        Assert.Single(Regex.Matches(layer, @"\.RemoveItem\(item, count\)"));
        Assert.Single(Regex.Matches(layer, @"new GameOrganizeContainerLayer\("));
        Assert.Contains("OrganizeLayerRules.CanFit(", layer, StringComparison.Ordinal);
        Assert.Contains("PlayerInventoryCapture.Capture(", layer, StringComparison.Ordinal);
    }

    // GW-U-1945 (5, DB12, NFR exclusions): the capture reads the list order, the grid, the favourite slot and the equipped flag, and maps the three item types.
    [Fact]
    public void TheInventoryCaptureReadsTheGameFactsOfEveryStack()
    {
        string capture = Text(FolderSources(), Capture);

        foreach (string literal in new[]
                 {
                     "GetAllItems()",
                     "m_gridPos",
                     "ProfileFavourites.TryGetSlot(",
                     "m_equipped",
                     "IsItemEquiped(",
                     "ItemKindAdapter.From(",
                     "DepositTraitRules.Of(",
                     "ItemType.Consumable",
                     "ItemType.Ammo",
                     "ItemType.AmmoNonEquipable",
                     "ZDOMan.GetSessionID()",
                 })
        {
            Assert.Contains(literal, capture, StringComparison.Ordinal);
        }
    }

    // GW-U-1946 (NFR, DB8, DB10, DB13): no notice, popup, ownership, RPC, persistence, blocking, time, frame method or table text anywhere in the folder.
    [Fact]
    public void TheDepositFolderHasNoForbiddenTokenOrEnglishTableText()
    {
        Dictionary<string, string> files = FolderSources();
        // Guard: an absent folder must not let the scan pass vacuously.
        Assert.Equal(FolderNames.Length, files.Count);
        string[] forbidden =
        {
            "Notices.Show(",
            ".ShowMessage(",
            "UnifiedPopup.Push",
            "YesNoPopup",
            "ClaimOwnership",
            "SetOwner",
            "InvokeRPC",
            "SetInUse",
            "ForceSendZDO",
            "m_customData",
            "PersistenceKeys.",
            ".Resolve(",
            "Thread",
            "Task.",
            "Sleep(",
            "DateTime",
            "Stopwatch",
            "MonoBehaviour",
            "StringTable.",
        };
        string[] englishTexts = UiFixtures.EnglishTable.Select(entry => entry.Text).Where(text => text.Length >= 8).ToArray();

        foreach (KeyValuePair<string, string> pair in files)
        {
            foreach (string token in forbidden)
            {
                Assert.DoesNotContain(token, pair.Value, StringComparison.Ordinal);
            }

            Assert.DoesNotMatch(@"\b(Update|LateUpdate|FixedUpdate|OnGUI)\s*\(", pair.Value);

            foreach (Match literal in Regex.Matches(pair.Value, "\"([^\"\\n]*)\""))
            {
                foreach (string english in englishTexts)
                {
                    Assert.DoesNotContain(english, literal.Groups[1].Value, StringComparison.Ordinal);
                }
            }
        }
    }

    // GW-U-1947 (2, DB11, 3.4): the runner declares StartDepositAt, polls the Organize key before the Deposit key while idle, and starts the Deposit from the position.
    [Fact]
    public void TheRunnerPollsTheDepositKeyAfterTheOrganizeKey()
    {
        string runner = Text(PluginSourceScan.Sources(), Runner);

        Assert.Matches(@"internal\s+static\s+OrganizeStartOutcome\s+StartDepositAt\(BaseStart\s+start\)", runner);
        int organize = runner.IndexOf("coordinator.State == OrganizeRunState.Idle && OrganizeHotkey.IsPressed(", StringComparison.Ordinal);
        int deposit = runner.IndexOf("coordinator.State == OrganizeRunState.Idle && DepositHotkey.IsPressed(", StringComparison.Ordinal);
        Assert.True(organize >= 0, "The Organize poll is missing.");
        Assert.True(deposit > organize, "The Deposit poll must come after the Organize poll.");
        Assert.Contains("coordinator.StartDeposit(BaseStart.FromPosition(", runner, StringComparison.Ordinal);
    }

    // GW-U-1948 (4, NFR): the port reads the favourites through the session, captures the inventory, and builds the Deposit layer without touching the custom data.
    [Fact]
    public void ThePortReadsTheFavouritesAndBuildsTheDepositLayer()
    {
        string port = Text(PluginSourceScan.Sources(), Port);

        Assert.Contains("FavouriteSession.OccupiedSlots()", port, StringComparison.Ordinal);
        Assert.Contains("PlayerInventoryCapture.Capture(", port, StringComparison.Ordinal);
        Assert.Contains("new GameDepositContainerLayer(", port, StringComparison.Ordinal);
        Assert.DoesNotContain("m_customData", port, StringComparison.Ordinal);
    }

    // GW-U-1949 (4, DB8): the coordinator has exactly one planner call, one capture and one layer call, and its strings are each used once.
    [Fact]
    public void TheCoordinatorCallsEachDepositSeamOnceAndUsesEachDepositKeyOnce()
    {
        string coordinator = PluginSourceScan.StripComments(RepoRoot.ReadText(Coordinator));

        Assert.Single(Regex.Matches(coordinator, @"DepositPlanner\.Plan\("));
        Assert.Single(Regex.Matches(coordinator, @"port\.CaptureInventory\("));
        Assert.Single(Regex.Matches(coordinator, @"port\.CreateDepositLayer\("));
        Assert.Contains("settings.DepositPreview", coordinator, StringComparison.Ordinal);
        foreach (string key in new[] { "MessageDepositSummary", "MessageDepositSummaryStopped", "MessageDepositCancelled", "DepositPreviewHeader" })
        {
            Assert.Single(Regex.Matches(coordinator, @"StringKeys\." + key + @"\b"));
        }
    }

    // The Deposit folder only. The runner and the port are read from the whole Plugin scan.
    private static Dictionary<string, string> FolderSources() =>
        PluginSourceScan.Sources()
            .Where(pair => pair.Key.StartsWith(Folder, StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    private static string Text(Dictionary<string, string> sources, string relative)
    {
        Assert.True(sources.ContainsKey(relative), relative + " does not exist yet (S-E004-015, implement phase).");
        return sources[relative];
    }
}
