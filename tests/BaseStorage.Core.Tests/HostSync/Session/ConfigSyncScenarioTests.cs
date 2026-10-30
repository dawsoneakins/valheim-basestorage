namespace BaseStorage.Core.Tests.HostSync.Session;

using System;
using BaseStorage.Core.HostSync;
using BaseStorage.Core.HostSync.Session;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Settings;

/// <summary>S-E006-004 AC-3 (contract 4.4): the examples table replayed top to bottom on one session, with literal results.</summary>
public sealed class ConfigSyncScenarioTests
{
    // GW-U-1412 (4.4): every row of the golden table, in order, on one session.
    [Fact]
    public void TheContractExampleTableReplaysWithTheListedResults()
    {
        FakeSettingsSource local = ConfigSyncFixtures.Local();
        var s = new ConfigSyncSession(local);
        var settings = new BaseStorageSettings(s.Settings);

        // new
        Assert.Equal(ConfigSyncMode.Menu, s.Mode);
        Assert.Null(s.HostPeerId);
        Assert.False(s.Settings.HasHost);
        Assert.Same(BaseStorage.Core.Categories.CategoryCatalog.Empty, s.Categories);
        Assert.Equal("local", Assert.Throws<ArgumentNullException>(() => new ConfigSyncSession(null!)).ParamName);

        // solo
        Assert.Equal(ConfigSyncTick.ReloadLocalCategories, s.Tick(ConfigSyncMode.LocalWorld));
        s.LoadLocalCategories(ConfigSyncFixtures.LocalText);
        Assert.Equal(new[] { "Stone" }, s.Categories.CategoryNames);
        Assert.Equal(ConfigSyncTick.None, s.Tick(ConfigSyncMode.LocalWorld));
        Assert.Equal(ConfigSyncAction.None, s.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.LocalWorld).Action);
        ConfigSyncOutcome solo = s.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.LocalWorld);
        Assert.Equal(ConfigSyncAction.Rejected, solo.Action);
        Assert.Equal(ConfigSyncRejectReason.NotClient, solo.Reason);
        Assert.False(s.Settings.HasHost);
        Assert.True(settings.CraftFromBase);

        // host
        ConfigSyncOutcome sendOutcome = s.OnVersionOutcome(ConfigSyncFixtures.HostAccept(), ConfigSyncMode.LocalWorld);
        Assert.Equal(ConfigSyncAction.Send, sendOutcome.Action);
        Assert.Equal(1L, sendOutcome.PeerId);
        Assert.Equal(ConfigSyncAction.None, s.OnVersionOutcome(ConfigSyncFixtures.LenientNone(), ConfigSyncMode.LocalWorld).Action);
        Assert.Equal(ConfigSyncAction.None, s.OnVersionOutcome(ConfigSyncFixtures.Refused(), ConfigSyncMode.LocalWorld).Action);
        Assert.Equal(ConfigSyncAction.None, s.OnVersionOutcome(ConfigSyncFixtures.HostAccept(), ConfigSyncMode.Client).Action);
        Assert.Equal(HostSyncCodec.EncodeBytes(HostSyncPayload.Capture(local, string.Empty)), s.BuildHostPayload(null));
        Assert.Null(s.BuildHostPayload(new string('a', HostSyncCodec.MaxPayloadLength)));
        Assert.Equal("outcome", Assert.Throws<ArgumentNullException>(() => s.OnVersionOutcome(null!, ConfigSyncMode.LocalWorld)).ParamName);

        // menu
        Assert.Equal(ConfigSyncTick.None, s.Tick(ConfigSyncMode.Menu));

        // client
        Assert.Equal(ConfigSyncTick.ReloadLocalCategories, s.Tick(ConfigSyncMode.Client));
        Assert.Equal(ConfigSyncRejectReason.NoAcceptedHost, s.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.Client).Reason);
        Assert.Equal(ConfigSyncAction.None, s.OnVersionOutcome(ConfigSyncFixtures.ClientLeave(), ConfigSyncMode.Client).Action);
        Assert.Null(s.HostPeerId);
        Assert.Equal(ConfigSyncAction.None, s.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client).Action);
        Assert.Equal(1L, s.HostPeerId);
        Assert.Equal(ConfigSyncRejectReason.NoAcceptedHost, s.Receive(2, ConfigSyncFixtures.H(), ConfigSyncMode.Client).Reason);

        foreach (byte[]? malformed in new byte[]?[] { null, new byte[] { 0xFF }, ConfigSyncFixtures.Utf8("BSC1;garbage") })
        {
            ConfigSyncOutcome rejected = s.Receive(1, malformed, ConfigSyncMode.Client);
            Assert.Equal(ConfigSyncAction.Rejected, rejected.Action);
            Assert.Equal(ConfigSyncRejectReason.Malformed, rejected.Reason);
        }

        Assert.False(s.Settings.HasHost);
        Assert.Equal(64, settings.MaxBaseStations);
        Assert.Equal(new[] { "Stone" }, s.Categories.CategoryNames);

        ConfigSyncOutcome applied = s.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.Client);
        Assert.Equal(ConfigSyncAction.Applied, applied.Action);
        Assert.Equal(1L, applied.PeerId);
        Assert.Equal(9, applied.SettingCount);
        Assert.Equal(2, applied.CategoryCount);
        Assert.Same(s.Settings.Host, applied.Payload);

        // applied
        Assert.False(settings.CraftFromBase);
        Assert.Equal(3, settings.MaxBaseStations);
        Assert.Equal(1500, settings.ReserveTimeoutMs);
        Assert.Equal(2, settings.BaseCacheSeconds);
        Assert.Equal(new[] { "Wood", "Ores" }, s.Categories.CategoryNames);
        Assert.True(local.TryGetBool("CraftFromBase", out bool localCraft) && localCraft);
        Assert.True(local.TryGetInt("MaxBaseStations", out int localMax) && localMax == 64);
        s.LoadLocalCategories("Clay: Clay\n");
        Assert.Equal(new[] { "Wood", "Ores" }, s.Categories.CategoryNames);
        Assert.Equal(ConfigSyncRejectReason.Malformed, s.Receive(1, ConfigSyncFixtures.Utf8("BSC1;garbage"), ConfigSyncMode.Client).Reason);
        Assert.Equal(3, settings.MaxBaseStations);
        Assert.Equal(new[] { "Wood", "Ores" }, s.Categories.CategoryNames);
        Assert.Equal(ConfigSyncTick.None, s.Tick(ConfigSyncMode.Client));

        // leave
        Assert.Equal(ConfigSyncTick.HostCleared, s.Tick(ConfigSyncMode.Menu));
        Assert.False(s.Settings.HasHost);
        Assert.Null(s.HostPeerId);
        Assert.True(settings.CraftFromBase);
        Assert.Equal(64, settings.MaxBaseStations);
        Assert.Equal(new[] { "Clay" }, s.Categories.CategoryNames);
        Assert.Equal(ConfigSyncTick.None, s.Tick(ConfigSyncMode.Menu));

        // host again: a fresh session, with no Menu tick between the client and the host
        var s2 = new ConfigSyncSession(local);
        s2.Tick(ConfigSyncMode.Client);
        s2.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);
        Assert.Equal(ConfigSyncAction.Applied, s2.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.Client).Action);
        Assert.Equal(ConfigSyncTick.HostCleared | ConfigSyncTick.ReloadLocalCategories, s2.Tick(ConfigSyncMode.LocalWorld));
        Assert.False(s2.Settings.HasHost);
        Assert.Null(s2.HostPeerId);
        Assert.Equal(ConfigSyncMode.LocalWorld, s2.Mode);

        // any: an undefined mode is Menu, and loading a null or bad text gives an empty local catalog
        Assert.Equal(ConfigSyncTick.None, s.Tick((ConfigSyncMode)99));
        s.LoadLocalCategories(null);
        s.LoadLocalCategories("Bad line");
        Assert.Empty(s.Categories.CategoryNames);
    }
}
