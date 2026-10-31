namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using BaseStorage.PatchTargets.Tests.Checking;
using BaseStorage.PatchTargets.Tests.Fixtures;
using BaseStorage.PatchTargets.Tests.Infrastructure;
using BaseStorage.Shared;

/// <summary>
/// AC-1, AC-2 and AC-5: every entry of PatchTargetNames resolves in the installed assembly_valheim.dll (contract 4.6).
/// DLL-backed: skipped with BST2002 or BST2003 when the game is not installed.
/// </summary>
[Collection("ValheimAssembly")]
public sealed partial class GameTargetContractTests
{
    private const BindingFlags AllDeclared = GameTargetChecker.Declared;

    private readonly ValheimAssemblyFixture fixture;

    public GameTargetContractTests(ValheimAssemblyFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>One row per classified entry of the real list, named Owner.Const (contract 4.6.2).</summary>
    public static IEnumerable<object[]> ClassifiedEntries()
    {
        foreach (TargetEntry entry in TargetList.Classify(typeof(PatchTargetNames)))
        {
            yield return new object[] { entry.Owner + "." + entry.Const };
        }
    }

    // GW-U-109 (S-E002-005 edit, GW-U-561; S-E004-003 adds ZDO.SetParametersNameString; S-E006-002 adds the 32 version-handshake rows, GW-C-1260)
    // Edited by S-E003-006, ADR-GWIN-132 (GW-U-1653): the list grows to 330 rows with the building rows, and every new row resolves in the installed DLL.
    // Edited by integration (S-E004-004): plus the nine Category button rows (GW-U-1330), 349.
    // Edited by integration (S-E004-011): plus the 29 Organize run rows less the 8 the craft stories already declare, 370.
    // Edited by S-E004-012 (ADR-GWIN-136): plus the 15 station-actions rows, 385, renamed from 370.
    [RequiresValheimFact]
    public void TheClassifiedListHas404RowsSoTheTheoryCannotBeVacuous()
    {
        // Edited by S-E003-005, ADR-GWIN-127: renamed from 272 (the 11 requirement-display rows of contract 8.2 join).
        // Edited by S-E004-015 (ADR-GWIN-143): renamed from 385, plus the seven Deposit rows of deposit-button contract 7 = 392.
        // Edited by S-E010-002 (organize-console-command 6.2): plus the twelve console rows (Terminal, ConsoleCommand, ConsoleEvent, ConsoleEventArgs), 404.
        Assert.Equal(417, ClassifiedEntries().Count());
    }

    // GW-I-1900 (AC-5, deposit-button contract 7): the seven Deposit rows resolve in the installed assembly_valheim.dll, with no BST2010-BST2014 line.
    // Added by S-E004-015 (ADR-GWIN-143).
    [RequiresValheimTheory]
    [InlineData("ItemData.FieldEquipped")]
    [InlineData("ItemData.FieldEquippedType")]
    [InlineData("ItemData.FieldGridPos")]
    [InlineData("ItemData.FieldGridPosType")]
    [InlineData("SharedData.FieldItemType")]
    [InlineData("SharedData.FieldItemTypeType")]
    [InlineData("Humanoid.IsItemEquiped")]
    public void TheDepositRowsResolveInTheInstalledDll(string target)
    {
        IReadOnlyList<TargetEntry> entries = TargetList.Classify(typeof(PatchTargetNames));
        TargetEntry entry = entries.Single(e => e.Owner + "." + e.Const == target);

        Assert.Null(GameTargetChecker.Check(entry, entries, FindInGame, BuildValues.GameDll));
    }

    // GW-U-109
    [RequiresValheimTheory]
    [MemberData(nameof(ClassifiedEntries))]
    public void EveryClassifiedEntryResolvesInTheInstalledDll(string target)
    {
        IReadOnlyList<TargetEntry> entries = TargetList.Classify(typeof(PatchTargetNames));
        TargetEntry entry = entries.Single(e => e.Owner + "." + e.Const == target);

        Assert.Null(GameTargetChecker.Check(entry, entries, FindInGame, BuildValues.GameDll));
    }

    // GW-U-1154 (S-E003-004, contract 8.2): every new crafting row resolves in the installed DLL with no BST2010-BST2014 line.
    [RequiresValheimFact]
    public void TheCraftingRowsResolveInTheInstalledDll()
    {
        IReadOnlyList<TargetEntry> entries = TargetList.Classify(typeof(PatchTargetNames));
        Assert.Null(CheckEntry(entries, "Humanoid", "GetInventory"));
        Assert.Null(CheckEntry(entries, "Inventory", "CanAddItemParametersItemStack"));
        Assert.Null(CheckEntry(entries, "Inventory", "CanAddItemParametersPrefabStack"));
        Assert.Null(CheckEntry(entries, "DLCMan", "GetInstance"));
        Assert.Null(CheckEntry(entries, "ZoneSystem", "GetInstance"));
    }

    // GW-U-110
    [RequiresValheimFact]
    public void InstalledBuildMatchesTheFixtureTable()
    {
        Assembly game = fixture.Game;
        Type player = game.GetType("Player", throwOnError: true)!;
        Type container = game.GetType("Container", throwOnError: true)!;
        Type station = game.GetType("CraftingStation", throwOnError: true)!;

        Assert.Equal("Player", player.FullName);
        Assert.Equal("Humanoid", player.BaseType?.FullName);
        Assert.Equal("Container", container.FullName);
        Assert.Equal("CraftingStation", station.FullName);

        Assert.Equal(new[] { "RPC_RequestOpen(System.Int64,System.Int64)" }, Signatures(container, "RPC_RequestOpen"));
        Assert.Equal(new[] { "RPC_OpenResponse(System.Int64,System.Boolean)" }, Signatures(container, "RPC_OpenResponse"));
        Assert.Equal(new[] { "RPC_RequestStack(System.Int64,System.Int64)" }, Signatures(container, "RPC_RequestStack"));
        Assert.Equal(new[] { "RPC_StackResponse(System.Int64,System.Boolean)" }, Signatures(container, "RPC_StackResponse"));
        Assert.Empty(Signatures(container, "RPC_OpenRespons"));

        Assert.Equal("System.Int32", container.GetField("m_width", AllDeclared)?.FieldType.FullName);
        Assert.Equal("System.Int32", container.GetField("m_height", AllDeclared)?.FieldType.FullName);
        Assert.Equal("System.Single", station.GetField("m_rangeBuild", AllDeclared)?.FieldType.FullName);
    }

    // GW-U-111
    [RequiresValheimFact]
    public void NegativeFixturesFailWithTheExactMessages()
    {
        IReadOnlyList<TargetEntry> entries = TargetList.Classify(typeof(FixtureLists.V1Negative));
        string dll = BuildValues.GameDll;

        Assert.Equal(
            "BST2011: Patch target method 'Container.RPC_OpenRespons' (PatchTargetNames.Container.RpcOpenRespons) has 0 declared methods with that name in '" + dll + "'; expected exactly 1.",
            CheckEntry(entries, "Container", "RpcOpenRespons"));

        Assert.Equal(
            "BST2012: Patch target overload 'Player.HaveRequirements(Piece+Requirement[],System.Boolean,System.Int32,System.Int32)' (PatchTargetNames.Player.HaveRequirementsParametersPieceRequirements) not found in '" + dll + "'. Declared overloads: HaveRequirements(Piece,Player+RequirementMode); HaveRequirements(Recipe,System.Boolean,System.Int32,System.Int32).",
            CheckEntry(entries, "Player", "HaveRequirementsParametersPieceRequirements"));

        Assert.Equal(
            "BST2012: Patch target overload 'Player.HaveRequirements(Piece,Player.RequirementMode)' (PatchTargetNames.Player.HaveRequirementsParametersPiece) not found in '" + dll + "'. Declared overloads: HaveRequirements(Piece,Player+RequirementMode); HaveRequirements(Recipe,System.Boolean,System.Int32,System.Int32).",
            CheckEntry(entries, "Player", "HaveRequirementsParametersPiece"));
    }

    // GW-U-112
    [RequiresValheimFact]
    public void GameFolderIsUntouchedAndNoGameCodeRuns()
    {
        string managed = BuildValues.ManagedDir;
        string before = Snapshot(managed);

        IReadOnlyList<TargetEntry> entries = TargetList.Classify(typeof(PatchTargetNames));
        foreach (TargetEntry entry in entries)
        {
            Assert.Null(GameTargetChecker.Check(entry, entries, FindInGame, BuildValues.GameDll));
        }

        Assert.Equal(before, Snapshot(managed));
        Assert.DoesNotContain(
            AssemblyLoadContext.All.SelectMany(context => context.Assemblies),
            assembly => assembly.GetName().Name == "assembly_valheim");
        Assert.NotEqual("System.Reflection.RuntimeAssembly", fixture.Game.GetType().FullName);
    }

    private static string[] Signatures(Type type, string name)
    {
        return type.GetMethods(AllDeclared)
            .Where(m => m.Name == name)
            .Select(m => m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.FullName)) + ")")
            .ToArray();
    }

    private static string Snapshot(string managed)
    {
        string listing = string.Join(
            "\n",
            Directory.GetFiles(managed)
                .OrderBy(file => file, StringComparer.Ordinal)
                .Select(file => file + "|" + new FileInfo(file).Length + "|" + File.GetLastWriteTimeUtc(file).Ticks));
        string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(managed, "assembly_valheim.dll"))));
        return listing + "\n" + hash;
    }

    private Type? FindInGame(string name) => new GameTypeResolver(fixture).Resolve(name);

    private string? CheckEntry(IReadOnlyList<TargetEntry> entries, string owner, string constName)
    {
        TargetEntry entry = entries.Single(e => e.Owner == owner && e.Const == constName);
        return GameTargetChecker.Check(entry, entries, FindInGame, BuildValues.GameDll);
    }
}
