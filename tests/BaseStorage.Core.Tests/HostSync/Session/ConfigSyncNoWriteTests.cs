namespace BaseStorage.Core.Tests.HostSync.Session;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.HostSync.Session;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Settings;

/// <summary>S-E006-004 AC-2 (rule S4, ADR-GWIN-121): the local source is never written, and the session exposes no path back to it.</summary>
public sealed class ConfigSyncNoWriteTests
{
    // GW-U-1409 (4.4 "local unchanged"): the local source keeps its own values through apply, a malformed receive, clear and reapply.
    [Fact]
    public void TheLocalSourceIsNeverWrittenAcrossASession()
    {
        FakeSettingsSource local = ConfigSyncFixtures.Local();
        var session = new ConfigSyncSession(local);

        session.Tick(ConfigSyncMode.Client);
        session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);
        AssertLocalUnchanged(local);

        session.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.Client);
        AssertLocalUnchanged(local);

        session.Receive(1, ConfigSyncFixtures.Utf8("BSC1;garbage"), ConfigSyncMode.Client);
        AssertLocalUnchanged(local);

        session.Tick(ConfigSyncMode.Menu);
        AssertLocalUnchanged(local);

        session.Tick(ConfigSyncMode.Client);
        session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);
        session.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.Client);
        AssertLocalUnchanged(local);
    }

    // GW-U-1409 (S4, reflection): ISettingsSource declares only the three TryGet methods and no setter, and no public member of the session returns the local source.
    [Fact]
    public void TheSourceInterfaceHasNoWriteMemberAndTheSessionReturnsNoSource()
    {
        var local = ConfigSyncFixtures.Local();
        var session = new ConfigSyncSession(local);

        Assert.Equal(new[] { "TryGetBool", "TryGetInt", "TryGetText" }, typeof(ISettingsSource).GetMethods().Select(method => method.Name).OrderBy(name => name, StringComparer.Ordinal));
        Assert.Empty(typeof(ISettingsSource).GetProperties());

        const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        IEnumerable<Type> returned = typeof(ConfigSyncSession).GetMethods(Declared).Select(method => method.ReturnType)
            .Concat(typeof(ConfigSyncSession).GetProperties(Declared).Select(property => property.PropertyType));
        Assert.DoesNotContain(typeof(ISettingsSource), returned);
        Assert.NotNull(session.Settings);
    }

    private static void AssertLocalUnchanged(FakeSettingsSource local)
    {
        Assert.True(local.TryGetBool("CraftFromBase", out bool craft));
        Assert.True(craft);
        Assert.True(local.TryGetInt("MaxBaseStations", out int max));
        Assert.Equal(64, max);
    }
}
