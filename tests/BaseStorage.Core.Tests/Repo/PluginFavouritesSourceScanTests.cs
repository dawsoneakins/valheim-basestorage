namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

/// <summary>
/// AC-2 to AC-6, AC-10, AC-11, AC-12 and AC-15 (ADR-GWIN-044, ADR-GWIN-045, contract FV1 to FV8): textual scans of the favourites
/// Plugin files. The Plugin is net462 and cannot be loaded by tests, so the rules are checked on comment-stripped, CRLF-normalised
/// source text. Every scan that reads the game-facing files first asserts that all seven Favourites/ files were read.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginFavouritesSourceScanTests
{
    private const string FavouritesFolder = "src/BaseStorage.Plugin/Favourites/";
    private const string ToggleOutcomeFile = FavouritesFolder + "FavouriteToggleOutcome.cs";
    private const string AdapterFile = FavouritesFolder + "ProfileFavourites.cs";
    private const string OnLeftDownFile = FavouritesFolder + "InventoryGridOnLeftDownPatch.cs";
    private const string UpdateGuiFile = FavouritesFolder + "InventoryGridUpdateGuiPatch.cs";
    private const string ClicksFile = FavouritesFolder + "FavouriteClicks.cs";
    private const string SessionFile = FavouritesFolder + "FavouriteSession.cs";
    private const string RestockIdentityFile = "src/BaseStorage.Plugin/Restock/GameItemIdentity.cs"; // Edited by S-QR-002 (ADR-GWIN-174)
    private const string MarkersFile = FavouritesFolder + "FavouriteMarkers.cs";
    private const string CoreTestsProject = "tests/BaseStorage.Core.Tests/BaseStorage.Core.Tests.csproj";
    private const string MarkerLiteral = "\"BaseStorage.inventory.slot.favourite-marker\"";

    private static readonly string[] FavouritesFiles =
    [
        ToggleOutcomeFile,
        AdapterFile,
        OnLeftDownFile,
        UpdateGuiFile,
        ClicksFile,
        SessionFile,
        MarkersFile,
    ];

    private static readonly string[] LinkedFiles = [ToggleOutcomeFile, AdapterFile];

    // GW-U-709
    [Fact]
    public void TheLinkedAdapterFilesNameNoGameType()
    {
        var hits = new List<string>();
        foreach (string file in LinkedFiles)
        {
            string text = PluginSourceScan.StripComments(RepoRoot.ReadText(file));
            foreach (Match match in Regex.Matches(text, @"\b(UnityEngine|HarmonyLib|BepInEx|Player|Inventory|InventoryGui|InventoryGrid|ZInput|MessageHud|Localization|Notices)\b"))
            {
                hits.Add(file + ": " + match.Value);
            }
        }

        Assert.Empty(hits);
    }

    // GW-U-709
    [Fact]
    public void TheLinkedAdapterFilesUseOnlyTheAllowedUsingDirectives()
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "System",
            "System.Collections.Generic",
            "BaseStorage.Core.Persistence",
            "BaseStorage.Core.Ui",
        };
        var offenders = new List<string>();
        foreach (string file in LinkedFiles)
        {
            string text = PluginSourceScan.StripComments(RepoRoot.ReadText(file));
            foreach (Match match in Regex.Matches(text, @"^\s*using\s+([^;]+);", RegexOptions.Multiline))
            {
                string name = match.Groups[1].Value.Trim();
                if (!allowed.Contains(name))
                {
                    offenders.Add(file + ": using " + name);
                }
            }
        }

        Assert.Empty(offenders);
    }

    // GW-U-709 (edited by integration: the union of S-E003-005 and S-E003-006, thirty-three; edited by S-E003-006, ADR-GWIN-132: the six Building links join, thirty; edited by S-E005-002, ADR-GWIN-059: the three Reservations links join the eight; edited by S-E004-003, ADR-GWIN-096: the two ChestCategories links join the eleven; edited by S-E003-008, 8.3 errata 3: CraftFromBaseGate.cs joins, thirty-four; edited by integration (S-E003-008 + S-E004-015): the union of both sides, forty)
    [Fact]
    public void TheCoreTestsProjectLinksExactlyTheFortyContractedFiles()
    {
        XDocument project = RepoRoot.LoadXml(CoreTestsProject);
        string[] links = project.Descendants()
            .Where(element => element.Name.LocalName == "Compile")
            .Where(element => (element.Attribute("Link")?.Value ?? string.Empty).StartsWith("Linked\\", StringComparison.Ordinal))
            .Select(element => element.Attribute("Link")!.Value + "=" + element.Attribute("Include")!.Value)
            .OrderBy(link => link, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "Linked\\BuildEnd.cs=..\\..\\src\\BaseStorage.Plugin\\Building\\BuildEnd.cs",
                "Linked\\BuildGateReason.cs=..\\..\\src\\BaseStorage.Plugin\\Building\\BuildGateReason.cs",
                "Linked\\BuildMessages.cs=..\\..\\src\\BaseStorage.Plugin\\Building\\BuildMessages.cs",
                "Linked\\BuildSelection.cs=..\\..\\src\\BaseStorage.Plugin\\Building\\BuildSelection.cs",
                "Linked\\BuildSession.cs=..\\..\\src\\BaseStorage.Plugin\\Building\\BuildSession.cs",
                "Linked\\ChestCategoryWriteDriver.cs=..\\..\\src\\BaseStorage.Plugin\\ChestCategories\\ChestCategoryWriteDriver.cs",
                "Linked\\CraftContainerLayer.cs=..\\..\\src\\BaseStorage.Plugin\\Crafting\\CraftContainerLayer.cs",
                "Linked\\CraftEnd.cs=..\\..\\src\\BaseStorage.Plugin\\Crafting\\CraftEnd.cs",
                // Edited by S-E003-008, craft-from-base-toggle 8.3 errata 3 (operator-approved 2026-10-29)
                "Linked\\CraftFromBaseGate.cs=..\\..\\src\\BaseStorage.Plugin\\Patching\\CraftFromBaseGate.cs",
                "Linked\\CraftGateReason.cs=..\\..\\src\\BaseStorage.Plugin\\Crafting\\CraftGateReason.cs",
                "Linked\\CraftMessages.cs=..\\..\\src\\BaseStorage.Plugin\\Crafting\\CraftMessages.cs",
                "Linked\\CraftPhase.cs=..\\..\\src\\BaseStorage.Plugin\\Crafting\\CraftPhase.cs",
                "Linked\\CraftRequirementLines.cs=..\\..\\src\\BaseStorage.Plugin\\Crafting\\CraftRequirementLines.cs",
                "Linked\\CraftSelection.cs=..\\..\\src\\BaseStorage.Plugin\\Crafting\\CraftSelection.cs",
                "Linked\\CraftSession.cs=..\\..\\src\\BaseStorage.Plugin\\Crafting\\CraftSession.cs",
                "Linked\\DefaultCategoryFileWriter.cs=..\\..\\src\\BaseStorage.Plugin\\Categories\\DefaultCategoryFileWriter.cs",
                // Edited by S-E004-015 (ADR-GWIN-143): plus the two Deposit links (DepositMessages, DepositTraitRules), thirty-nine.
                "Linked\\DepositMessages.cs=..\\..\\src\\BaseStorage.Plugin\\Deposit\\DepositMessages.cs",
                "Linked\\DepositTraitRules.cs=..\\..\\src\\BaseStorage.Plugin\\Deposit\\DepositTraitRules.cs",
                "Linked\\FavouriteToggleOutcome.cs=..\\..\\src\\BaseStorage.Plugin\\Favourites\\FavouriteToggleOutcome.cs",
                "Linked\\IBuildGame.cs=..\\..\\src\\BaseStorage.Plugin\\Building\\IBuildGame.cs",
                "Linked\\IChestCategoryStorage.cs=..\\..\\src\\BaseStorage.Plugin\\ChestCategories\\IChestCategoryStorage.cs",
                "Linked\\ICraftGame.cs=..\\..\\src\\BaseStorage.Plugin\\Crafting\\ICraftGame.cs",
                "Linked\\ICraftWorld.cs=..\\..\\src\\BaseStorage.Plugin\\Crafting\\ICraftWorld.cs",
                // Edited by S-E003-005, ADR-GWIN-127: plus the three Requirements links (contract 1), so the total is 27.
                "Linked\\IRequirementDisplayWorld.cs=..\\..\\src\\BaseStorage.Plugin\\Requirements\\IRequirementDisplayWorld.cs",
                "Linked\\IReservationGame.cs=..\\..\\src\\BaseStorage.Plugin\\Reservations\\IReservationGame.cs",
                "Linked\\LoadedRegistry.cs=..\\..\\src\\BaseStorage.Plugin\\World\\LoadedRegistry.cs",
                "Linked\\PatchSafety.cs=..\\..\\src\\BaseStorage.Plugin\\Patching\\PatchSafety.cs",
                "Linked\\ProfileFavourites.cs=..\\..\\src\\BaseStorage.Plugin\\Favourites\\ProfileFavourites.cs",
                "Linked\\RecipeLine.cs=..\\..\\src\\BaseStorage.Plugin\\Crafting\\RecipeLine.cs",
                "Linked\\RegistryKind.cs=..\\..\\src\\BaseStorage.Plugin\\World\\RegistryKind.cs",
                "Linked\\RegistryMessages.cs=..\\..\\src\\BaseStorage.Plugin\\World\\RegistryMessages.cs",
                "Linked\\RequirementDisplayContext.cs=..\\..\\src\\BaseStorage.Plugin\\Requirements\\RequirementDisplayContext.cs",
                "Linked\\RequirementDisplaySession.cs=..\\..\\src\\BaseStorage.Plugin\\Requirements\\RequirementDisplaySession.cs",
                "Linked\\ReservationDriver.cs=..\\..\\src\\BaseStorage.Plugin\\Reservations\\ReservationDriver.cs",
                "Linked\\ReservationMessages.cs=..\\..\\src\\BaseStorage.Plugin\\Reservations\\ReservationMessages.cs",
                "Linked\\SnapshotMemo.cs=..\\..\\src\\BaseStorage.Plugin\\World\\SnapshotMemo.cs",
                // Edited by S-E004-012 (ADR-GWIN-136): plus the four game-free StationActions links, thirty-seven.
                "Linked\\StationActionsFacts.cs=..\\..\\src\\BaseStorage.Plugin\\StationActions\\StationActionsFacts.cs",
                "Linked\\StationActionsMemo.cs=..\\..\\src\\BaseStorage.Plugin\\StationActions\\StationActionsMemo.cs",
                "Linked\\StationActionsMessages.cs=..\\..\\src\\BaseStorage.Plugin\\StationActions\\StationActionsMessages.cs",
                "Linked\\StationPanelGeometry.cs=..\\..\\src\\BaseStorage.Plugin\\StationActions\\StationPanelGeometry.cs",
            },
            links);
    }

    // GW-U-710
    [Fact]
    public void TheCustomDataFieldIsReadOnlyInTheSessionFileOnly()
    {
        Assert.Equal(new[] { SessionFile, RestockIdentityFile }, PluginSourceScan.FilesMatching(ScanWithFavourites(), "m_customData")); // Edited by S-QR-002 (ADR-GWIN-174)
    }

    // GW-U-710
    [Fact]
    public void ThePersistenceKeyIsUsedOnlyByTheAdapter()
    {
        Assert.Equal(new[] { "src/BaseStorage.Plugin/Favourites/FavouriteItemStore.cs", AdapterFile }, PluginSourceScan.FilesMatching(ScanWithFavourites(), @"PersistenceKeys\.Favourites\b"));
    }

    // GW-U-710
    [Fact]
    public void NoFavouritesFileSavesClearsEnumeratesOrCopiesTheDictionary()
    {
        string[] forbidden =
        [
            @"\.Clear\(",
            "SavePlayerProfile",
            @"PlayerProfile\.Save",
            @"\.Save\(",
            @"foreach\s*\([^)]*(CustomData|m_customData)",
            @"\.Keys\b",
            @"\.Values\b",
            @"CopyTo\(",
        ];

        Dictionary<string, string> sources = ScanWithFavourites();
        var hits = new List<string>();
        foreach (string file in FavouritesFiles)
        {
            foreach (string pattern in forbidden)
            {
                if (Regex.IsMatch(sources[file], pattern))
                {
                    hits.Add(file + " matches " + pattern);
                }
            }
        }

        Assert.Empty(hits);
    }

    // GW-U-711
    [Fact]
    public void TheLeftDownPatchClassHasTheContractedShape()
    {
        string text = Flat(ScanWithFavourites()[OnLeftDownFile]);

        Assert.Contains("internal static class InventoryGridOnLeftDownPatch", text, StringComparison.Ordinal);
        Assert.Contains("[HarmonyPatch(typeof(InventoryGrid), PatchTargetNames.InventoryGrid.OnLeftDown)]", text, StringComparison.Ordinal);
        Assert.Contains(
            "internal const string PatchKey = \"BaseStorage.Plugin.Favourites.InventoryGridOnLeftDownPatch.Prefix\";",
            text,
            StringComparison.Ordinal);
        Assert.Contains(
            "private static bool Prefix(InventoryGrid __instance, UIInputHandler clickHandler) => PatchSafety.RunPrefix(PatchKey, () => FavouriteClicks.RunVanilla(__instance, clickHandler));",
            text,
            StringComparison.Ordinal);
    }

    // GW-U-711
    [Fact]
    public void TheUpdateGuiPatchClassHasTheContractedShape()
    {
        string text = Flat(ScanWithFavourites()[UpdateGuiFile]);

        Assert.Contains("internal static class InventoryGridUpdateGuiPatch", text, StringComparison.Ordinal);
        Assert.Contains("[HarmonyPatch(typeof(InventoryGrid), PatchTargetNames.InventoryGrid.UpdateGui)]", text, StringComparison.Ordinal);
        Assert.Contains(
            "internal const string PatchKey = \"BaseStorage.Plugin.Favourites.InventoryGridUpdateGuiPatch.Postfix\";",
            text,
            StringComparison.Ordinal);
        Assert.Contains(
            "private static void Postfix(InventoryGrid __instance) => PatchSafety.Run(PatchKey, __instance, static grid => FavouriteMarkers.Refresh(grid));",
            text,
            StringComparison.Ordinal);
    }

    // GW-U-711
    [Fact]
    public void NoHarmonyPatchNamesItsMethodWithAStringLiteral()
    {
        Assert.Empty(PluginSourceScan.FilesMatching(ScanWithFavourites(), "\\[HarmonyPatch\\([^\\n]*,\\s*\""));
    }

    // GW-U-711
    [Fact]
    public void TheTwoFavouritePatchClassesAreAmongExactlyTheTwentyThreeFilesThatCarryHarmonyPatch()
    {
        // Edited by S-E004-012 (ADR-GWIN-136): plus the station-actions UpdateRecipe postfix file, twenty-three files.
        // Edited by S-E003-006 (ADR-GWIN-132): plus the three Building patch classes (eighteen files).
        // Edited by S-E003-004 (ADR-GWIN-124): plus the three Crafting prefixes (fifteen files).
        // Edited by S-E003-005, ADR-GWIN-127: plus the three Requirements display patch files (eighteen files).
        // Edited by integration (S-E003-005 + S-E003-006): the union, 15 + 3 + 3 = 21 files.
        // ADR-GWIN-055: the union with the S-E002-004 World and S-E002-005 Hover patch classes; ADR-GWIN-059: plus the S-E005-002 Reservations patches; ADR-GWIN-104: plus the S-E004-004 Category button postfix.
        // Edited by integration (S-E004-004): plus the Category button patch class, 22 files.
        Assert.Equal(
            new[]
            {
                "src/BaseStorage.Plugin/BaseMenu/InventoryGridGamepadPatch.cs",
                "src/BaseStorage.Plugin/BaseMenu/InventoryGuiHidePatch.cs",
                "src/BaseStorage.Plugin/BaseMenu/InventoryGuiRecipeGamepadPatch.cs",
                "src/BaseStorage.Plugin/Building/PlayerHaveRequirementsPiecePatch.cs",
                "src/BaseStorage.Plugin/Building/PlayerPlacePiecePatch.cs",
                "src/BaseStorage.Plugin/Building/PlayerTryPlacePiecePatch.cs",
                "src/BaseStorage.Plugin/CategoryButton/InventoryGuiUpdateContainerPatch.cs",
                // Edited by S-E010-002 (organize-console-command 6.2): the console registration patch file.
                "src/BaseStorage.Plugin/Commands/TerminalInitTerminalPatch.cs",
                "src/BaseStorage.Plugin/Crafting/InventoryGuiDoCraftingPatch.cs",
                "src/BaseStorage.Plugin/Crafting/PlayerConsumeResourcesPatch.cs",
                "src/BaseStorage.Plugin/Crafting/PlayerHaveRequirementItemsPatch.cs",
                "src/BaseStorage.Plugin/Favourites/HumanoidIsItemEquipedPatch.cs",
                OnLeftDownFile,
                UpdateGuiFile,
                "src/BaseStorage.Plugin/Favourites/InventoryStackAllPatch.cs",
                "src/BaseStorage.Plugin/Feeding/CookingStationAwakePatch.cs",
                "src/BaseStorage.Plugin/Feeding/CookingStationHoverTextPatch.cs",
                "src/BaseStorage.Plugin/Feeding/CookingStationInteractPatch.cs",
                "src/BaseStorage.Plugin/Feeding/FermenterAwakePatch.cs",
                "src/BaseStorage.Plugin/Feeding/FermenterHoverTextPatch.cs",
                "src/BaseStorage.Plugin/Feeding/FermenterInteractPatch.cs",
                "src/BaseStorage.Plugin/Feeding/FireplaceAwakePatch.cs",
                "src/BaseStorage.Plugin/Feeding/FireplaceHoverTextPatch.cs",
                "src/BaseStorage.Plugin/Feeding/FireplaceInteractPatch.cs",
                "src/BaseStorage.Plugin/Feeding/SmelterAwakePatch.cs",
                "src/BaseStorage.Plugin/Feeding/SwitchGetHoverTextPatch.cs",
                "src/BaseStorage.Plugin/Feeding/SwitchInteractPatch.cs",
                "src/BaseStorage.Plugin/Hover/ContainerHoverTextPatch.cs",
                "src/BaseStorage.Plugin/Hover/CraftingStationHoverTextPatch.cs",
                "src/BaseStorage.Plugin/Networking/FejdStartupShowConnectErrorPatch.cs",
                "src/BaseStorage.Plugin/Networking/ZNetOnNewConnectionPatch.cs",
                "src/BaseStorage.Plugin/Requirements/HudSetupPieceInfoPatch.cs",
                "src/BaseStorage.Plugin/Requirements/InventoryGuiSetupRequirementListPatch.cs",
                "src/BaseStorage.Plugin/Requirements/InventoryGuiSetupRequirementPatch.cs",
                "src/BaseStorage.Plugin/Reservations/ContainerInteractPatch.cs",
                "src/BaseStorage.Plugin/Reservations/ContainerOpenResponsePatch.cs",
                "src/BaseStorage.Plugin/StationActions/InventoryGuiUpdateRecipePatch.cs", // Edited by S-E004-012 (ADR-GWIN-136).
                "src/BaseStorage.Plugin/Tooltips/ItemDataGetTooltipPatch.cs",
                "src/BaseStorage.Plugin/World/ContainerAwakePatch.cs",
                "src/BaseStorage.Plugin/World/CraftingStationOnDestroyPatch.cs",
                "src/BaseStorage.Plugin/World/CraftingStationStartPatch.cs",
                "src/BaseStorage.Plugin/World/ZNetViewOnDestroyPatch.cs",
            },
            PluginSourceScan.FilesMatching(PluginSourceScan.Sources(), @"\[HarmonyPatch\("));
    }

    // GW-U-712
    [Fact]
    public void TheDecisionAnchorsAppearInTheContractOrderWithAltFirst()
    {
        string text = ClicksText();
        string[] anchors =
        [
            "KeyCode.LeftAlt",
            "InventoryGui.instance",
            "m_playerGrid",
            "m_dragItem",
            "FavouriteSession.ItemsForLocalPlayer()",
            "GetButtonPos(",
            "GetItemAt(",
            ".Toggle(",
        ];

        int previous = -1;
        foreach (string anchor in anchors)
        {
            int at = text.IndexOf(anchor, StringComparison.Ordinal);
            Assert.True(at >= 0, $"FavouriteClicks.cs has no '{anchor}'.");
            Assert.True(at > previous, $"'{anchor}' at {at} does not follow the previous anchor at {previous}.");
            previous = at;
        }
    }

    // GW-U-712
    [Fact]
    public void TheRightAltIsAcceptedAndTheGridIdentityIsAReferenceCheck()
    {
        string text = ClicksText();

        Assert.Contains("KeyCode.RightAlt", text, StringComparison.Ordinal);
        Assert.Contains("ReferenceEquals(", text, StringComparison.Ordinal);
    }

    // GW-U-712
    [Fact]
    public void ShiftControlAndRightClickAreNeverHandled()
    {
        string text = ClicksText();

        Assert.DoesNotContain("KeyCode.LeftShift", text, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyCode.LeftControl", text, StringComparison.Ordinal);
        Assert.DoesNotContain("OnRightDown", text, StringComparison.Ordinal);
    }

    // GW-U-712
    [Fact]
    public void NoPluginFileMentionsTheExcludedHandlersOrGridFields()
    {
        Assert.Equal(new[] { "src/BaseStorage.Plugin/BaseMenu/InventoryGridGamepadPatch.cs" }, PluginSourceScan.FilesMatching(ScanWithFavourites(), "OnRightDown|UpdateGamepad|m_containerGrid|OnSelectedItem")); // Pauses the grid for the mod's panels; it does not touch favourites.
    }

    // GW-U-713
    [Fact]
    public void ExactlyOneHandledReturnFalseComesAfterTheNoticeAndTheToggle()
    {
        string text = ClicksText();
        MatchCollection falses = Regex.Matches(text, @"return\s+false\s*;");

        Assert.Single(falses);
        int notice = text.IndexOf("Notices.Show(", StringComparison.Ordinal);
        int toggle = text.IndexOf(".Toggle(", StringComparison.Ordinal);
        Assert.True(toggle >= 0 && notice > toggle, "Notices.Show must follow .Toggle(.");
        Assert.True(falses[0].Index > notice, "return false must follow Notices.Show(.");
    }

    // GW-U-713
    [Fact]
    public void EveryOtherReturnIsReturnTrue()
    {
        var returns = Regex.Matches(ClicksText(), @"\breturn\b[^;]*;").Select(match => match.Value).ToArray();
        string[] others = returns
            .Where(value => !Regex.IsMatch(value, @"^return\s+(true|false)\s*;$"))
            .ToArray();

        Assert.Empty(others);
        Assert.Contains(returns, value => Regex.IsMatch(value, @"^return\s+true\s*;$"));
    }

    // GW-U-713
    [Fact]
    public void AHandledClickTouchesNoVanillaSelectionOrItemState()
    {
        string text = ClicksText();
        string[] forbidden =
        [
            "m_onSelected",
            "OnSelectedItem",
            @"m_dragItem\s*=[^=]",
            "RemoveItem",
            "AddItem",
            "MoveItem",
        ];

        var hits = forbidden.Where(pattern => Regex.IsMatch(text, pattern)).ToArray();

        Assert.Empty(hits);
        Assert.Contains(
            "internal static bool RunVanilla(InventoryGrid grid, UIInputHandler clickHandler)",
            Flat(text),
            StringComparison.Ordinal);
    }

    // GW-U-714
    [Fact]
    public void ExactlyOneTopLeftNoticeIsShownFromTheMessageTable()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        string literal = "Notices.Show(NoticePlacement.TopLeft, ProfileFavourites.MessageKeyFor(";

        Assert.Equal(1, PluginSourceScan.CountMatches(sources, Regex.Escape(literal)));
        Assert.Equal(new[] { ClicksFile }, PluginSourceScan.FilesMatching(sources, Regex.Escape(literal)));
    }

    // GW-U-714
    [Fact]
    public void TheLocalisedNameIsNullCheckedAndFallsBackToTheVanillaName()
    {
        string text = ClicksText();

        int check = text.IndexOf("Localization.instance != null", StringComparison.Ordinal);
        int localize = text.IndexOf(".Localize(", StringComparison.Ordinal);
        Assert.True(check >= 0, "The localisation must be null-checked.");
        Assert.True(localize > check, ".Localize( must follow the null check.");
        Assert.Contains("m_shared.m_name", text, StringComparison.Ordinal);
    }

    // GW-U-714
    [Fact]
    public void NoFavouritesFileHasAnEnglishLiteral()
    {
        Dictionary<string, string> sources = ScanWithFavourites();
        var hits = new List<string>();
        foreach (string file in FavouritesFiles)
        {
            if (sources[file].Contains("\"Favourite", StringComparison.Ordinal) || sources[file].Contains("\"No longer", StringComparison.Ordinal))
            {
                hits.Add(file);
            }
        }

        Assert.Empty(hits);
    }

    // GW-U-715
    [Fact]
    public void TheMarkerNameLiteralOccursOnceInThePluginInTheMarkerFile()
    {
        Dictionary<string, string> sources = ScanWithFavourites();

        Assert.Equal(1, PluginSourceScan.CountMatches(sources, Regex.Escape(MarkerLiteral)));
        Assert.Equal(new[] { MarkersFile }, PluginSourceScan.FilesMatching(sources, Regex.Escape(MarkerLiteral)));
    }

    // GW-U-715
    [Fact]
    public void TheMarkerIsFoundBeforeItIsInstantiatedFromTheQueuedImage()
    {
        string text = MarkersText();

        int find = text.IndexOf(".Find(", StringComparison.Ordinal);
        int instantiate = text.IndexOf("Instantiate(", StringComparison.Ordinal);
        Assert.True(find >= 0, "The marker lookup must call .Find(.");
        Assert.True(instantiate > find, "The lookup must precede Instantiate(.");
    }

    // GW-U-715
    [Fact]
    public void TheOnlyInstantiateCopiesTheQueuedImage()
    {
        string text = MarkersText();

        Assert.Single(Regex.Matches(text, @"Instantiate\("));
        Assert.Contains("Instantiate(element.m_queued,", text, StringComparison.Ordinal);
    }

    // GW-U-715
    [Fact]
    public void TheMarkerIsNonInteractiveAndSitsBottomRightOnTopOfTheIcon()
    {
        string text = MarkersText();

        Assert.Contains("raycastTarget = false", text, StringComparison.Ordinal);
        Assert.True(Regex.Matches(text, @"new Vector2\(1f, 0f\)").Count >= 3, "anchorMin, anchorMax and pivot must be bottom-right.");
        Assert.Contains("clone.enabled = true", text, StringComparison.Ordinal);
        Assert.Contains("SetAsLastSibling()", text, StringComparison.Ordinal);
        Assert.Contains("m_icon.transform.parent", text, StringComparison.Ordinal);
        Assert.Contains("m_playerGrid", text, StringComparison.Ordinal);
        Assert.Contains("ReferenceEquals(", text, StringComparison.Ordinal);
        Assert.True(SetActiveIsGuardedByActiveSelf(text), "SetActive( must sit inside a branch guarded by activeSelf.");
    }

    // GW-U-715
    [Fact]
    public void TheMarkerSizeAndBrandColourAreTheContractConstants()
    {
        string text = MarkersText();

        Assert.Matches(@"MarkerSize\s*=\s*22f\b", text);
        Assert.Matches(@"BrandPrimaryR\s*=\s*1f\b", text);
        Assert.Matches(@"BrandPrimaryG\s*=\s*0\.6470588f\b", text);
        Assert.Matches(@"BrandPrimaryB\s*=\s*0f\b", text);
    }

    // GW-U-716
    [Fact]
    public void TheRefresherAllocatesNothingPerFrame()
    {
        string text = MarkersText();
        string[] forbidden =
        [
            "using System.Linq",
            "$\"",
            "string.Format",
            "string.Concat",
            "+ \"",
            "new List<",
            "new Dictionary<",
            "new HashSet<",
            ".ToArray(",
            ".ToList(",
        ];

        var hits = forbidden.Where(token => text.Contains(token, StringComparison.Ordinal)).ToList();
        if (Regex.IsMatch(text, @"foreach\s*\([^)]*\.(Where|Select|OrderBy|Concat|Distinct)\("))
        {
            hits.Add("foreach over a LINQ result");
        }

        Assert.Empty(hits);
    }

    // GW-U-716
    [Fact]
    public void TheMarkerArrayIsReallocatedOnlyInTheSizeChangeBranchAndRefreshHasNoLambda()
    {
        string text = MarkersText();

        Assert.Single(Regex.Matches(text, @"new Image\?\[", RegexOptions.None));
        int guard = text.IndexOf("markers.Length !=", StringComparison.Ordinal);
        int allocation = text.IndexOf("new Image?[", StringComparison.Ordinal);
        Assert.True(guard >= 0 && allocation > guard, "The array must be allocated inside the markers.Length != branch.");

        Assert.Contains("internal static void Refresh(InventoryGrid grid)", Flat(text), StringComparison.Ordinal);
        string body = PluginSourceScan.MethodBody(text, @"internal static void Refresh\(InventoryGrid grid\)");
        Assert.DoesNotContain("=>", body, StringComparison.Ordinal);
    }

    // GW-U-717
    [Fact]
    public void NoFavouritesFileTouchesTheSaveFileOrAnotherPersistenceApi()
    {
        string[] forbidden =
        [
            "ZPackage",
            @"Player\.Save",
            @"Player\.Load",
            "PlayerProfile",
            "DontDestroyOnLoad",
            "PlayerPrefs",
            @"\bFile\.",
        ];

        Dictionary<string, string> sources = ScanWithFavourites();
        var hits = new List<string>();
        foreach (string file in FavouritesFiles)
        {
            foreach (string pattern in forbidden)
            {
                if (Regex.IsMatch(sources[file], pattern))
                {
                    hits.Add(file + " matches " + pattern);
                }
            }
        }

        Assert.Empty(hits);
    }

    // GW-U-717
    [Fact]
    public void TheAdrDocumentsThatVanillaIgnoresTheKey()
    {
        const string adr = "docs/adr/0001-basestorage-keys-ignored-by-vanilla.md";

        Assert.True(RepoRoot.Exists(adr), $"{adr} is missing.");
        Assert.Contains("basestorage.fav", RepoRoot.ReadText(adr), StringComparison.Ordinal);
    }

    /// <summary>Scans the Plugin and asserts that every Favourites/ file was read, so an empty folder cannot pass.</summary>
    private static Dictionary<string, string> ScanWithFavourites()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        foreach (string file in FavouritesFiles)
        {
            Assert.True(sources.ContainsKey(file), $"Scan did not read {file}.");
        }

        return sources;
    }

    private static string ClicksText() => ScanWithFavourites()[ClicksFile];

    private static string MarkersText() => ScanWithFavourites()[MarkersFile];

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

    // Edited by S-QR-002 (ADR-GWIN-174): GameItemIdentity reads m_customData only as the read `item.m_customData ??`.
    [Fact]
    public void GameItemIdentityReadsCustomDataOnly()
    {
        string text = PluginSourceScan.StripComments(RepoRoot.ReadText(RestockIdentityFile));
        Assert.Contains("item.m_customData ??", text, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"m_customData\s*=[^=]"), text);
        Assert.DoesNotMatch(new Regex(@"m_customData\s*\["), text);
        Assert.DoesNotMatch(new Regex(@"m_customData\s*\.\s*(Add|Remove|Clear)\("), text);
    }
}
