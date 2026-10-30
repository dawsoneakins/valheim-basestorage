namespace BaseStorage.Core.Tests.Ui;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>S-FOUND-001 AC-1: the table's keys, immutability and public surface (contract 2.2 S9, U1, U5).</summary>
public sealed class StringTableSurfaceTests
{
    private const BindingFlags AllDeclared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private static readonly Regex KeyPattern = new(
        @"^[a-z0-9]+(-[a-z0-9]+)*(\.[a-z0-9]+(-[a-z0-9]+)*)+$",
        RegexOptions.CultureInvariant);

    // GW-U-048
    [Fact]
    public void KeysContainsEveryFixtureKeyExactlyOnce()
    {
        string[] keys = UiContract.Keys();

        foreach ((string key, string _) in UiFixtures.EnglishTable)
        {
            Assert.Contains(key, keys);
        }

        Assert.Equal(keys.Length, keys.Distinct(StringComparer.Ordinal).Count());
    }

    // GW-U-048
    [Fact]
    public void KeysAreOrdinalSortedAndMatchTheKeyGrammar()
    {
        string[] keys = UiContract.Keys();

        Assert.Equal(keys.OrderBy(k => k, StringComparer.Ordinal).ToArray(), keys);
        Assert.All(keys, key => Assert.Matches(KeyPattern, key));
    }

    // GW-U-048
    [Fact]
    public void KeysCollectionIsReadOnlyAndRejectsAdd()
    {
        object keys = UiContract.KeysObject();
        var collection = Assert.IsAssignableFrom<System.Collections.Generic.ICollection<string>>(keys);

        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Add("no.such.key"));
    }

    // GW-U-048
    [Fact]
    public void StringTableIsSealedWithNoPublicConstructor()
    {
        Type table = UiContract.Find("StringTable");

        Assert.True(table.IsSealed, "StringTable must be sealed.");
        Assert.Empty(table.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
    }

    // GW-U-048
    [Fact]
    public void StringTableHasNoPublicSetterOrMutator()
    {
        Type table = UiContract.Find("StringTable");

        string[] settable = table
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(p => p.GetSetMethod(nonPublic: false) is not null)
            .Select(p => p.Name)
            .ToArray();
        string[] mutators = table
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "Add" || m.Name == "Register" || m.Name.StartsWith("Set", StringComparison.Ordinal))
            .Select(m => m.Name)
            .ToArray();

        Assert.Empty(settable);
        Assert.Empty(mutators);
    }

    // GW-U-048
    [Fact]
    public void EveryStaticFieldInTheUiNamespaceIsReadOnlyOrConstant()
    {
        UiContract.Find("StringTable");

        var violations = new List<string>();
        foreach (Type type in ContractTypes.CoreAssembly().GetTypes()
                     .Where(t => t.Namespace == UiContract.Namespace && !t.Name.Contains('<', StringComparison.Ordinal)))
        {
            foreach (FieldInfo field in type.GetFields(AllDeclared).Where(f => f.IsStatic))
            {
                if (!field.IsInitOnly && !field.IsLiteral)
                {
                    violations.Add(type.FullName + "." + field.Name);
                }
            }
        }

        Assert.Empty(violations);
    }

    // GW-U-048
    [Fact]
    public void EnglishIsGetOnlyAndReturnsTheSameInstance()
    {
        Type table = UiContract.Find("StringTable");
        PropertyInfo english = table.GetProperty("English", BindingFlags.Public | BindingFlags.Static)!;

        Assert.False(english.CanWrite);
        Assert.Same(UiContract.English(), UiContract.English());
    }

    // GW-U-048
    [Fact]
    public void PluralBaseKeysWithoutTextAreNotContained()
    {
        foreach (string baseKey in UiFixtures.PluralBaseKeysWithoutText)
        {
            Assert.False(UiContract.Contains(baseKey), $"Plural base {baseKey} must have no text of its own.");
        }
    }
}
