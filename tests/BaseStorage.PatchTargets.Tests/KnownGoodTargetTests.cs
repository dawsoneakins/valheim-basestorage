namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.PatchTargets.Tests.Checking;
using BaseStorage.PatchTargets.Tests.Infrastructure;
using BaseStorage.Shared;

/// <summary>
/// AC-5 (as corrected in core-contracts.md 4.3.2): the first known-good targets are the two HaveRequirements overloads,
/// recipe first (contract 4.6.3 rows 1 and 2). DLL-backed.
/// </summary>
[Collection("ValheimAssembly")]
public sealed class KnownGoodTargetTests
{
    private readonly ValheimAssemblyFixture fixture;

    public KnownGoodTargetTests(ValheimAssemblyFixture fixture)
    {
        this.fixture = fixture;
    }

    // GW-U-108
    [RequiresValheimTheory]
    [InlineData("HaveRequirementsParametersRecipe", "Recipe,System.Boolean,System.Int32,System.Int32")]
    [InlineData("HaveRequirementsParametersPiece", "Piece,Player+RequirementMode")]
    public void FirstKnownGoodTargetsResolveRecipeFirst(string companionConst, string parameterText)
    {
        IReadOnlyList<TargetEntry> entries = TargetList.Classify(typeof(PatchTargetNames));
        TargetEntry companion = entries.Single(e => e.Owner == "Player" && e.Const == companionConst);
        Assert.Null(GameTargetChecker.Check(companion, entries, FindInGame, BuildValues.GameDll));

        Type player = fixture.Game.GetType("Player", throwOnError: true)!;
        MethodInfo[] matches = player
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "HaveRequirements"
                && string.Join(",", m.GetParameters().Select(p => p.ParameterType.FullName)) == parameterText)
            .ToArray();

        Assert.Single(matches);
        Assert.Equal("System.Boolean", matches[0].ReturnType.FullName);
        Assert.False(matches[0].IsStatic, "HaveRequirements must be an instance method.");

        Type mode = fixture.Game.GetType("Player+RequirementMode", throwOnError: true)!;
        Assert.True(mode.IsEnum, "Player+RequirementMode must be an enum.");
        Assert.Equal("Player", mode.DeclaringType?.FullName);
    }

    private Type? FindInGame(string name) => new GameTypeResolver(fixture).Resolve(name);
}
