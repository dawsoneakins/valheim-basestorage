namespace BaseStorage.Core.Tests.Settings;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Settings;

/// <summary>
/// AC-3 and AC-7 (ADR-GWIN-015): reflection over the public surface of BaseStorage.Core.Settings. Only that namespace is
/// inspected. GW-U-031 keeps walking every exported type on its own.
/// </summary>
public sealed class SettingsSurfaceTests
{
    private const string SettingsNamespace = "BaseStorage.Core.Settings";
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly string[] ContractTypeNames =
    {
        "BaseStorage.Core.Settings.BaseStorageSettings",
        "BaseStorage.Core.Settings.ISettingsSource",
        "BaseStorage.Core.Settings.ItemListText",
        "BaseStorage.Core.Settings.ItemListTokens",
        "BaseStorage.Core.Settings.SettingDefinition",
        "BaseStorage.Core.Settings.SettingKeys",
        "BaseStorage.Core.Settings.SettingKind",
        "BaseStorage.Core.Settings.SettingSections",
        "BaseStorage.Core.Settings.SettingsCatalog",
    };

    // GW-U-100 (AC-3)
    [Fact]
    public void ExportedTypesAreExactlyTheNineContractTypes()
    {
        string[] exported = typeof(SettingKind).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == SettingsNamespace)
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ContractTypeNames, exported);
    }

    // GW-U-100 (AC-3)
    [Fact]
    public void SettingKeysHasExactly19PublicConstantsEqualToTheirNames()
    {
        FieldInfo[] fields = typeof(SettingKeys).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        Assert.Equal(20, fields.Length);
        foreach (FieldInfo field in fields)
        {
            Assert.True(field.IsPublic && field.IsLiteral && field.FieldType == typeof(string), field.Name + " is not a public string constant.");
            Assert.Equal(field.Name, field.GetRawConstantValue());
        }
    }

    // GW-U-100 (AC-3)
    [Fact]
    public void SettingSectionsHasExactly5PublicConstantsEqualToTheirNames()
    {
        FieldInfo[] fields = typeof(SettingSections).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        Assert.Equal(5, fields.Length);
        foreach (FieldInfo field in fields)
        {
            Assert.True(field.IsPublic && field.IsLiteral && field.FieldType == typeof(string), field.Name + " is not a public string constant.");
            Assert.Equal(field.Name, field.GetRawConstantValue());
        }
    }

    // GW-U-100 (AC-3)
    [Fact]
    public void ItemListTokensHoldsTheFourClassTokens()
    {
        Assert.Equal("@food", ItemListTokens.Food);
        Assert.Equal("@mead", ItemListTokens.Mead);
        Assert.Equal("@ammo", ItemListTokens.Ammo);
        Assert.Equal("@bait", ItemListTokens.Bait);
    }

    // GW-U-100 (AC-3)
    [Fact]
    public void SettingDefinitionIsSealedWithNoPublicConstructorAndSixGetOnlyProperties()
    {
        Type type = typeof(SettingDefinition);

        Assert.True(type.IsSealed, "SettingDefinition is not sealed.");
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        PropertyInfo[] properties = type.GetProperties(PublicDeclared);
        string[] names = properties.Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            new[] { "DefaultText", "Description", "HostSynced", "Key", "Kind", "Section" },
            names);
        foreach (PropertyInfo property in properties)
        {
            Assert.Null(property.SetMethod);
        }
    }

    // GW-U-100 (AC-3)
    [Fact]
    public void CatalogAndHelperTypesAreStaticClasses()
    {
        foreach (Type type in new[] { typeof(SettingsCatalog), typeof(SettingSections), typeof(SettingKeys), typeof(ItemListText), typeof(ItemListTokens) })
        {
            Assert.True(type.IsAbstract && type.IsSealed, type.Name + " is not a static class.");
        }
    }

    // GW-U-100 (AC-3)
    [Fact]
    public void FindReturnsANullableSettingDefinition()
    {
        MethodInfo find = typeof(SettingsCatalog).GetMethod(nameof(SettingsCatalog.Find), new[] { typeof(string) })!;

        Assert.Equal(typeof(SettingDefinition), find.ReturnType);
        NullabilityInfo info = new NullabilityInfoContext().Create(find.ReturnParameter);
        Assert.Equal(NullabilityState.Nullable, info.ReadState);
    }

    // GW-U-100 (AC-3)
    [Fact]
    public void ISettingsSourceDeclaresExactlyTheThreeTryGetMethodsWithKeyAndOutValue()
    {
        Dictionary<string, Type> expected = new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["TryGetBool"] = typeof(bool),
            ["TryGetInt"] = typeof(int),
            ["TryGetText"] = typeof(string),
        };

        MethodInfo[] methods = typeof(ISettingsSource).GetMethods();
        Assert.Equal(expected.Keys.OrderBy(name => name, StringComparer.Ordinal), methods.Select(method => method.Name).OrderBy(name => name, StringComparer.Ordinal));

        foreach (MethodInfo method in methods)
        {
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
            Assert.Equal("key", parameters[0].Name);
            Assert.True(parameters[1].IsOut, method.Name + " value parameter is not out.");
            Assert.Equal(expected[method.Name].MakeByRefType(), parameters[1].ParameterType);
            Assert.Equal("value", parameters[1].Name);
            Assert.Equal(typeof(bool), method.ReturnType);
        }
    }

    // GW-U-108 (AC-7)
    [Fact]
    public void BaseStorageSettingsIsSealed()
    {
        Assert.True(typeof(BaseStorageSettings).IsSealed, "BaseStorageSettings is not sealed.");
    }

    // GW-U-108 (AC-7): exactly one get-only property per key, with the CLR type that follows the kind.
    [Fact]
    public void PublicPropertiesAreOneGetOnlyPerKeyWithTheKindClrType()
    {
        Type type = typeof(BaseStorageSettings);
        PropertyInfo[] properties = type.GetProperties(PublicDeclared);
        Assert.Equal(20, properties.Length);

        foreach (var row in Section7Table.Rows)
        {
            PropertyInfo? property = type.GetProperty(row.Key, PublicDeclared);
            Assert.NotNull(property);
            Assert.Null(property.SetMethod);
            Assert.Equal(ClrTypeFor(row.Kind), property.PropertyType);
        }
    }

    // GW-U-108 (AC-7): the public methods are exactly the four typed getters, each taking one key.
    [Fact]
    public void PublicMethodsAreExactlyTheFourGettersEachTakingAKey()
    {
        MethodInfo[] methods = typeof(BaseStorageSettings).GetMethods(PublicDeclared)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        Assert.Equal(
            new[] { "GetBool", "GetHotkeyText", "GetInt", "GetItemList" },
            methods.Select(method => method.Name).OrderBy(name => name, StringComparer.Ordinal));

        foreach (MethodInfo method in methods)
        {
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
            Assert.Equal("key", parameters[0].Name);
        }
    }

    private static Type ClrTypeFor(SettingKind kind) => kind switch
    {
        SettingKind.Bool => typeof(bool),
        SettingKind.Int => typeof(int),
        SettingKind.ItemList => typeof(IReadOnlyList<string>),
        SettingKind.Hotkey => typeof(string),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
