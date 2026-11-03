namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Source scans of the S-QR-002 Plugin Restock adapter (ADR-GWIN-174, ADR-GWIN-176): the hotkey, the runner order, the one identity mapper,
/// the capture marks and reasons, the layer shape, the real assignments, and the game members of the restock. Not a test row by itself.
/// </summary>
public sealed class PluginRestockSourceScanTests
{
    private const string Plugin = "src/BaseStorage.Plugin";
    private const string RestockFolder = "src/BaseStorage.Plugin/Restock";
    private const string RunnerFile = "src/BaseStorage.Plugin/Organize/OrganizeRunner.cs";
    private const string CaptureFile = "src/BaseStorage.Plugin/Deposit/PlayerInventoryCapture.cs";
    private const string PortFile = "src/BaseStorage.Plugin/Organize/GameOrganizeRunPort.cs";

    /// <summary>GW-U-2557: the Restock hotkey file reads the setting, parses it with HotkeyChord, polls once a second, and has every gate of the Deposit hotkey, the chat gate being held by Player.TakeInput() and not by Chat.instance; ZInput and KeyCode appear in no other Restock file.</summary>
    [Fact]
    public void TheRestockHotkeyIsGatedLikeTheDepositHotkey()
    {
        string hotkey = RepoRoot.ReadText(RestockFolder + "/RestockHotkey.cs");
        Assert.Contains("settings.RestockKey", hotkey, StringComparison.Ordinal);
        Assert.Contains("HotkeyChord.Parse(", hotkey, StringComparison.Ordinal);
        Assert.Contains("RefreshInterval", hotkey, StringComparison.Ordinal);
        Assert.Contains("Player.m_localPlayer == null", hotkey, StringComparison.Ordinal);
        // Edited by S-QR-002 (verify fix): the chat gate is Player.TakeInput(), which is already a PatchTargetNames row; Chat.instance has no row.
        foreach (string gate in new[] { "TakeInput()", "Console.IsVisible()", "TextInput.IsVisible()", "Menu.IsVisible()", "InventoryGui.IsVisible()", "UnifiedPopup.IsVisible()" })
        {
            Assert.Contains(gate, hotkey, StringComparison.Ordinal);
        }

        // Edited by S-QR-002 (verify fix): Chat.instance (assembly_valheim get_instance) has no PatchTargetNames row, so the hotkey must not read it.
        Assert.DoesNotContain("Chat.instance", hotkey, StringComparison.Ordinal);

        foreach (string file in RestockFiles())
        {
            if (Path.GetFileName(file) == "RestockHotkey.cs")
            {
                continue;
            }

            string text = RepoRoot.ReadText(file);
            Assert.DoesNotContain("ZInput", text, StringComparison.Ordinal);
            Assert.DoesNotContain("KeyCode", text, StringComparison.Ordinal);
        }
    }

    /// <summary>GW-U-2558: the runner polls the Restock key only while idle, after the Deposit key, which is after the Organize key; the branch calls StartRestock at the player's position.</summary>
    [Fact]
    public void TheRunnerPollsTheRestockKeyAfterTheDepositKey()
    {
        string runner = RepoRoot.ReadText(RunnerFile);
        int organize = runner.IndexOf("OrganizeHotkey.IsPressed(", StringComparison.Ordinal);
        int deposit = runner.IndexOf("DepositHotkey.IsPressed(", StringComparison.Ordinal);
        int restock = runner.IndexOf("coordinator.State == OrganizeRunState.Idle && RestockHotkey.IsPressed(", StringComparison.Ordinal);
        Assert.True(organize >= 0 && deposit > organize && restock > deposit, "The hotkey polls are not in the order Organize, Deposit, Restock.");
        Assert.Contains("coordinator.StartRestock(new WorldPosition(", runner, StringComparison.Ordinal);
        Assert.Contains("StartRestockAt(WorldPosition position)", runner, StringComparison.Ordinal);
    }

