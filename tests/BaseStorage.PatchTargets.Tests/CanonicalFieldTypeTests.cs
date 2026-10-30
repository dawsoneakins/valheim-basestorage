namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Collections.Generic;
using System.Reflection;
using BaseStorage.PatchTargets.Tests.Checking;
using BaseStorage.PatchTargets.Tests.Fixtures.Game;

/// <summary>
/// AC-13 (contract 6.3, rule PT1): GameTargetChecker.Canonical renders a field type without assembly qualification, and a field
/// companion compares the canonical form. Runtime types only, so these rows need no ValheimDir. Canonical is read by reflection,
/// so the test project still compiles until implement adds it.
/// </summary>
public sealed class CanonicalFieldTypeTests
{
    private const string DictionaryCanonical = "System.Collections.Generic.Dictionary`2[System.String,System.String]";
    private const string DllPath = "fixture.dll";

    /// <summary>A nested non-generic type, whose canonical name keeps the + separator.</summary>
    public sealed class NestedMarker
    {
    }

    // GW-U-718
    [Fact]
    public void ANonGenericTypeKeepsItsFullName()
    {
        Assert.Equal("System.Int32", Canonical(typeof(int)));
    }

    // GW-U-718
    [Fact]
    public void ADictionaryRendersWithBracketsAndNoAssemblyQualification()
    {
        Assert.Equal(DictionaryCanonical, Canonical(typeof(Dictionary<string, string>)));
    }

    // GW-U-718
    [Fact]
    public void ANestedGenericArgumentRendersRecursively()
    {
        Assert.Equal(
            "System.Collections.Generic.List`1[System.Collections.Generic.KeyValuePair`2[System.String,System.Int32]]",
            Canonical(typeof(List<KeyValuePair<string, int>>)));
    }

    // GW-U-718
    [Fact]
    public void ANestedNonGenericTypeKeepsThePlusSeparator()
    {
        string name = Canonical(typeof(NestedMarker));

        Assert.Equal(typeof(NestedMarker).FullName, name);
        Assert.Contains("+", name, StringComparison.Ordinal);
    }

    // GW-U-718
    [Fact]
    public void AFieldCompanionWithTheCanonicalGenericNameIsAccepted()
    {
        IReadOnlyList<TargetEntry> entries = FieldEntries(DictionaryCanonical);

        Assert.Null(GameTargetChecker.Check(entries[2], entries, FindHolder, DllPath));
    }

    // GW-U-718
    [Fact]
    public void AWrongFieldCompanionFailsWithTheCanonicalTypeInTheMessage()
    {
        IReadOnlyList<TargetEntry> entries = FieldEntries("System.String");

        string? message = GameTargetChecker.Check(entries[2], entries, FindHolder, DllPath);

        Assert.Equal(
            "BST2014: Patch target field '" + typeof(ProfileHolder).FullName + ".m_customData' (PatchTargetNames.Holder.FieldCustomData)"
            + " has type '" + DictionaryCanonical + "' in '" + DllPath + "'; expected 'System.String'.",
            message);
    }

    private static Type? FindHolder(string name) => name == typeof(ProfileHolder).FullName ? typeof(ProfileHolder) : null;

    private static IReadOnlyList<TargetEntry> FieldEntries(string companionValue)
    {
        return new[]
        {
            new TargetEntry("Holder", "TypeName", TargetKind.TypeEntry, typeof(ProfileHolder).FullName!, null),
            new TargetEntry("Holder", "FieldCustomData", TargetKind.FieldEntry, "m_customData", null),
            new TargetEntry("Holder", "FieldCustomDataType", TargetKind.FieldTypeCompanion, companionValue, "FieldCustomData"),
        };
    }

    /// <summary>Calls GameTargetChecker.Canonical by reflection. A missing method fails the row with its own message.</summary>
    private static string Canonical(Type type)
    {
        MethodInfo? method = typeof(GameTargetChecker).GetMethod(
            "Canonical",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(Type) },
            null);
        Assert.True(method is not null, "GameTargetChecker.Canonical(Type) does not exist yet (rule PT1, contract 6.3).");

        var canonical = (Func<Type, string>)Delegate.CreateDelegate(typeof(Func<Type, string>), method!);
        return canonical(type);
    }
}
