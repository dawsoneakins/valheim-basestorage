namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.PatchTargets.Tests.Checking;
using BaseStorage.PatchTargets.Tests.Fixtures;

/// <summary>
/// AC-3: the Harmony reader (contract 4.7.2) on the offline scenarios in Fixtures/Patches. The reader reads this test
/// assembly's own file, through PEReader, and never loads it. Unconditional: no game install.
/// </summary>
public sealed class HarmonyReaderTests
{
    private const string Patches = "BaseStorage.PatchTargets.Tests.Fixtures.Patches.";
    private const string Game = "BaseStorage.PatchTargets.Tests.Fixtures.Game.";
    private const string RecipeParams = Game + "Recipe,System.Boolean,System.Int32,System.Int32";
    private const string RecipeId = Game + "Player.HaveRequirements(" + RecipeParams + ")";

    private static readonly string TestDll = typeof(HarmonyReaderTests).Assembly.Location;

    // GW-U-098
    [Fact]
    public void ClassLevelAttributeGivesOneTarget()
    {
        Assert.Equal(new[] { RecipeId }, IdsOf("ClassLevel", new FixtureResolver()));
    }

    // GW-U-098
    [Fact]
    public void ClassAndMethodAttributesMergeIntoOneTarget()
    {
        Assert.Equal(new[] { RecipeId }, IdsOf("ClassPlusMethod", new FixtureResolver()));
    }

    // GW-U-098
    [Fact]
    public void TwoMethodsWithTheirOwnAttributesGiveTwoTargets()
    {
        Assert.Equal(
            new[]
            {
                Game + "Container.RPC_RequestOpen(System.Int64,System.Int64)",
                Game + "Container.RPC_StackResponse(System.Int64,System.Boolean)",
            },
            IdsOf("TwoMethods", new FixtureResolver()));
    }

    // GW-U-098
    [Fact]
    public void ALaterNonNullValueOverridesAnEarlierOne()
    {
        Assert.Equal(new[] { RecipeId }, IdsOf("OverrideLater", new FixtureResolver()));
    }

    // GW-U-098
    [Fact]
    public void ArrayShapeAndTypeAndStringShapeAreMerged()
    {
        Assert.Equal(new[] { RecipeId }, IdsOf("ArrayAndString", new FixtureResolver()));
    }

    // GW-U-098
    [Fact]
    public void NoArgumentTypesWithOneDeclaredOverloadUsesTheImplicitParameterText()
    {
        Assert.Equal(
            new[]
            {
                Game + "Container.RPC_OpenResponse(System.Int64,System.Boolean)",
                Game + "Container.RPC_RequestOpen(System.Int64,System.Int64)",
            },
            IdsOf("NoArgs", new FixtureResolver()));
    }

    // GW-U-098
    [Fact]
    public void PrefixAndPostfixClassesOnOneMethodCollapseToOneTarget()
    {
        string[] ids = IdsOf("PrefixPostfix", new FixtureResolver());

        Assert.Equal(new[] { RecipeId }, ids);
    }

    // GW-U-098
    [Fact]
    public void AbstractClassesAndInheritedAttributesAreIgnoredButStaticClassesAreKept()
    {
        IReadOnlyList<PatchClass> classes = HarmonyAttributeReader.Read(TestDll, Patches + "Classification", new FixtureResolver());

        Assert.Equal(new[] { Patches + "Classification.StaticPatch" }, classes.Select(c => c.ClassFullName));
    }

    // GW-U-098
    [Fact]
    public void NestedTypesAndArraysRenderAsFullNames()
    {
        string[] expected =
        [
            Game + "Container.RPC_RequestOpen(System.Int64[],System.Int64)",
            Game + "Player.HaveRequirements(" + Game + "Piece," + Game + "Player+RequirementMode)",
        ];

        Assert.Equal(expected, IdsOf("Nested", new FixtureResolver()));
    }

    // GW-U-099
    [Theory]
    [InlineData("MethodTypeCtor", "unsupported HarmonyPatch constructor (System.Type, HarmonyLib.MethodType)")]
    [InlineData("TargetMethodDeclared", "TargetMethod or TargetMethods is declared")]
    [InlineData("TargetMethodAttributeDeclared", "TargetMethod or TargetMethods is declared")]
    [InlineData("NoDeclaringType", "declaring type is not set")]
    [InlineData("NoMethodName", "method name is not set")]
    [InlineData("OverloadedWithoutArgs", "method 'BaseStorage.PatchTargets.Tests.Fixtures.Game.Player.HaveRequirements' has 2 declared overloads and no argument types are given")]
    public void EachUnsupportedShapeGivesItsFixedReason(string className, string reason)
    {
        IReadOnlyList<PatchClass> classes = HarmonyAttributeReader.Read(TestDll, Patches + "Unsupported", new FixtureResolver());
        PatchClass patch = classes.Single(c => c.ClassFullName == Patches + "Unsupported." + className);

        Assert.Equal(
            new[] { "BST2022: Plugin patch class '" + Patches + "Unsupported." + className + "' has a Harmony target this check cannot resolve: " + reason + "." },
            patch.Diagnostics);
    }

    // GW-U-099
    [Fact]
    public void DeclaringTypeOutsideTheGameIsReported()
    {
        PatchClass patch = HarmonyAttributeReader.Read(TestDll, Patches + "OutsideGame", new FixtureResolver(gameTypes: false)).Single();

        Assert.Equal(
            new[] { "BST2022: Plugin patch class '" + Patches + "OutsideGame.DeclaringTypeNotGame' has a Harmony target this check cannot resolve: declaring type '" + Game + "Player' is not in assembly_valheim." },
            patch.Diagnostics);
    }

    private static string[] IdsOf(string scenario, ITypeNameResolver resolver)
    {
        return HarmonyAttributeReader.Read(TestDll, Patches + scenario, resolver)
            .SelectMany(c => c.TargetIds)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }
}
