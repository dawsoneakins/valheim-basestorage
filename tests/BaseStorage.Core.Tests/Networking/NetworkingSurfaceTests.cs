namespace BaseStorage.Core.Tests.Networking;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Networking;

/// <summary>AC-6.1 (contract 1, 2.1, 3.1, 4.1, 4.2, 4.3; ADR-GWIN-064): the exported shape of BaseStorage.Core.Networking, checked by reflection.</summary>
public sealed class NetworkingSurfaceTests
{
    private const string NetworkingNamespace = "BaseStorage.Core.Networking";
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-812 (contract 1): the exported types of the namespace are exactly the five contract names, ordinal-sorted.
    [Fact]
    public void ExportedTypesAreExactlyTheFiveNetworkingNames()
    {
        string[] exported = typeof(VersionCheck).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == NetworkingNamespace)
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "BaseStorage.Core.Networking.ModVersion",
                "BaseStorage.Core.Networking.VersionCheck",
                "BaseStorage.Core.Networking.VersionCheckResult",
                "BaseStorage.Core.Networking.VersionCheckStatus",
                "BaseStorage.Core.Networking.VersionHandshake",
            },
            exported);
    }

    // GW-U-812 (contract 2.2): the generated build constant is internal, not exported.
    [Fact]
    public void BuildConstantIsInternal()
    {
        Type? buildInfo = typeof(ModVersion).Assembly.GetType("BaseStorage.Core.CoreBuildInfo");

        Assert.NotNull(buildInfo);
        Assert.False(buildInfo!.IsPublic, "CoreBuildInfo must be internal.");
        FieldInfo? version = buildInfo.GetField("Version", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(version);
        Assert.True(version!.IsLiteral, "CoreBuildInfo.Version must be a const.");
    }

    // GW-U-812 (contract 2.1): ModVersion is a static class with exactly Current and IsWellFormed.
    [Fact]
    public void ModVersionMembersAreExactlyTheContractList()
    {
        Assert.True(typeof(ModVersion).IsAbstract && typeof(ModVersion).IsSealed, "ModVersion must be static.");
        Assert.Equal(
            Sorted(
                "M IsWellFormed(String) : Boolean",
                "P Current : String"),
            Members(typeof(ModVersion)));
    }

    // GW-U-812 (contract 3.1): VersionHandshake is a static class with the tag const and the five contracted members.
    [Fact]
    public void HandshakeMembersAreExactlyTheContractList()
    {
        Assert.True(typeof(VersionHandshake).IsAbstract && typeof(VersionHandshake).IsSealed, "VersionHandshake must be static.");
        Assert.Equal(
            Sorted(
                "M DecodePayload(Byte[]) : String",
                "M DecodePayload(String) : String",
                "M EncodePayload(String) : String",
                "M EncodePayloadBytes(String) : Byte[]",
                "P LocalPayload : String"),
            Members(typeof(VersionHandshake)));

        FieldInfo[] fields = typeof(VersionHandshake).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        FieldInfo tag = Assert.Single(fields);
        Assert.Equal("PayloadTag", tag.Name);
        Assert.True(tag.IsLiteral);
        Assert.Equal("BSV1;", tag.GetRawConstantValue());
    }

    // GW-U-812 (contract 4.3): VersionCheck is a static class with Compare and the two CheckPayload overloads.
    [Fact]
    public void CheckMembersAreExactlyTheContractList()
    {
        Assert.True(typeof(VersionCheck).IsAbstract && typeof(VersionCheck).IsSealed, "VersionCheck must be static.");
        Assert.Equal(
            Sorted(
                "M CheckPayload(String, Byte[], Boolean) : VersionCheckResult",
                "M CheckPayload(String, String, Boolean) : VersionCheckResult",
                "M Compare(String, String, Boolean) : VersionCheckResult"),
            Members(typeof(VersionCheck)));
        Assert.Empty(typeof(VersionCheck).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));
    }

    // GW-U-812 (contract 4.2): VersionCheckResult has exactly the contracted factories, properties, equality and FormatMessage.
    [Fact]
    public void ResultMembersAreExactlyTheContractList()
    {
        Assert.Equal(
            Sorted(
                "M Accepted(String, String) : VersionCheckResult",
                "M Equals(Object) : Boolean",
                "M Equals(VersionCheckResult) : Boolean",
                "M FormatMessage(StringTable) : String",
                "M GetHashCode() : Int32",
                "M Mismatch(String, String) : VersionCheckResult",
                "M Missing(String) : VersionCheckResult",
                "M op_Equality(VersionCheckResult, VersionCheckResult) : Boolean",
                "M op_Inequality(VersionCheckResult, VersionCheckResult) : Boolean",
                "P ClientVersion : String",
                "P HostVersion : String",
                "P IsAccepted : Boolean",
                "P MessageKey : String",
                "P Status : VersionCheckStatus"),
            Members(typeof(VersionCheckResult)));
    }

    // GW-U-812 (contract 4.2): VersionCheckResult is sealed, has no public constructor, and implements IEquatable of itself.
    [Fact]
    public void ResultIsSealedWithNoPublicConstructorAndIsEquatable()
    {
        Assert.True(typeof(VersionCheckResult).IsSealed, "VersionCheckResult must be sealed.");
        Assert.Empty(typeof(VersionCheckResult).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.True(typeof(IEquatable<VersionCheckResult>).IsAssignableFrom(typeof(VersionCheckResult)));
    }

    // GW-U-812 (contract 4.1): the status enum has exactly the three contracted values on an int base and no other members.
    [Fact]
    public void StatusEnumHasExactlyTheContractValues()
    {
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(VersionCheckStatus)));
        Assert.Equal(
            new[] { "Accepted=0", "RejectedMismatch=1", "RejectedMissing=2" },
            Enum.GetValues(typeof(VersionCheckStatus)).Cast<VersionCheckStatus>().OrderBy(v => (int)v).Select(v => v + "=" + (int)v).ToArray());
        Assert.Empty(Members(typeof(VersionCheckStatus)));
    }

    // GW-U-812 (R11): the nullable parameters and returns are reported as Nullable, and the non-nullable ones as NotNull.
    [Fact]
    public void NullabilityAnnotationsFollowTheContract()
    {
        var context = new NullabilityInfoContext();

        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(ModVersion).GetMethod("IsWellFormed")!.GetParameters()[0]).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(VersionHandshake).GetMethod("DecodePayload", [typeof(string)])!.GetParameters()[0]).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(VersionHandshake).GetMethod("DecodePayload", [typeof(string)])!.ReturnParameter).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(VersionHandshake).GetMethod("DecodePayload", [typeof(byte[])])!.GetParameters()[0]).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(VersionHandshake).GetMethod("DecodePayload", [typeof(byte[])])!.ReturnParameter).ReadState);
        Assert.Equal(NullabilityState.NotNull, context.Create(typeof(VersionHandshake).GetMethod("EncodePayload")!.GetParameters()[0]).ReadState);
        Assert.Equal(NullabilityState.NotNull, context.Create(typeof(VersionCheck).GetMethod("Compare")!.GetParameters()[0]).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(VersionCheck).GetMethod("Compare")!.GetParameters()[1]).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(VersionCheck).GetMethod("CheckPayload", [typeof(string), typeof(string), typeof(bool)])!.GetParameters()[1]).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(VersionCheck).GetMethod("CheckPayload", [typeof(string), typeof(byte[]), typeof(bool)])!.GetParameters()[1]).ReadState);
        Assert.Equal(NullabilityState.NotNull, context.Create(typeof(VersionCheckResult).GetProperty("HostVersion")!).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(VersionCheckResult).GetProperty("ClientVersion")!).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(VersionCheckResult).GetProperty("MessageKey")!).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(VersionCheckResult).GetMethod("FormatMessage")!.ReturnParameter).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(VersionCheckResult).GetMethod("Accepted")!.GetParameters()[1]).ReadState);
        Assert.Equal(NullabilityState.NotNull, context.Create(typeof(VersionCheckResult).GetMethod("Mismatch")!.GetParameters()[1]).ReadState);
    }

    // GW-U-812 (R4): every public property of the five types is get-only.
    [Fact]
    public void EveryPublicPropertyIsGetOnly()
    {
        Type[] types = [typeof(ModVersion), typeof(VersionHandshake), typeof(VersionCheck), typeof(VersionCheckResult), typeof(VersionCheckStatus)];

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
