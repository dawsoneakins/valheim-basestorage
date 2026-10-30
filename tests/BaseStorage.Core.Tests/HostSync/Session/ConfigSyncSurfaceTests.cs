namespace BaseStorage.Core.Tests.HostSync.Session;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Categories;
using BaseStorage.Core.HostSync;
using BaseStorage.Core.HostSync.Session;
using BaseStorage.Core.Logging;
using BaseStorage.Core.Networking.Peers;
using BaseStorage.Core.Settings;

/// <summary>S-E006-004 AC-8 (contract 1, 2.1, 3, 4.1, 5.1): the exported shape of BaseStorage.Core.HostSync.Session, checked by reflection.</summary>
public sealed class ConfigSyncSurfaceTests
{
    private const string SessionNamespace = "BaseStorage.Core.HostSync.Session";
    private const string HostSyncNamespace = "BaseStorage.Core.HostSync";
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-1400 (contract 1): the exported types of the Session namespace are exactly the eight contract names; HostSync still exports four.
    [Fact]
    public void ExportedTypesAreExactlyTheEightSessionNames()
    {
        string[] session = typeof(ConfigSyncSession).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == SessionNamespace)
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "ConfigSyncAction",
                "ConfigSyncMessages",
                "ConfigSyncMode",
                "ConfigSyncOutcome",
                "ConfigSyncRejectReason",
                "ConfigSyncRpc",
                "ConfigSyncSession",
                "ConfigSyncTick",
            },
            session);

        int hostSync = typeof(ConfigSyncSession).Assembly.GetExportedTypes().Count(type => type.Namespace == HostSyncNamespace);
        Assert.Equal(4, hostSync);
    }

    // GW-U-1400 (3.1-3.4, 2.1): the enum names and values hold, ConfigSyncTick is flags, and the RPC name is the contracted one.
    [Fact]
    public void EnumValuesAndTheRpcNameMatchTheContract()
    {
        Assert.Equal(new[] { "Menu=0", "LocalWorld=1", "Client=2" }, EnumText<ConfigSyncMode>());
        Assert.Equal(new[] { "None=0", "Send=1", "Applied=2", "Rejected=3" }, EnumText<ConfigSyncAction>());
        Assert.Equal(new[] { "None=0", "NotClient=1", "NoAcceptedHost=2", "Malformed=3" }, EnumText<ConfigSyncRejectReason>());
        Assert.Equal(new[] { "None=0", "HostCleared=1", "ReloadLocalCategories=2" }, EnumText<ConfigSyncTick>());
        Assert.NotNull(typeof(ConfigSyncTick).GetCustomAttribute<FlagsAttribute>());

        Assert.Equal("BaseStorage_ConfigSync", ConfigSyncRpc.Name);
        Assert.NotEqual(VersionRpc.Name, ConfigSyncRpc.Name);
        Assert.StartsWith("BaseStorage_", ConfigSyncRpc.Name, StringComparison.Ordinal);
        Assert.StartsWith("BaseStorage_", VersionRpc.Name, StringComparison.Ordinal);
    }

    // GW-U-1400 (3.5): ConfigSyncOutcome is sealed, has no public constructor, and its properties are get-only with the contracted types.
    [Fact]
    public void OutcomeHasTheContractedShapeAndNoPublicConstructor()
    {
        Type type = typeof(ConfigSyncOutcome);

        Assert.True(type.IsSealed);
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.All(type.GetProperties(PublicDeclared), property => Assert.False(property.CanWrite));
        Assert.Equal(
            new (string, Type)[]
            {
                ("Action", typeof(ConfigSyncAction)),
                ("CategoryCount", typeof(int)),
                ("Payload", typeof(HostSyncPayload)),
                ("PeerId", typeof(long)),
                ("Reason", typeof(ConfigSyncRejectReason)),
                ("SettingCount", typeof(int)),
            },
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).OrderBy(property => property.Name, StringComparer.Ordinal)
                .Select(property => (property.Name, property.PropertyType))
                .ToArray());
        Assert.Equal(typeof(ConfigSyncOutcome), type.GetProperty("None", BindingFlags.Public | BindingFlags.Static)!.PropertyType);
    }

    // GW-U-1400 (4.1): ConfigSyncSession is sealed, has exactly the contracted members, and its signatures match the contract.
    [Fact]
    public void SessionMembersAreExactlyTheContractList()
    {
        Type type = typeof(ConfigSyncSession);

        Assert.True(type.IsSealed);
        Assert.Equal(new[] { "BuildHostPayload", "LoadLocalCategories", "OnVersionOutcome", "Receive", "Tick" }, Methods(type));
        Assert.Equal(new[] { "Categories", "HostPeerId", "Mode", "Settings" }, Properties(type));
        Assert.Equal(typeof(LayeredSettingsSource), type.GetProperty("Settings")!.PropertyType);
        Assert.Equal(typeof(ConfigSyncMode), type.GetProperty("Mode")!.PropertyType);
        Assert.Equal(typeof(long?), type.GetProperty("HostPeerId")!.PropertyType);
        Assert.Equal(typeof(CategoryCatalog), type.GetProperty("Categories")!.PropertyType);

        AssertSignature(type, "LoadLocalCategories", typeof(void), typeof(string));
        AssertSignature(type, "Tick", typeof(ConfigSyncTick), typeof(ConfigSyncMode));
        AssertSignature(type, "OnVersionOutcome", typeof(ConfigSyncOutcome), typeof(PeerVersionOutcome), typeof(ConfigSyncMode));
        AssertSignature(type, "BuildHostPayload", typeof(byte[]), typeof(string));
        AssertSignature(type, "Receive", typeof(ConfigSyncOutcome), typeof(long), typeof(byte[]), typeof(ConfigSyncMode));
        Assert.NotNull(type.GetConstructor(new[] { typeof(ISettingsSource) }));
    }

    // GW-U-1400 (5.1): ConfigSyncMessages is a static class with the eight contracted methods and their signatures.
    [Fact]
    public void MessagesAreAStaticClassWithTheContractedMethods()
    {
        Type type = typeof(ConfigSyncMessages);

        Assert.True(type.IsAbstract && type.IsSealed);
        Assert.Equal(
            new[] { "CategoryFileUnavailable", "CategoryOf", "Cleared", "Format", "IsWarning", "Sent", "SettingDetail", "TooLarge" },
            Methods(type));
        AssertSignature(type, "Format", typeof(string), typeof(ConfigSyncOutcome));
        AssertSignature(type, "IsWarning", typeof(bool), typeof(ConfigSyncOutcome));
        AssertSignature(type, "CategoryOf", typeof(LogCategory), typeof(ConfigSyncOutcome));
        AssertSignature(type, "Sent", typeof(string), typeof(long), typeof(int));
        AssertSignature(type, "TooLarge", typeof(string), typeof(long));
        AssertSignature(type, "SettingDetail", typeof(string), typeof(string), typeof(string));
        AssertSignature(type, "Cleared", typeof(string));
        AssertSignature(type, "CategoryFileUnavailable", typeof(string), typeof(string));
    }

    private static string[] EnumText<T>() where T : struct, Enum =>
        Enum.GetValues<T>().Select(value => value.ToString() + "=" + Convert.ToInt32(value).ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();

    private static string[] Methods(Type type) =>
        type.GetMethods(PublicDeclared)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    private static string[] Properties(Type type) =>
        type.GetProperties(PublicDeclared)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    private static void AssertSignature(Type type, string name, Type returnType, params Type[] parameters)
    {
        MethodInfo? method = type.GetMethod(name, parameters);
        Assert.NotNull(method);
        Assert.Equal(returnType, method.ReturnType);
    }
}
