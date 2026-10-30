namespace BaseStorage.Core.Tests.Ui;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>
/// Reflection binding to BaseStorage.Core.Ui (ADR-GWIN-011). Every type is resolved through
/// <see cref="ContractTypes.Require"/>, so a missing type fails the calling test with
/// "Contract type ... does not exist in BaseStorage.Core yet.". Calls rethrow inner exceptions unwrapped,
/// so Assert.Throws sees the real exception and its ParamName. This is a helper, not a test class.
/// </summary>
public static class UiContract
{
    /// <summary>Namespace of the Core strings table, plural helper, single-line helper and hover formatter.</summary>
    public const string Namespace = "BaseStorage.Core.Ui";

    private const BindingFlags StaticPublic = BindingFlags.Public | BindingFlags.Static;
    private const BindingFlags InstancePublic = BindingFlags.Public | BindingFlags.Instance;

    /// <summary>Resolves <c>BaseStorage.Core.Ui.&lt;simpleName&gt;</c>, or fails the calling test with the missing name.</summary>
    public static Type Find(string simpleName) => ContractTypes.Require(Namespace + "." + simpleName);

    /// <summary>The <c>StringTable.English</c> instance. Every call returns the same instance (ADR-GWIN-009).</summary>
    public static object English()
    {
        Type table = Find("StringTable");
        PropertyInfo? property = table.GetProperty("English", StaticPublic);
        Assert.True(property is not null, "StringTable has no public static English property.");
        MethodInfo? getter = property!.GetGetMethod();
        Assert.True(getter is not null, "StringTable.English has no public getter.");
        return ContractTypes.Invoke(getter!, null, Array.Empty<object?>())!;
    }

    /// <summary>StringTable.Keys as the raw object, so a test can inspect its runtime collection type.</summary>
    public static object KeysObject()
    {
        Type table = Find("StringTable");
        PropertyInfo? property = table.GetProperty("Keys", InstancePublic);
        Assert.True(property is not null, "StringTable has no public Keys property.");
        MethodInfo? getter = property!.GetGetMethod();
        Assert.True(getter is not null, "StringTable.Keys has no public getter.");
        return ContractTypes.Invoke(getter!, English(), Array.Empty<object?>())!;
    }

    /// <summary>StringTable.Keys copied into an array, in the order Core returns them.</summary>
    public static string[] Keys() => ((IEnumerable<string>)KeysObject()).ToArray();

    /// <summary>StringTable.Contains(key). Passing null exercises the R11 path.</summary>
    public static bool Contains(string? key) =>
        (bool)CallTable("Contains", new[] { typeof(string) }, new object?[] { key })!;

    /// <summary>StringTable.Get(key), the raw template or the key itself when missing.</summary>
    public static string Get(string? key) =>
        (string)CallTable("Get", new[] { typeof(string) }, new object?[] { key })!;

    /// <summary>StringTable.Format(key, args). A null params array is passed through as null (S6).</summary>
    public static string Format(string? key, params object?[]? args) =>
        (string)CallTable("Format", new[] { typeof(string), typeof(object[]) }, new object?[] { key, args })!;

    /// <summary>StringTable.FormatPlural(key, count, args).</summary>
    public static string FormatPlural(string? key, int count, params object?[]? args) =>
        (string)CallTable(
            "FormatPlural",
            new[] { typeof(string), typeof(int), typeof(object[]) },
            new object?[] { key, count, args })!;

    /// <summary>Plural.Form(count).</summary>
    public static string PluralForm(int count) =>
        (string)StaticCall("Plural", "Form", new[] { typeof(int) }, new object?[] { count })!;

    /// <summary>The Plural.One constant.</summary>
    public static string PluralOne() => ConstantString("Plural", "One");

    /// <summary>The Plural.Other constant.</summary>
    public static string PluralOther() => ConstantString("Plural", "Other");

    /// <summary>SingleLine.Of(text). Passing null exercises the R11 path.</summary>
    public static string SingleLineOf(string? text) =>
        (string)StaticCall("SingleLine", "Of", new[] { typeof(string) }, new object?[] { text })!;

    /// <summary>HoverLineFormatter.Chest(stationCount, categoryLabel, cycleKeyLabel).</summary>
    public static string Chest(int stationCount, string? categoryLabel, string? cycleKeyLabel) =>
        (string)StaticCall(
            "HoverLineFormatter",
            "Chest",
            new[] { typeof(int), typeof(string), typeof(string) },
            new object?[] { stationCount, categoryLabel, cycleKeyLabel })!;

    /// <summary>HoverLineFormatter.Station(chestCount).</summary>
    public static string Station(int chestCount) =>
        (string)StaticCall("HoverLineFormatter", "Station", new[] { typeof(int) }, new object?[] { chestCount })!;

    /// <summary>Every public const string field of StringKeys, by constant name.</summary>
    public static IReadOnlyDictionary<string, string> StringKeyConstants()
    {
        var constants = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (FieldInfo field in Find("StringKeys").GetFields(StaticPublic))
        {
            if (field.IsLiteral && field.FieldType == typeof(string))
            {
                constants[field.Name] = (string)field.GetValue(null)!;
            }
        }

        return constants;
    }

    private static object? CallTable(string name, Type[] parameterTypes, object?[] args)
    {
        MethodInfo method = ContractTypes.Method(Find("StringTable"), name, InstancePublic, parameterTypes);
        return ContractTypes.Invoke(method, English(), args);
    }

    private static object? StaticCall(string typeName, string name, Type[] parameterTypes, object?[] args)
    {
        MethodInfo method = ContractTypes.Method(Find(typeName), name, StaticPublic, parameterTypes);
        return ContractTypes.Invoke(method, null, args);
    }

    private static string ConstantString(string typeName, string field)
    {
        FieldInfo? info = Find(typeName).GetField(field, StaticPublic);
        Assert.True(info is not null, $"{typeName}.{field} does not exist in BaseStorage.Core yet.");
        return (string)info!.GetValue(null)!;
    }
}