    /// <summary>GW-U-2565: one identity mapper reads the six game fields, it is the only Plugin site that builds an identity, and every restock read calls it.</summary>
    [Fact]
    public void EveryRestockReadUsesTheOneIdentityMapper()
    {
        string mapper = RepoRoot.ReadText(RestockFolder + "/GameItemIdentity.cs");
        Assert.Contains("internal static ItemStackIdentity Of(ItemDrop.ItemData item)", mapper, StringComparison.Ordinal);
        foreach (string field in new[] { "m_durability", "m_crafterID", "m_crafterName ??", "m_variant", "m_worldLevel", "m_customData ??" })
        {
            Assert.Contains(field, mapper, StringComparison.Ordinal);
        }

        Dictionary<string, string> sources = PluginSourceScan.Sources();
        var builders = sources.Where(pair => pair.Value.Contains("new ItemStackIdentity(", StringComparison.Ordinal)).Select(pair => Path.GetFileName(pair.Key)).ToList();
        Assert.Equal(new[] { "GameItemIdentity.cs" }, builders);

        foreach (string caller in new[] { "PlayerInventoryCapture.cs", "RestockChestCapture.cs", "GameRestockLayer.cs" })
        {
            Assert.True(sources.Any(pair => Path.GetFileName(pair.Key) == caller && pair.Value.Contains("GameItemIdentity.Of(", StringComparison.Ordinal)), caller + " does not call GameItemIdentity.Of(.");
        }
    }

    /// <summary>GW-U-2567: the Organize and Deposit snapshot source keeps the empty identity, and the restock chest capture goes through its own reader.</summary>
    [Fact]
    public void GameSnapshotSourceKeepsTheEmptyIdentity()
    {
        string snapshot = RepoRoot.ReadText(Plugin + "/World/GameSnapshotSource.cs");
        Assert.DoesNotContain("GameItemIdentity", snapshot, StringComparison.Ordinal);
        Assert.DoesNotContain("ItemStackIdentity", snapshot, StringComparison.Ordinal);

        string port = RepoRoot.ReadText(PortFile);
        Assert.Matches(new Regex(@"CaptureChests\(IReadOnlyList<ContainerSnapshot> containers\)[\s\S]*RestockChestCapture"), port);
    }

    /// <summary>GW-U-2570: the carried capture sets the hotbar and favourite marks, and never the starred slot.</summary>
    [Fact]
    public void CaptureMarksHotbarAndFavouriteTypesInThePlugin()
    {
        string capture = RepoRoot.ReadText(CaptureFile);
        Assert.Contains("CarriedMarks.Hotbar", capture, StringComparison.Ordinal);
        Assert.Contains("m_gridPos.y == 0", capture, StringComparison.Ordinal);
        Assert.Contains("CarriedMarks.FavouriteType", capture, StringComparison.Ordinal);
        Assert.Contains("FavouriteSession.IsFavourite(item)", capture, StringComparison.Ordinal);

        foreach (string file in PluginFiles())
        {
            Assert.DoesNotContain("CarriedMarks.StarredSlot", RepoRoot.ReadText(file), StringComparison.Ordinal);
        }

        // Edited by S-QR-002 (ADR-GWIN-176): PersistenceKeys.cs is the one Core file that holds the favitems key (pinned by PersistenceKeysTests).
        foreach (string file in CoreFiles().Where(path => Path.GetFileName(path) != "PersistenceKeys.cs"))
        {
            Assert.DoesNotContain("favitems", RepoRoot.ReadText(file), StringComparison.Ordinal);
        }
    }

    /// <summary>GW-U-2576: both captures return a reason with every null, neither swallows an exception silently, and the port logs the reason at Error.</summary>
    [Fact]
    public void CaptureFailuresCarryAReasonAndAreLoggedAtError()
    {
        string capture = RepoRoot.ReadText(CaptureFile);
        Assert.True(Regex.Matches(capture, @"out string\? reason").Count == 2);
        string[] lines = capture.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains("return null;", StringComparison.Ordinal))
            {
                continue;
            }

