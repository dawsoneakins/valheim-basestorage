namespace BaseStorage.Core.Tests.HostSync.Session;

using System;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Categories;
using BaseStorage.Core.HostSync;
using BaseStorage.Core.HostSync.Session;
using BaseStorage.Core.Networking;
using BaseStorage.Core.Networking.Peers;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Tests.Settings;

/// <summary>S-E006-004 AC-1 (contract 4.1, 4.2 V1-V3 and R4): construction, the host send matrix, the client accept and the apply.</summary>
public sealed class ConfigSyncSessionTests
{
    // GW-U-1403 (4.4 new): a new session is in Menu with no host, the empty catalog, and the local source under its view.
    [Fact]
    public void ANewSessionStartsInMenuWithNoHostAndTheEmptyCatalog()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());

        Assert.Equal(ConfigSyncMode.Menu, session.Mode);
        Assert.Null(session.HostPeerId);
        Assert.False(session.Settings.HasHost);
        Assert.Same(CategoryCatalog.Empty, session.Categories);
        var settings = new BaseStorageSettings(session.Settings);
        Assert.True(settings.CraftFromBase);
        Assert.Equal(64, settings.MaxBaseStations);
    }

    // GW-U-1403: a null local source is refused with the parameter name "local".
    [Fact]
    public void ANullLocalSourceIsRefusedWithItsParameterName()
    {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => new ConfigSyncSession(null!));
        Assert.Equal("local", error.ParamName);
    }

    // GW-U-1404 (V1, V2 host, V3): only an accepted remote client in LocalWorld is sent to; every other outcome or mode gives None.
    [Theory]
    [InlineData("hostAccept", ConfigSyncMode.LocalWorld, ConfigSyncAction.Send)]
    [InlineData("lenientNone", ConfigSyncMode.LocalWorld, ConfigSyncAction.None)]
    [InlineData("refused", ConfigSyncMode.LocalWorld, ConfigSyncAction.None)]
    [InlineData("hostAccept", ConfigSyncMode.Client, ConfigSyncAction.None)]
    [InlineData("hostAccept", ConfigSyncMode.Menu, ConfigSyncAction.None)]
    [InlineData("clientAccept", ConfigSyncMode.LocalWorld, ConfigSyncAction.None)]
    [InlineData("disconnect", ConfigSyncMode.LocalWorld, ConfigSyncAction.None)]
    [InlineData("ignore", ConfigSyncMode.LocalWorld, ConfigSyncAction.None)]
    public void OnlyAnAcceptedRemoteClientInLocalWorldIsSent(string outcomeName, ConfigSyncMode mode, ConfigSyncAction expected)
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());

        ConfigSyncOutcome result = session.OnVersionOutcome(ConfigSyncFixtures.Named(outcomeName), mode);

        Assert.Equal(expected, result.Action);
        if (expected == ConfigSyncAction.Send)
        {
            Assert.Equal(1L, result.PeerId);
        }
    }

    // GW-U-1404: a missing outcome is refused with the parameter name "outcome".
    [Fact]
    public void AMissingOutcomeIsRefusedWithItsParameterName()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());

        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => session.OnVersionOutcome(null!, ConfigSyncMode.LocalWorld));
        Assert.Equal("outcome", error.ParamName);
    }

    // GW-U-1405 (V2): an accepted host in Client mode gives None and records the host peer; a leave records nothing.
    [Fact]
    public void AcceptedHostInClientModeIsRecordedAndReturnsNone()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());

        ConfigSyncOutcome result = session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);

        Assert.Equal(ConfigSyncAction.None, result.Action);
        Assert.Equal(1L, session.HostPeerId);
    }

    // GW-U-1405: a leave mismatch in Client mode gives None and leaves the host peer null.
    [Fact]
    public void ClientLeaveRecordsNoHost()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());

        ConfigSyncOutcome result = session.OnVersionOutcome(ConfigSyncFixtures.ClientLeave(), ConfigSyncMode.Client);

        Assert.Equal(ConfigSyncAction.None, result.Action);
        Assert.Null(session.HostPeerId);
    }

    // GW-U-1405: an accept outside Client mode records nothing.
    [Fact]
    public void AcceptInLocalWorldRecordsNothing()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());

        ConfigSyncOutcome result = session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.LocalWorld);

        Assert.Equal(ConfigSyncAction.None, result.Action);
        Assert.Null(session.HostPeerId);
    }

    // GW-U-1405: a second accept overwrites the recorded host id.
    [Fact]
    public void ASecondAcceptOverwritesTheHostId()
    {
        var table = new PeerVersionTable(new ManualClock(TimeSpan.FromSeconds(100)), "0.1.0");
        long first = table.Track(true);
        long second = table.Track(true);
        PeerVersionOutcome firstOutcome = table.Receive(first, VersionHandshake.EncodePayloadBytes("0.1.0"), true);
        PeerVersionOutcome secondOutcome = table.Receive(second, VersionHandshake.EncodePayloadBytes("0.1.0"), true);
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());

        session.OnVersionOutcome(firstOutcome, ConfigSyncMode.Client);
        Assert.Equal(first, session.HostPeerId);

        session.OnVersionOutcome(secondOutcome, ConfigSyncMode.Client);
        Assert.Equal(second, session.HostPeerId);
    }

    // GW-U-1407 (R4, 4.4 applied): after the accepted host, the host payload is applied, the layered view reads the host values, and the catalog is the host catalog.
    [Fact]
    public void AValidHostPayloadIsAppliedForTheSession()
    {
        var local = ConfigSyncFixtures.Local();
        var session = new ConfigSyncSession(local);
        session.Tick(ConfigSyncMode.Client);
        session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);

        ConfigSyncOutcome applied = session.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.Client);

        Assert.Equal(ConfigSyncAction.Applied, applied.Action);
        Assert.Equal(1L, applied.PeerId);
        Assert.Equal(9, applied.SettingCount);
        Assert.Equal(2, applied.CategoryCount);
        Assert.Same(session.Settings.Host, applied.Payload);

        var settings = new BaseStorageSettings(session.Settings);
        Assert.False(settings.CraftFromBase);
        Assert.Equal(3, settings.MaxBaseStations);
        Assert.Equal(1500, settings.ReserveTimeoutMs);
        Assert.Equal(2, settings.BaseCacheSeconds);
        Assert.Equal(new[] { "Wood", "Ores" }, session.Categories.CategoryNames);
    }

    // GW-U-1407: a second valid payload from the same accepted host replaces the first.
    [Fact]
    public void ASecondValidPayloadFromTheSameHostReplacesTheFirst()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());
        session.Tick(ConfigSyncMode.Client);
        session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);
        session.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.Client);
        var settings = new BaseStorageSettings(session.Settings);
        byte[] second = HostSyncCodec.EncodeBytes(HostSyncPayload.Capture(new FakeSettingsSource().WithInt("MaxBaseStations", 5), "Clay: Clay\n"));

        ConfigSyncOutcome again = session.Receive(1, second, ConfigSyncMode.Client);

        Assert.Equal(ConfigSyncAction.Applied, again.Action);
        Assert.Equal(5, settings.MaxBaseStations);
        Assert.Equal(new[] { "Clay" }, session.Categories.CategoryNames);
    }

    // GW-U-1408 (R-rules, principle 5): a payload is decided by the mode passed to Receive, so one that arrives before the first Client tick is applied.
    [Fact]
    public void ReceiveIsDecidedByItsOwnModeBeforeTheFirstClientTick()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());
        session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);

        ConfigSyncOutcome applied = session.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.Client);

        Assert.Equal(ConfigSyncAction.Applied, applied.Action);
        Assert.Equal(ConfigSyncMode.Menu, session.Mode);
        Assert.Equal(3, new BaseStorageSettings(session.Settings).MaxBaseStations);
    }
}
