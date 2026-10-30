namespace BaseStorage.Core.Tests.HostSync;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.HostSync;
using BaseStorage.Core.Settings;

/// <summary>AC-6.2 (contract 1, 2.1, 3.1, 4.1, 5.1): the exported shape of BaseStorage.Core.HostSync, checked by reflection.</summary>
public sealed class HostSyncSurfaceTests
{
    private const string HostSyncNamespace = "BaseStorage.Core.HostSync";
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-1011 (contract 1): the exported types of the namespace are exactly the four contract names, ordinal-sorted.
    [Fact]
    public void ExportedTypesAreExactlyTheFourHostSyncNames()
    {
        string[] exported = typeof(HostSyncCodec).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == HostSyncNamespace)
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "BaseStorage.Core.HostSync.HostSyncCodec",
                "BaseStorage.Core.HostSync.HostSyncPayload",
                "BaseStorage.Core.HostSync.HostSyncValueText",
                "BaseStorage.Core.HostSync.LayeredSettingsSource",
            },
            exported);
    }

    // GW-U-1011 (contract 2.1): HostSyncPayload is sealed, IEquatable, has one public constructor and exactly the contracted members.
    [Fact]
    public void PayloadMembersAreExactlyTheContractList()
    {
        Type type = typeof(HostSyncPayload);

        Assert.True(type.IsSealed);
        Assert.True(typeof(IEquatable<HostSyncPayload>).IsAssignableFrom(type));
        ConstructorInfo ctor = Assert.Single(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Equal(new[] { "settings", "categoryFileText" }, ctor.GetParameters().Select(parameter => parameter.Name));
        Assert.Equal(typeof(IEnumerable<KeyValuePair<string, string>>), ctor.GetParameters()[0].ParameterType);
        Assert.Equal(
            Sorted(
                "M Capture(ISettingsSource, String) : HostSyncPayload",
                "M Equals(HostSyncPayload) : Boolean",
                "M Equals(Object) : Boolean",
                "M GetHashCode() : Int32",
                "M TryGetValueText(String, String&) : Boolean",
                "M ValueText(String) : String",
                "M op_Equality(HostSyncPayload, HostSyncPayload) : Boolean",
                "M op_Inequality(HostSyncPayload, HostSyncPayload) : Boolean",
                "P CategoryFileText : String",
                "P Settings : IReadOnlyList<KeyValuePair<String, String>>"),
            Members(type));
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly));
    }

    // GW-U-1011 (contract 3.1): HostSyncValueText is static with exactly the two contracted methods.
    [Fact]
    public void ValueTextMembersAreExactlyTheContractList()
    {
        Assert.True(typeof(HostSyncValueText).IsAbstract && typeof(HostSyncValueText).IsSealed, "HostSyncValueText must be static.");
        Assert.Equal(
            Sorted(
                "M IsCanonical(SettingKind, String) : Boolean",
                "M TryCanonicalize(SettingKind, String, String&) : Boolean"),
            Members(typeof(HostSyncValueText)));
        Assert.Empty(typeof(HostSyncValueText).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));
    }

    // GW-U-1011 (contract 4.1): HostSyncCodec is static with the two consts and the four contracted methods.
    [Fact]
    public void CodecMembersAreExactlyTheContractList()
    {
        Assert.True(typeof(HostSyncCodec).IsAbstract && typeof(HostSyncCodec).IsSealed, "HostSyncCodec must be static.");
        Assert.Equal(
            Sorted(
                "M Decode(Byte[]) : HostSyncPayload",
                "M Decode(String) : HostSyncPayload",
                "M Encode(HostSyncPayload) : String",
                "M EncodeBytes(HostSyncPayload) : Byte[]"),
            Members(typeof(HostSyncCodec)));

        FieldInfo[] fields = typeof(HostSyncCodec).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .OrderBy(field => field.Name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "MaxPayloadLength", "PayloadTag" }, fields.Select(field => field.Name));
        Assert.All(fields, field => Assert.True(field.IsLiteral, field.Name + " must be a const."));
        Assert.Equal(1048576, fields[0].GetRawConstantValue());
        Assert.Equal("BSC1;", fields[1].GetRawConstantValue());
    }

    // GW-U-1011 (contract 5.1): LayeredSettingsSource is sealed, implements ISettingsSource, and has exactly the contracted members.
    [Fact]
    public void LayeredSourceMembersAreExactlyTheContractList()
    {
        Type type = typeof(LayeredSettingsSource);

        Assert.True(type.IsSealed);
        Assert.True(typeof(ISettingsSource).IsAssignableFrom(type));
        ConstructorInfo ctor = Assert.Single(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Equal("local", Assert.Single(ctor.GetParameters()).Name);
        Assert.Equal(
            Sorted(
                "M ApplyHost(HostSyncPayload) : Void",
                "M ClearHost() : Void",
                "M TryApplyHost(Byte[]) : Boolean",
                "M TryApplyHost(String) : Boolean",
                "M TryGetBool(String, Boolean&) : Boolean",
                "M TryGetInt(String, Int32&) : Boolean",
                "M TryGetText(String, String&) : Boolean",
                "P HasHost : Boolean",
                "P Host : HostSyncPayload"),
            Members(type));
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly));
    }

    // GW-U-1011 (R11): the nullable parameters and returns are reported as Nullable, and the non-nullable ones as NotNull.
    [Fact]
    public void NullabilityAnnotationsFollowTheContract()
    {
        var context = new NullabilityInfoContext();
        MethodInfo decodeText = typeof(HostSyncCodec).GetMethod("Decode", [typeof(string)])!;
        MethodInfo decodeBytes = typeof(HostSyncCodec).GetMethod("Decode", [typeof(byte[])])!;
        MethodInfo applyText = typeof(LayeredSettingsSource).GetMethod("TryApplyHost", [typeof(string)])!;
        MethodInfo applyBytes = typeof(LayeredSettingsSource).GetMethod("TryApplyHost", [typeof(byte[])])!;

        Assert.Equal(NullabilityState.Nullable, context.Create(decodeText.GetParameters()[0]).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(decodeText.ReturnParameter).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(decodeBytes.GetParameters()[0]).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(decodeBytes.ReturnParameter).ReadState);
        Assert.Equal(NullabilityState.NotNull, context.Create(typeof(HostSyncCodec).GetMethod("Encode")!.GetParameters()[0]).ReadState);
        Assert.Equal(NullabilityState.NotNull, context.Create(typeof(HostSyncCodec).GetMethod("Encode")!.ReturnParameter).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(applyText.GetParameters()[0]).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(applyBytes.GetParameters()[0]).ReadState);
        Assert.Equal(NullabilityState.NotNull, context.Create(typeof(LayeredSettingsSource).GetMethod("ApplyHost")!.GetParameters()[0]).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(LayeredSettingsSource).GetProperty("Host")!).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(HostSyncPayload).GetMethod("Equals", [typeof(HostSyncPayload)])!.GetParameters()[0]).ReadState);
        Assert.Equal(NullabilityState.NotNull, context.Create(typeof(HostSyncPayload).GetProperty("CategoryFileText")!).ReadState);
        Assert.Equal(NullabilityState.NotNull, context.Create(typeof(HostSyncPayload).GetProperty("Settings")!).ReadState);
        Assert.Equal(NullabilityState.NotNull, context.Create(typeof(HostSyncPayload).GetMethod("Capture")!.GetParameters()[0]).ReadState);
        Assert.Equal(NullabilityState.NotNull, context.Create(typeof(HostSyncPayload).GetMethod("Capture")!.GetParameters()[1]).ReadState);
        Assert.Equal(NullabilityState.NotNull, context.Create(typeof(HostSyncPayload).GetMethod("ValueText")!.GetParameters()[0]).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(HostSyncValueText).GetMethod("TryCanonicalize")!.GetParameters()[1]).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(HostSyncValueText).GetMethod("IsCanonical")!.GetParameters()[1]).ReadState);
    }

    // GW-U-1011 (R4): every public property of the four types is get-only.
    [Fact]
    public void EveryPublicPropertyIsGetOnly()
    {
        Type[] types = [typeof(HostSyncPayload), typeof(HostSyncValueText), typeof(HostSyncCodec), typeof(LayeredSettingsSource)];

        foreach (Type type in types)
        {
            Assert.All(type.GetProperties(PublicDeclared), property => Assert.Null(property.SetMethod));
        }
    }

    private static string[] Sorted(params string[] items) => items.OrderBy(item => item, StringComparer.Ordinal).ToArray();

    private static string[] Members(Type type)
    {
        var lines = new List<string>();
        foreach (MethodInfo method in type.GetMethods(PublicDeclared))
        {
            if (method.IsSpecialName && !method.Name.StartsWith("op_", StringComparison.Ordinal))
            {
                continue;
            }

            string parameters = string.Join(", ", method.GetParameters().Select(parameter => Name(parameter.ParameterType)));
            lines.Add("M " + method.Name + "(" + parameters + ") : " + Name(method.ReturnType));
        }

        foreach (PropertyInfo property in type.GetProperties(PublicDeclared))
        {
            lines.Add("P " + property.Name + " : " + Name(property.PropertyType));
        }

        return Sorted(lines.ToArray());
    }

    private static string Name(Type type)
    {
        if (type.IsGenericType)
        {
            string definition = type.Name.Substring(0, type.Name.IndexOf('`'));
            return definition + "<" + string.Join(", ", type.GetGenericArguments().Select(Name)) + ">";
        }

        return type.Name;
    }
}
