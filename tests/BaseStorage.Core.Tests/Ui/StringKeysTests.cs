namespace BaseStorage.Core.Tests.Ui;

using System;
using System.Linq;
using System.Reflection;

/// <summary>S-FOUND-001 AC-1: StringKeys holds one constant per contract 3.2 key family (contract 3.2 "Const" column).</summary>
public sealed class StringKeysTests
{
    // GW-U-050
    [Fact]
    public void StringKeysIsAPublicStaticClass()
    {
        Type keys = UiContract.Find("StringKeys");

        Assert.True(keys.IsPublic, "StringKeys must be public.");
        Assert.True(keys.IsAbstract && keys.IsSealed, "StringKeys must be a static class.");
    }

    // GW-U-050
    [Fact]
    public void StringKeysHoldsExactlyTheContractConstants()
    {
        Type keys = UiContract.Find("StringKeys");
        FieldInfo[] publicFields = keys.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        Assert.Equal(UiFixtures.StringKeyValues.Length, publicFields.Length);
        Assert.Equal(UiFixtures.StringKeyCount, publicFields.Length);
        Assert.All(publicFields, field => Assert.True(field.IsLiteral && field.FieldType == typeof(string), $"{field.Name} must be a public const string."));
    }

    // GW-U-050
    [Fact]
    public void EachConstantHoldsTheContractValue()
    {
        var constants = UiContract.StringKeyConstants();

        Assert.Equal(
            UiFixtures.StringKeyValues.Select(pair => pair.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray(),
            constants.Keys.OrderBy(n => n, StringComparer.Ordinal).ToArray());

        foreach ((string name, string value) in UiFixtures.StringKeyValues)
        {
            Assert.True(constants.TryGetValue(name, out string? actual), $"StringKeys.{name} is missing.");
            Assert.Equal(value, actual);
        }
    }

    // GW-U-050
    [Fact]
    public void EveryConstantIsAKeyOrAPluralFamilyBase()
    {
        string[] keys = UiContract.Keys();

        foreach (string value in UiContract.StringKeyConstants().Values)
        {
            bool isKey = UiContract.Contains(value);
            bool isFamily = keys.Any(k => k.StartsWith(value + ".", StringComparison.Ordinal));
            Assert.True(isKey || isFamily, $"{value} is neither a key nor a plural family base.");
        }
    }

    // GW-U-050
    [Fact]
    public void PluralFamilyBaseConstantHoldsTheBaseKeyName()
    {
        Assert.Equal("hover.chest.in-base", UiContract.StringKeyConstants()["HoverChestInBase"]);
    }
}
