namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;
using BaseStorage.Core.Tests.Ui;

/// <summary>
/// S-E005-002 source rules for src/BaseStorage.Plugin/Reservations/ (contract 4.5 SR1-SR8, 4.2 wiring, ADR-GWIN-057): the
/// game-free files are game-free and linked, the adapter is thin, no file takes a chest from a player, the patches have the
/// contracted shape, the port computes the contracted facts, and nothing in the folder waits or runs per frame on its own.
/// Rows: GW-U-763, GW-U-764, GW-U-772, GW-U-776, GW-U-777, GW-U-783, GW-U-784, GW-U-788, GW-U-794.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginReservationsSourceScanTests
{
    private const string Folder = "src/BaseStorage.Plugin/Reservations/";
    private const string Driver = Folder + "ReservationDriver.cs";
    private const string Port = Folder + "GameReservationPort.cs";
    private const string Holder = Folder + "PluginReservations.cs";
    private const string Messages = Folder + "ReservationMessages.cs";
    private const string Game = Folder + "IReservationGame.cs";
    private const string ResponsePatch = Folder + "ContainerOpenResponsePatch.cs";
    private const string InteractPatch = Folder + "ContainerInteractPatch.cs";
    private const string SnapshotSource = "src/BaseStorage.Plugin/World/GameSnapshotSource.cs";

    private static readonly string[] GameFreeFiles = { Game, Driver, Messages };
    private static readonly string[] GameBoundFiles = { Port, Holder, ResponsePatch, InteractPatch };

    // GW-U-763 (SR5): the three game-free files import no game namespace and name no game type.
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

            foreach (string word in new[] { "ZNetView", "ZDO", "ZNet", "InventoryGui" })
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

    // GW-U-763: Core.Tests links exactly the three game-free files and none of the game-bound ones, with no Plugin ProjectReference.
    [Fact]
    public void CoreTestsLinkExactlyTheThreeGameFreeFiles()
    {
        string[] links = RepoRoot.Named(RepoRoot.LoadXml(ContractPaths.CoreTestsProject), "Compile")
            .Select(e => RepoRoot.Attr(e, "Include").Replace('\\', '/'))
            .Where(include => include.Contains("/Reservations/", StringComparison.Ordinal))
            .OrderBy(include => include, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "../../src/BaseStorage.Plugin/Reservations/IReservationGame.cs",
                "../../src/BaseStorage.Plugin/Reservations/ReservationDriver.cs",
                "../../src/BaseStorage.Plugin/Reservations/ReservationMessages.cs",
            },
            links);
        foreach (string file in GameBoundFiles)
        {
            Assert.DoesNotContain(links, link => link.EndsWith(Path.GetFileName(file), StringComparison.Ordinal));
        }

        Assert.DoesNotContain(
            RepoRoot.ValuesOf(ContractPaths.CoreTestsProject, "ProjectReference").Concat(RepoRoot.Named(RepoRoot.LoadXml(ContractPaths.CoreTestsProject), "ProjectReference").Select(e => RepoRoot.Attr(e, "Include"))),
            value => value.Contains("BaseStorage.Plugin", StringComparison.Ordinal));
    }

    // GW-U-764 (RA1): the rules are consulted only by the driver.
    [Fact]
    public void OnlyTheDriverConsultsTheRules()
    {
        Dictionary<string, string> sources = Sources();

        string[] rules = FolderFiles(sources).Where(file => sources[file].Contains("ChestAccessRules.", StringComparison.Ordinal)).ToArray();
        string[] routes = FolderFiles(sources).Where(file => sources[file].Contains("ReservationRoute.", StringComparison.Ordinal)).ToArray();

        Assert.Equal(new[] { Driver }, rules);
        Assert.Equal(new[] { Driver }, routes);
    }

    // GW-U-764 (contract 4.2): Boot and Update wiring.
    [Fact]
    public void BootInitialisesReservationsBetweenWorldAndPatchesAndUpdateTicksFirst()
    {
        string entry = Sources()[PluginSourceScan.EntryPoint];
        string boot = PluginSourceScan.MethodBody(entry, @"void\s+Boot\s*\(\s*\)");
        string update = Regex.Replace(PluginSourceScan.MethodBody(entry, @"void\s+Update\s*\(\s*\)"), @"\s+", string.Empty);

        Assert.Single(Regex.Matches(boot, Regex.Escape("PluginReservations.Initialize(new GameClock(), Settings, sink)")));
        int world = boot.IndexOf("PluginWorld.Initialize(new GameClock())", StringComparison.Ordinal);
        int reservations = boot.IndexOf("PluginReservations.Initialize(", StringComparison.Ordinal);
        int apply = boot.IndexOf("PatchApplier.ApplyAll(", StringComparison.Ordinal);
        Assert.True(world >= 0 && world < reservations && reservations < apply, "Boot order must be PluginWorld.Initialize, PluginReservations.Initialize, PatchApplier.ApplyAll.");
        // Edited by S-E003-006, ADR-GWIN-132 (GW-U-1642): the building session ticks right after the crafting session.
        Assert.StartsWith("PluginReservations.Tick();PluginCrafting.Tick();PluginBuilding.Tick();", update, StringComparison.Ordinal);
    }

    // GW-U-764: the holder is an internal static class in the contracted namespace.
    [Fact]
    public void PluginReservationsIsAnInternalStaticClass()
    {
        string holder = Sources()[Holder];

        Assert.Matches(@"(?m)^namespace BaseStorage\.Plugin\.Reservations;", holder);
        Assert.Matches(@"internal\s+static\s+class\s+PluginReservations\b", holder);
        Assert.Matches(@"internal\s+static\s+ReservationDriver<Container>\?\s+Current\s*\{\s*get;\s*private\s+set;\s*\}", holder);
    }

    // GW-U-772 (SR1 time tokens, SR7): nothing waits, and nothing runs per frame on its own.
    [Fact]
    public void NothingInTheFolderWaitsOrRunsPerFrameOnItsOwn()
    {
        Dictionary<string, string> sources = Sources();
        var offenders = new List<string>();
        foreach (string file in FolderFiles(sources))
        {
            string code = sources[file];
            foreach (string token in new[] { "Thread", "Task.", "Sleep(", "DateTime", "Stopwatch" })
            {
                if (code.Contains(token, StringComparison.Ordinal))
                {
                    offenders.Add(file + " contains " + token);
                }
            }

            if (Regex.IsMatch(code, @"\b(void|bool)\s+(Update|LateUpdate|FixedUpdate|OnGUI)\s*\("))
            {
                offenders.Add(file + " declares a Unity frame method");
            }

            if (code.Contains("MonoBehaviour", StringComparison.Ordinal))
            {
                offenders.Add(file + " derives from MonoBehaviour");
            }
        }

        Assert.Empty(offenders);
    }

    // GW-U-772: the timeout setting is read once, in the holder.
    [Fact]
    public void ReserveTimeoutIsReadOnlyInTheHolder()
    {
        Dictionary<string, string> sources = Sources();

        string[] readers = FolderFiles(sources).Where(file => sources[file].Contains("settings.ReserveTimeoutMs", StringComparison.Ordinal)).ToArray();

        Assert.Equal(new[] { Holder }, readers);
        Assert.Single(Regex.Matches(sources[Holder], Regex.Escape("settings.ReserveTimeoutMs")));
    }

    // GW-U-776 (SR6): exactly two patch files, each with the contracted attribute, key and single-statement prefix.
    // Edited by S-E003-006, ADR-GWIN-132 (GW-U-1642): fifteen Prefix or Postfix bodies before, plus the three prefixes and the one postfix of the building patches.
    // Edited by integration (S-E003-005 + S-E003-006): plus the five requirement-display bodies of S-E003-005, twenty-four.
    [Fact]
    public void ThePluginHasTwentySixPrefixOrPostfixBodies()
    {
        // Edited by integration (S-E004-004): plus the Category button postfix, twenty-five.
        // Edited by S-E004-012 (ADR-GWIN-136): plus the UpdateRecipe postfix of the station actions, twenty-six.
        // Edited by S-E010-002 (organize-console-command 6.2): plus TerminalInitTerminalPatch.Postfix, twenty-seven.
        Assert.Equal(45, PluginSourceScan.CountMatches(PluginSourceScan.Sources(), @"static (bool|void) (Prefix|Postfix)\("));
    }

    // GW-U-776 (SR6): exactly two patch files, each with the contracted attribute, key and single-statement prefix.
    [Fact]
    public void TheTwoPatchFilesHaveTheContractedShape()
    {
        Dictionary<string, string> sources = Sources();
        string[] patchFiles = FolderFiles(sources).Where(file => sources[file].Contains("[HarmonyPatch", StringComparison.Ordinal)).ToArray();
        Assert.Equal(new[] { InteractPatch, ResponsePatch }, patchFiles);

        string response = sources[ResponsePatch];
        Assert.Contains("[HarmonyPatch(typeof(Container), PatchTargetNames.Container.RpcOpenResponse)]", response, StringComparison.Ordinal);
        Assert.Contains("internal const string PatchKey = \"BaseStorage.Plugin.Reservations.ContainerOpenResponsePatch.Prefix\";", response, StringComparison.Ordinal);
        Assert.Matches(@"private\s+static\s+bool\s+Prefix\(Container __instance, bool granted\)\s*=>\s*PatchSafety\.RunPrefix\(PatchKey,\s*\(\)\s*=>\s*PluginReservations\.OnOpenResponse\(__instance, granted\)\);", response);

        string interact = sources[InteractPatch];
        Assert.Contains("[HarmonyPatch(typeof(Container), PatchTargetNames.Container.Interact)]", interact, StringComparison.Ordinal);
        Assert.Contains("internal const string PatchKey = \"BaseStorage.Plugin.Reservations.ContainerInteractPatch.Prefix\";", interact, StringComparison.Ordinal);
        Assert.Matches(@"private\s+static\s+bool\s+Prefix\(Container __instance, bool hold, bool alt, ref bool __result\)\s*=>\s*PatchSafety\.RunPrefix\(PatchKey,\s*ref __result,\s*\(ref bool result\)\s*=>\s*PluginReservations\.OnInteract\(__instance, hold, alt, ref result\)\);", interact);

        foreach (string file in patchFiles)
        {
            Assert.Single(Regex.Matches(sources[file], @"\[HarmonyPatch\("));
            Assert.DoesNotMatch(@"\[HarmonyPatch\([^\]]*new\s*\[\]|new Type\[\]", sources[file]);
            Assert.DoesNotMatch(@"\b(Postfix|Transpiler|Finalizer)\b", sources[file]);
        }
    }

    // GW-U-776: no InventoryGui patch in the Plugin apart from the S-E004-004 Category button postfix (ADR-GWIN-104), which targets
    // UpdateContainer and never shows or hides the chest window; the reservation adapter itself still patches no InventoryGui method.
    [Fact]
    public void NoPluginFilePatchesInventoryGuiExceptTheCategoryButtonPostfix()
    {
        Dictionary<string, string> sources = Sources();

        // Edited by S-E003-005, ADR-GWIN-127: the allowed InventoryGui patches are the crafting DoCrafting prefix (S-E003-004, contract 5.3)
        // and the two requirement-display postfixes (contract 5.5). None of them is a Show, Hide, CloseContainer or UpdateContainer patch.
        // Edited by integration (S-E003-005 + S-E004-004): plus the S-E004-004 Category button postfix, the only UpdateContainer patch.
        string[] inventoryGuiPatches = PluginSourceScan.FilesMatching(sources, @"\[HarmonyPatch\(typeof\(InventoryGui\)");
        Assert.Equal(
            new[]
            {
                "src/BaseStorage.Plugin/BaseMenu/InventoryGuiHidePatch.cs", // Blocks Hide only while the menu's search box has focus.
                "src/BaseStorage.Plugin/BaseMenu/InventoryGuiRecipeGamepadPatch.cs", // Waits only while one of the mod's panels has the gamepad.
                "src/BaseStorage.Plugin/CategoryButton/InventoryGuiUpdateContainerPatch.cs",
                "src/BaseStorage.Plugin/Crafting/InventoryGuiDoCraftingPatch.cs",
                "src/BaseStorage.Plugin/Requirements/InventoryGuiSetupRequirementListPatch.cs",
                "src/BaseStorage.Plugin/Requirements/InventoryGuiSetupRequirementPatch.cs",
                // Edited by S-E004-012 (ADR-GWIN-136): plus the StationActions UpdateRecipe postfix (contract 6.1), which never shows or hides the chest window.
                "src/BaseStorage.Plugin/StationActions/InventoryGuiUpdateRecipePatch.cs",
            },
            inventoryGuiPatches.Select(path => path.Replace('\\', '/')).ToArray());
        Assert.Empty(PluginSourceScan.FilesMatching(sources, @"\[HarmonyPatch\(typeof\(InventoryGui\),\s*PatchTargetNames\.InventoryGui\.(Show|CloseContainer|IsContainerOpen)\b"));
        Assert.Equal(
            new[] { "src/BaseStorage.Plugin/BaseMenu/InventoryGuiHidePatch.cs" },
            PluginSourceScan.FilesMatching(sources, @"\[HarmonyPatch\(typeof\(InventoryGui\),\s*PatchTargetNames\.InventoryGui\.Hide\b"));
        Assert.Empty(PluginSourceScan.FilesMatching(sources, @"\[HarmonyPatch\(typeof\(InventoryGui\),\s*"""));
        Assert.Equal(
            new[] { "src/BaseStorage.Plugin/CategoryButton/InventoryGuiUpdateContainerPatch.cs" },
            PluginSourceScan.FilesMatching(sources, @"\[HarmonyPatch\(typeof\(InventoryGui\)[^\n]*UpdateContainer"));
    }

    // GW-U-777 (contract 4.2): the holder's entry points.
    [Fact]
    public void TheHolderEntryPointsFollowTheContract()
    {
        string holder = Sources()[Holder];

        string response = Regex.Replace(PluginSourceScan.MethodBody(holder, @"bool\s+OnOpenResponse\s*\(Container chest, bool granted\)"), @"\s+", string.Empty);
        Assert.Equal("ReservationDriver<Container>?driver=Current;if(driver==null||chest==null){returntrue;}returndriver.OnOpenResponse(chest,granted);", response);

        string interact = Regex.Replace(PluginSourceScan.MethodBody(holder, @"bool\s+OnInteract\s*\(Container chest, bool hold, bool alt, ref bool result\)"), @"\s+", string.Empty);
        Assert.Equal("ReservationDriver<Container>?driver=Current;if(driver==null||chest==null){returntrue;}boolrun=driver.OnLocalInteract(chest,hold,alt,BaseStoragePlugin.Settings?.DebugLogging==true);if(!run){result=true;}returnrun;", interact);

        Assert.Single(Regex.Matches(holder, Regex.Escape("new ReservationDriver<Container>(new GameReservationPort(), clock, settings.ReserveTimeoutMs, StringTable.English, sink)")));
    }

    // GW-U-783 (SR1, SR3, SR4): no forced close, no ownership theft.
    [Fact]
    public void NoFileTakesAChestFromAPlayer()
    {
        Dictionary<string, string> sources = Sources();
        var offenders = new List<string>();
        foreach (string file in FolderFiles(sources))
        {
            foreach (string token in new[] { "SetOwner", "ForceSendZDO", ".Hide(", "CloseContainer", "RPC_RequestStack", "RPC_RequestTakeAll", "RequestStack", "TakeAll", ".Register(" })
            {
                if (sources[file].Contains(token, StringComparison.Ordinal))
                {
                    offenders.Add(file + " contains " + token);
                }
            }
        }

        Assert.Empty(offenders);
    }

    // GW-U-783 (SR3): ClaimOwnership is a game call only in the port, and the driver calls the port only on HostClaim.
    [Fact]
    public void ClaimOwnershipIsCalledOnlyOnTheHostClaimRoute()
    {
        Dictionary<string, string> sources = Sources();

        Assert.Single(Regex.Matches(sources[Port], @"m_nview\.ClaimOwnership\(\)"));
        foreach (string file in FolderFiles(sources).Where(f => f != Port && f != Driver && f != Game))
        {
            Assert.DoesNotContain("ClaimOwnership(", sources[file], StringComparison.Ordinal);
        }

        string driver = sources[Driver];
        Assert.Single(Regex.Matches(driver, @"game\.ClaimOwnership\("));
        Match branch = Regex.Match(driver, @"case ReservationRoute\.HostClaim:(.*?)break;", RegexOptions.Singleline);
        Assert.True(branch.Success, "the driver has no HostClaim case.");
        Assert.Contains("game.ClaimOwnership(", branch.Groups[1].Value, StringComparison.Ordinal);
    }

    // GW-U-783 (SR4): the in-use marker is set on the game only in the port; the driver goes through the port.
    [Fact]
    public void SetInUseAsAGameCallOccursOnlyInThePort()
    {
        Dictionary<string, string> sources = Sources();

        Assert.Single(Regex.Matches(sources[Port], @"\bc\.SetInUse\(inUse\)"));
        foreach (string file in FolderFiles(sources).Where(f => f != Port))
        {
            foreach (Match match in Regex.Matches(sources[file], @"(\w+)\.SetInUse\("))
            {
                Assert.True(match.Groups[1].Value == "game", file + " calls SetInUse on " + match.Groups[1].Value);
            }
        }
    }

    // GW-U-784 (contract 4.3): the port's observation agrees with the snapshot source.
    [Fact]
    public void ThePortReadsTheSameAccessAndInUseExpressionsAsTheSnapshotSource()
    {
        Dictionary<string, string> sources = Sources();
        string port = sources[Port];
        string snapshot = sources[SnapshotSource];

        foreach (string token in new[] { "GetInt(ZDOVars.s_inUse, 0) == 1", "GetPlayerList()", "m_characterID.UserID", ".m_name", "c.CheckAccess(", "PrivateArea.CheckAccess(", "0f, false, false" })
        {
            Assert.Contains(token, port, StringComparison.Ordinal);
        }

        Assert.Contains("ZDOVars.s_inUse, 0) == 1", snapshot, StringComparison.Ordinal);
        Assert.Contains("0f, false, false", snapshot, StringComparison.Ordinal);
    }

    // GW-U-784: the listed-owner walk reads the player list and never the peer on that path.
    [Fact]
    public void TheListedOwnerWalkUsesThePlayerListNotThePeer()
    {
        string port = Sources()[Port];
        Match walk = Regex.Match(port, @"ZNet\.instance\.GetPlayerList\(\);(.*?)bool ownerConnected", RegexOptions.Singleline);

        Assert.True(walk.Success, "the port has no player-list walk before ownerConnected.");
        Assert.DoesNotContain("GetPeer(", walk.Groups[1].Value, StringComparison.Ordinal);
    }

    // GW-U-788 (contract 4.3 fast-path facts).
    [Fact]
    public void ThePortComputesTheFastPathFacts()
    {
        string port = Sources()[Port];

        Assert.Contains("ZNet.instance != null && ZDOMan.GetSessionID() != 0", port, StringComparison.Ordinal);
        Assert.Contains("ZNet.instance.IsServer()", port, StringComparison.Ordinal);
        Match peer = Regex.Match(port, @"else if \(ZNet\.instance != null && ZNet\.instance\.IsServer\(\)\)\s*\{\s*ownerConnected = ZNet\.instance\.GetPeer\(owner\) != null;\s*\}");
        Assert.True(peer.Success, "GetPeer must be read only inside an IsServer() branch.");
        Assert.Single(Regex.Matches(port, @"GetPeer\("));
        Assert.Contains("bool ownerConnected = ownerListed;", port, StringComparison.Ordinal);
    }

    // GW-U-788 (SR2): the one RPC send.
    [Fact]
    public void TheOnlyRpcSendIsTheOpenRequestWithThePlayerId()
    {
        Dictionary<string, string> sources = Sources();

        string[] senders = FolderFiles(sources).Where(file => sources[file].Contains("InvokeRPC(", StringComparison.Ordinal)).ToArray();
        Assert.Equal(new[] { Port }, senders);
        Assert.Single(Regex.Matches(sources[Port], @"InvokeRPC\("));
        Assert.Contains("c.m_nview.InvokeRPC(PatchTargetNames.Container.RpcRequestOpen, Game.instance.GetPlayerProfile().GetPlayerID());", sources[Port], StringComparison.Ordinal);
        foreach (string file in FolderFiles(sources))
        {
            Assert.DoesNotMatch("\"RPC_", sources[file]);
        }
    }

    // GW-U-794 (ADR-GWIN-057): the per-frame tick is wrapped, and only the two patches use RunPrefix.
    [Fact]
    public void TheTickIsWrappedAndOnlyThePatchesUseRunPrefix()
    {
        Dictionary<string, string> sources = Sources();
        string holder = sources[Holder];

        Assert.Contains("internal const string TickKey = \"BaseStorage.Plugin.Reservations.PluginReservations.Tick\";", holder, StringComparison.Ordinal);
        Assert.Matches(@"PatchSafety\.Run\(TickKey,\s*static\s*\(\)\s*=>\s*Current\?\.Tick\(\)\);", holder);
        string[] prefixUsers = FolderFiles(sources).Where(file => sources[file].Contains("PatchSafety.RunPrefix(", StringComparison.Ordinal)).ToArray();
        Assert.Equal(new[] { InteractPatch, ResponsePatch }, prefixUsers);
    }

    // GW-U-794 (SR8): no English strings-table text in a literal, and "probe" only as ProbeOperation.
    [Fact]
    public void NoLiteralHoldsStringsTableTextAndProbeIsOnlyTheOperationConstant()
    {
        Dictionary<string, string> sources = Sources();
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

                if (inner == "probe" && file != Driver)
                {
                    offenders.Add(file + ": probe literal outside the driver");
                }
            }
        }

        Assert.Empty(offenders);
        Assert.Single(Regex.Matches(sources[Driver], "\"probe\""));
        Assert.Contains("internal const string ProbeOperation = \"probe\";", sources[Driver], StringComparison.Ordinal);
    }

    private static Dictionary<string, string> Sources()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        foreach (string file in GameFreeFiles.Concat(GameBoundFiles))
        {
            Assert.True(sources.ContainsKey(file), "Scan did not read " + file);
        }

        Assert.True(sources.ContainsKey(SnapshotSource), "Scan did not read " + SnapshotSource);
        return sources;
    }

    private static string[] FolderFiles(Dictionary<string, string> sources) =>
        sources.Keys.Where(key => key.StartsWith(Folder, StringComparison.Ordinal)).OrderBy(key => key, StringComparer.Ordinal).ToArray();
}
