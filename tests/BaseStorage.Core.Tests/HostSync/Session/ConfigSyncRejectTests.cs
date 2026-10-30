namespace BaseStorage.Core.Tests.HostSync.Session;

using System.Collections.Generic;
using BaseStorage.Core.Categories;
using BaseStorage.Core.HostSync.Session;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Settings;

/// <summary>S-E006-004 AC-5 (contract 4.2 R1-R3, H5): a malformed or unexpected payload leaves the active settings and catalog unchanged.</summary>
public sealed class ConfigSyncRejectTests
{
    /// <summary>The malformed payload cases of the R3 matrix, by name.</summary>
    public static IEnumerable<object[]> MalformedCases() =>
        new[] { "null", "empty", "0xFF", "garbage", "truncated", "wrong tag", "oversize" }.Select(name => new object[] { name });

    // GW-U-1415 (R3, H5): each malformed payload is rejected as Malformed, both before any apply and after an applied H, and the applied layer stays.
    [Theory]
    [MemberData(nameof(MalformedCases))]
    public void MalformedPayloadsAreRejectedAndTheActiveLayerStays(string name)
    {
        FakeSettingsSource local = ConfigSyncFixtures.Local();
        var session = new ConfigSyncSession(local);
        var settings = new BaseStorageSettings(session.Settings);
        session.LoadLocalCategories(ConfigSyncFixtures.LocalText);
        session.Tick(ConfigSyncMode.Client);
        session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);

        ConfigSyncOutcome before = session.Receive(1, ConfigSyncFixtures.MalformedBytes(name), ConfigSyncMode.Client);

        Assert.Equal(ConfigSyncAction.Rejected, before.Action);
        Assert.Equal(ConfigSyncRejectReason.Malformed, before.Reason);
        Assert.False(session.Settings.HasHost);
        Assert.Equal(64, settings.MaxBaseStations);
        Assert.Equal(new[] { "Stone" }, session.Categories.CategoryNames);

        session.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.Client);
        ConfigSyncOutcome after = session.Receive(1, ConfigSyncFixtures.MalformedBytes(name), ConfigSyncMode.Client);

        Assert.Equal(ConfigSyncAction.Rejected, after.Action);
        Assert.Equal(ConfigSyncRejectReason.Malformed, after.Reason);
        Assert.True(session.Settings.HasHost);
        Assert.Equal(3, settings.MaxBaseStations);
        Assert.Equal(new[] { "Wood", "Ores" }, session.Categories.CategoryNames);
    }

    // GW-U-1416 (R1): a payload received outside Client mode is NotClient and changes nothing.
    [Theory]
    [InlineData(ConfigSyncMode.LocalWorld)]
    [InlineData(ConfigSyncMode.Menu)]
    public void ReceivingOutsideClientModeChangesNothing(ConfigSyncMode mode)
    {
        FakeSettingsSource local = ConfigSyncFixtures.Local();
        var session = new ConfigSyncSession(local);
        var settings = new BaseStorageSettings(session.Settings);
        session.LoadLocalCategories(ConfigSyncFixtures.LocalText);
        CategoryCatalog before = session.Categories;

        ConfigSyncOutcome outcome = session.Receive(1, ConfigSyncFixtures.H(), mode);

        Assert.Equal(ConfigSyncAction.Rejected, outcome.Action);
        Assert.Equal(ConfigSyncRejectReason.NotClient, outcome.Reason);
        Assert.False(session.Settings.HasHost);
        Assert.Null(session.HostPeerId);
        Assert.Equal(64, settings.MaxBaseStations);
        Assert.Same(before, session.Categories);
    }

    // GW-U-1416 (R2): a payload without an accepted host (none yet, another peer, after a leave, after the host was cleared) is NoAcceptedHost and changes nothing.
    [Theory]
    [InlineData("noAccept")]
    [InlineData("otherPeer")]
    [InlineData("afterLeave")]
    [InlineData("afterMenu")]
    public void ReceivingWithoutAnAcceptedHostChangesNothing(string scenario)
    {
        FakeSettingsSource local = ConfigSyncFixtures.Local();
        var session = new ConfigSyncSession(local);
        var settings = new BaseStorageSettings(session.Settings);
        session.LoadLocalCategories(ConfigSyncFixtures.LocalText);
        session.Tick(ConfigSyncMode.Client);
        long peer = 1;
        if (scenario == "otherPeer")
        {
            session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);
            peer = 2;
        }
        else if (scenario == "afterLeave")
        {
            session.OnVersionOutcome(ConfigSyncFixtures.ClientLeave(), ConfigSyncMode.Client);
        }
        else if (scenario == "afterMenu")
        {
            session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);
            session.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.Client);
            session.Tick(ConfigSyncMode.Menu);
            session.Tick(ConfigSyncMode.Client);
        }

        CategoryCatalog before = session.Categories;
        long? hostBefore = session.HostPeerId;
        ConfigSyncOutcome outcome = session.Receive(peer, ConfigSyncFixtures.H(), ConfigSyncMode.Client);

        Assert.Equal(ConfigSyncAction.Rejected, outcome.Action);
        Assert.Equal(ConfigSyncRejectReason.NoAcceptedHost, outcome.Reason);
        Assert.False(session.Settings.HasHost);
        Assert.Equal(hostBefore, session.HostPeerId);
        Assert.Equal(64, settings.MaxBaseStations);
        Assert.Same(before, session.Categories);
        Assert.Equal(new[] { "Stone" }, session.Categories.CategoryNames);
    }
}
