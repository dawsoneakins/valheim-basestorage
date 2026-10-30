namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.PatchTargets.Tests.Checking;
using BaseStorage.PatchTargets.Tests.Fixtures;

/// <summary>
/// AC-2: the checker logic (contract 4.6.2) on the offline fixture game types. The DLL is a fake path, and the
/// lookup is the fixture assembly. Unconditional: no game install.
/// </summary>
public sealed class CheckerTests
{
    private const string DllPath = @"X:\fixture.dll";

    private static readonly Func<string, Type?> FindType = name => typeof(CheckerTests).Assembly.GetType(name, false, false);

    // GW-U-096
    [Fact]
    public void ValidFixtureListReturnsNullForEveryEntry()
    {
        IReadOnlyList<TargetEntry> entries = TargetList.Classify(typeof(FixtureLists.Valid));

        Assert.NotEmpty(entries);
        Assert.All(entries, entry => Assert.Null(GameTargetChecker.Check(entry, entries, FindType, DllPath)));
    }

    // GW-U-096
    [Fact]
    public void MisspeltTypeNameFailsWithBst2010AndEveryMemberRowFailsToo()
    {
        const string expected = @"BST2010: Patch target type 'BaseStorage.PatchTargets.Tests.Fixtures.Game.Containr' (PatchTargetNames.BadType.TypeName) not found in 'X:\fixture.dll'.";

        Assert.Equal(expected, CheckOne(typeof(FixtureLists.CheckerCases), "BadType", "TypeName"));
        Assert.Equal(expected, CheckOne(typeof(FixtureLists.CheckerCases), "BadType", "RpcRequestOpen"));
    }

    // GW-U-096
    [Fact]
    public void NameOnlyMethodWithNoDeclaredMethodFailsWithBst2011Zero()
    {
        Assert.Equal(
            @"BST2011: Patch target method 'BaseStorage.PatchTargets.Tests.Fixtures.Game.Container.RPC_Missing' (PatchTargetNames.BadName.RpcMissing) has 0 declared methods with that name in 'X:\fixture.dll'; expected exactly 1.",
            CheckOne(typeof(FixtureLists.CheckerCases), "BadName", "RpcMissing"));
    }

    // GW-U-096
    [Fact]
    public void NameOnlyMethodWithTwoOverloadsFailsWithBst2011Two()
    {
        Assert.Equal(
            @"BST2011: Patch target method 'BaseStorage.PatchTargets.Tests.Fixtures.Game.Player.HaveRequirements' (PatchTargetNames.Overloaded.HaveRequirements) has 2 declared methods with that name in 'X:\fixture.dll'; expected exactly 1.",
            CheckOne(typeof(FixtureLists.CheckerCases), "Overloaded", "HaveRequirements"));
    }

    // GW-U-096
    [Fact]
    public void OverloadCompanionFailsWithBst2012ListingTheDeclaredOverloads()
    {
        Assert.Equal(
            @"BST2012: Patch target overload 'BaseStorage.PatchTargets.Tests.Fixtures.Game.Player.HaveRequirements(BaseStorage.PatchTargets.Tests.Fixtures.Game.Piece,BaseStorage.PatchTargets.Tests.Fixtures.Game.Player.RequirementMode)' (PatchTargetNames.BadOverload.HaveRequirementsParametersPiece) not found in 'X:\fixture.dll'. Declared overloads: HaveRequirements(BaseStorage.PatchTargets.Tests.Fixtures.Game.Piece,BaseStorage.PatchTargets.Tests.Fixtures.Game.Player+RequirementMode); HaveRequirements(BaseStorage.PatchTargets.Tests.Fixtures.Game.Recipe,System.Boolean,System.Int32,System.Int32).",
            CheckOne(typeof(FixtureLists.CheckerCases), "BadOverload", "HaveRequirementsParametersPiece"));
    }

    // GW-U-096
    [Fact]
    public void OverloadModeMethodPassesOnlyWhenEveryCompanionPasses()
    {
        string? method = CheckOne(typeof(FixtureLists.CheckerCases), "BadOverload", "HaveRequirements");

        Assert.Equal(
            CheckOne(typeof(FixtureLists.CheckerCases), "BadOverload", "HaveRequirementsParametersPiece"),
            method);
        Assert.NotNull(method);
    }

    // GW-U-096
    [Fact]
    public void OverloadWithNoDeclaredMethodListsNone()
    {
        Assert.Equal(
            @"BST2012: Patch target overload 'BaseStorage.PatchTargets.Tests.Fixtures.Game.Container.RPC_None(System.Int64,System.Int64)' (PatchTargetNames.NoMethod.RpcNoneParametersX) not found in 'X:\fixture.dll'. Declared overloads: (none).",
            CheckOne(typeof(FixtureLists.CheckerCases), "NoMethod", "RpcNone"));
    }

    // GW-U-096
    [Fact]
    public void MissingFieldFailsWithBst2013()
    {
        Assert.Equal(
            @"BST2013: Patch target field 'BaseStorage.PatchTargets.Tests.Fixtures.Game.Container.m_widht' (PatchTargetNames.BadField.FieldWidth) not found in 'X:\fixture.dll'.",
            CheckOne(typeof(FixtureLists.CheckerCases), "BadField", "FieldWidth"));
    }

    // GW-U-096
    [Fact]
    public void FieldWithTheWrongTypeFailsWithBst2014()
    {
        Assert.Equal(
            @"BST2014: Patch target field 'BaseStorage.PatchTargets.Tests.Fixtures.Game.CraftingStation.m_rangeBuild' (PatchTargetNames.BadFieldType.FieldRangeBuild) has type 'System.Single' in 'X:\fixture.dll'; expected 'System.Int32'.",
            CheckOne(typeof(FixtureLists.CheckerCases), "BadFieldType", "FieldRangeBuildType"));
    }

    // GW-U-096
    [Fact]
    public void MethodDeclaredOnlyOnTheBaseClassFails()
    {
        Assert.Equal(
            @"BST2011: Patch target method 'BaseStorage.PatchTargets.Tests.Fixtures.Game.Player.InheritedHelper' (PatchTargetNames.DeclaredOnly.InheritedHelper) has 0 declared methods with that name in 'X:\fixture.dll'; expected exactly 1.",
            CheckOne(typeof(FixtureLists.CheckerCases), "DeclaredOnly", "InheritedHelper"));
    }

    // GW-U-096
    [Fact]
    public void ConstructorsAreNotCountedForNameOnlyLookup()
    {
        Assert.Equal(
            @"BST2011: Patch target method 'BaseStorage.PatchTargets.Tests.Fixtures.Game.Player..ctor' (PatchTargetNames.CtorExcluded.Ctor) has 0 declared methods with that name in 'X:\fixture.dll'; expected exactly 1.",
            CheckOne(typeof(FixtureLists.CheckerCases), "CtorExcluded", "Ctor"));
    }

    // GW-U-096
    [Fact]
    public void StaticAndNonPublicMembersAreFound()
    {
        Assert.Null(CheckOne(typeof(FixtureLists.CheckerCases), "Hidden", "HiddenHelper"));
    }

    private static string? CheckOne(Type list, string owner, string constName)
    {
        IReadOnlyList<TargetEntry> entries = TargetList.Classify(list);
        TargetEntry entry = entries.Single(e => e.Owner == owner && e.Const == constName);
        return GameTargetChecker.Check(entry, entries, FindType, DllPath);
    }
}