            bool preceded = Enumerable.Range(Math.Max(0, i - 4), Math.Min(4, i)).Any(j => lines[j].Contains("reason = ", StringComparison.Ordinal));
            Assert.True(preceded, "A return null without a reason at line " + (i + 1));
        }

        Assert.DoesNotMatch(new Regex(@"catch \(Exception\)"), capture);
        Assert.DoesNotMatch(new Regex(@"catch\s*\{\s*return null;\s*\}"), capture);

        string port = RepoRoot.ReadText(PortFile);
        Assert.Contains("ILogSink", port, StringComparison.Ordinal);
        Assert.Contains("LogSeverity.Error", port, StringComparison.Ordinal);
        Assert.True(Regex.Matches(port, Regex.Escape("\"Inventory capture failed: \"")).Count == 2);
        Assert.Matches(new Regex(@"new GameOrganizeRunPort\([^)]*sink"), RepoRoot.ReadText(RunnerFile));
    }

    /// <summary>GW-U-2582: the base summary passes real assignments read from the chest categories, not the empty dictionary.</summary>
    [Fact]
    public void PluginWorldPassesRealAssignmentsToEligibility()
    {
        string world = RepoRoot.ReadText(Plugin + "/World/PluginWorld.cs");
        Assert.DoesNotContain("NoAssignments", world, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"ContainerEligibility\.Evaluate\([\s\S]*?ChestCategoryWriter\.Read\("), world);
    }

    // Added by S-QR-002 (recovery attempt 1)
    /// <summary>GW-U-2629: the restock start reads each chest's category assignment with ChestCategoryWriter.Read into the facts it hands to the planner, and passes no empty dictionary (P8, AC-9).</summary>
    [Fact]
    public void GatherFactsReadsTheRealChestAssignments()
    {
        string port = RepoRoot.ReadText(PortFile);
        Assert.DoesNotContain("NoAssignments", port, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"GatherFacts\(IReadOnlyList<ContainerSnapshot> containers\)[\s\S]*?ChestCategoryWriter\.Read\(container\)[\s\S]*?assignments\[id\] = assignment;[\s\S]*?return new OrganizeBaseFacts\(assignments, inUseBy\);"), port);

        string coordinator = RepoRoot.ReadText("src/BaseStorage.Core/Organize/Run/OrganizeRunCoordinator.cs");
        Assert.Contains("OrganizeBaseFacts facts = port.GatherFacts(resolution.Containers);", coordinator, StringComparison.Ordinal);
        Assert.Contains("return StartRestockRun(resolution, facts, player, id, name);", coordinator, StringComparison.Ordinal);
    }

    /// <summary>GW-U-2597: the restock layer adds to the exact slot then removes, and checks the reservation and the identity before a remove.</summary>
    [Fact]
    public void TheRestockLayerAddsToTheExactSlotThenRemoves()
    {
        string layer = RepoRoot.ReadText(RestockFolder + "/GameRestockLayer.cs");
        Assert.True(Regex.Matches(layer, Regex.Escape(".AddItem(clone, target.m_gridPos)")).Count == 1);
        Assert.DoesNotMatch(new Regex(@"\.AddItem\((?!clone, target\.m_gridPos)"), layer);
        int clone = layer.IndexOf(".Clone()", StringComparison.Ordinal);
        int setStack = layer.IndexOf("m_stack =", StringComparison.Ordinal);
        Assert.True(clone >= 0 && setStack > clone, ".Clone() must come before m_stack =.");

        int removeAt = layer.IndexOf("RemoveFromStack(", StringComparison.Ordinal);
        Assert.True(removeAt >= 0, "RemoveFromStack is missing.");
        string remove = layer.Substring(removeAt);
        int canWrite = remove.IndexOf("reservations.CanWrite(", StringComparison.Ordinal);
        int removeItem = remove.IndexOf(".RemoveItem(", StringComparison.Ordinal);
        Assert.True(canWrite >= 0 && removeItem > canWrite, "RemoveFromStack must check reservations.CanWrite( before .RemoveItem(.");
        Assert.Contains("GetItemAt(", layer, StringComparison.Ordinal);
        Assert.Contains("DestinationFull", RepoRoot.ReadText(Plugin + "/Deposit/GameDepositContainerLayer.cs"), StringComparison.Ordinal);
    }

    /// <summary>GW-U-2625: every game member the restock code uses has a PatchTargetNames row, through a fixed token map.</summary>
    [Fact]
    public void EveryGameMemberUsedByRestockHasAPatchTargetRow()
    {
        // Edited by S-QR-002 (ADR-GWIN-176): each row is (owning nested class or null, constant name, expected constant value).
        var map = new Dictionary<string, (string? Owner, string Constant, string Value)>
        {
            ["m_crafterID"] = (null, "FieldCrafterId", "m_crafterID"),
            ["m_crafterName"] = (null, "FieldCrafterName", "m_crafterName"),
            ["m_variant"] = (null, "FieldVariant", "m_variant"),
            ["m_worldLevel"] = (null, "FieldWorldLevel", "m_worldLevel"),
            ["m_customData"] = (null, "FieldCustomData", "m_customData"),
            ["m_durability"] = (null, "FieldDurability", "m_durability"),
            ["m_gridPos"] = (null, "FieldGridPos", "m_gridPos"),
            ["m_stack"] = (null, "FieldStack", "m_stack"),
            ["m_quality"] = (null, "FieldQuality", "m_quality"),
            ["m_dropPrefab"] = (null, "FieldDropPrefab", "m_dropPrefab"),
            ["m_maxStackSize"] = (null, "FieldMaxStackSize", "m_maxStackSize"),
            [".AddItem("] = (null, "AddItem", "AddItem"),
            [".RemoveItem("] = (null, "RemoveItem", "RemoveItem"),
            [".GetItemAt("] = (null, "GetItemAt", "GetItemAt"),
            [".GetAllItems("] = (null, "GetAllItems", "GetAllItems"),
            [".Clone("] = (null, "Clone", "Clone"),
            [".GetInventory("] = (null, "GetInventory", "GetInventory"),
            ["TextInput.IsVisible("] = ("TextInput", "IsVisible", "IsVisible"),
            ["UnifiedPopup.IsVisible("] = ("UnifiedPopup", "IsVisible", "IsVisible"),
            ["InventoryGui.IsVisible("] = ("InventoryGui", "IsVisible", "IsVisible"),
            ["Menu.IsVisible("] = ("Menu", "IsVisible", "IsVisible"),
            ["Console.IsVisible("] = ("Console", "IsVisible", "IsVisible"),
            // Edited by S-QR-002 (verify fix): any Chat member read in a restock file needs a row; Chat has none for get_instance, so Chat.instance is red.
            ["Chat."] = ("Chat", "GetInstance", "get_instance"),
            // Edited by S-QR-002 (verify fix): the chat gate is Player.TakeInput(), already listed (CategoryHotkey.cs uses it).
            [".TakeInput("] = (null, "TakeInput", "TakeInput"),
        };

        string patchNames = RepoRoot.ReadText("src/Shared/PatchTargetNames.cs");
        var files = RestockFiles().Select(RepoRoot.ReadText).ToList();
        files.Add(RepoRoot.ReadText(CaptureFile));
        Assert.True(files.Count > 1, "The restock files were not read.");
        var missing = new List<string>();
        foreach (string text in files)
        {
            foreach (string token in map.Keys.Where(token => text.Contains(token, StringComparison.Ordinal)))
            {
                (string? owner, string constant, string value) = map[token];
                // A nested row is declared inside its owning class; a top-level row anywhere in PatchTargetNames.
                string declaration = owner == null
                    ? @"public const string " + Regex.Escape(constant) + @"\s*="
                    : @"public static class " + Regex.Escape(owner) + @"\s*\{[^}]*public const string " + Regex.Escape(constant) + @"\s*=";
                if (!Regex.IsMatch(patchNames, declaration + @"\s*""" + Regex.Escape(value) + @""""))
                {
                    missing.Add(token + " -> " + (owner == null ? string.Empty : owner + ".") + constant);
                }
            }
        }

        Assert.True(missing.Count == 0, "Restock uses game members with no PatchTargetNames row: " + string.Join(", ", missing.Distinct()));
    }

    // Added by S-QR-002 (recovery attempt 2)
    /// <summary>GW-U-2630: AddToCarried clamps the count to the target's room, walks the player's list to check that the first matching stack with room is the target (by reference), and only then clones and adds to the target slot (AC-12, AC-13; the game's AddItem fills the first matching stack in list order).</summary>
    [Fact]
    public void AddToCarriedOnlyEverWritesTheTargetSlot()
    {
        string layer = RepoRoot.ReadText(RestockFolder + "/GameRestockLayer.cs").Replace("\r\n", "\n");
        int start = layer.IndexOf("public bool AddToCarried(", StringComparison.Ordinal);
        Assert.True(start >= 0, "AddToCarried is missing.");
        int end = layer.IndexOf("public bool RemoveFromCarried(", start, StringComparison.Ordinal);
        Assert.True(end > start, "RemoveFromCarried must follow AddToCarried.");
        string body = layer.Substring(start, end - start);

        // 1. The count is clamped against the room of the target stack.
        int clamp = body.IndexOf("m_maxStackSize", StringComparison.Ordinal);
        Assert.True(clamp >= 0, "AddToCarried must clamp the count against m_maxStackSize.");

        // 2. The player's list is walked in order to find the first matching stack with room.
        int walk = body.IndexOf("GetAllItems(", StringComparison.Ordinal);
        Assert.True(walk > clamp, "AddToCarried must walk GetAllItems( after the clamp.");

        // 3. That first matching stack must be the target, checked by reference.
        Match check = Regex.Match(body.Substring(walk), @"ReferenceEquals\(|!=\s*target\b|\btarget\s*!=(?!\s*null\b)");
        Assert.True(check.Success, "AddToCarried must check by reference that the first matching stack with room is the target.");
        int checkAt = walk + check.Index;

        // 4. Only then is the clone made, its count set, and the clone added to the target slot, last.
        int clone = body.IndexOf(".Clone()", StringComparison.Ordinal);
        Assert.True(clone > checkAt, ".Clone() must come after the target check.");
        int setStack = body.IndexOf("m_stack =", StringComparison.Ordinal);
        Assert.True(setStack > clone, "m_stack = must come after .Clone().");
        Assert.True(Regex.Matches(body, @"\.AddItem\(").Count == 1, "AddToCarried must call AddItem exactly once.");
        int add = body.IndexOf(".AddItem(clone, target.m_gridPos)", StringComparison.Ordinal);
        Assert.True(add > setStack, "AddToCarried must add .AddItem(clone, target.m_gridPos) last.");
    }

    private static IEnumerable<string> RestockFiles() => Directory.GetFiles(RepoRoot.Combine(RestockFolder), "*.cs").Select(path => "src/BaseStorage.Plugin/Restock/" + Path.GetFileName(path));

    private static IEnumerable<string> PluginFiles() => Directory.GetFiles(RepoRoot.Combine(Plugin), "*.cs", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(RepoRoot.FullPath, path).Replace('\\', '/'));

    private static IEnumerable<string> CoreFiles() => Directory.GetFiles(RepoRoot.Combine("src/BaseStorage.Core"), "*.cs", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(RepoRoot.FullPath, path).Replace('\\', '/'));
}
