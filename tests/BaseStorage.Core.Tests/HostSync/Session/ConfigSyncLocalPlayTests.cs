namespace BaseStorage.Core.Tests.HostSync.Session;

using BaseStorage.Core.Categories;
using BaseStorage.Core.HostSync.Session;
using BaseStorage.Core.Settings;

/// <summary>
/// S-E006-004 AC-4 (rule S5, contract 4.2 V1 and R1): local play and a client that never receives a payload send nothing and wait for nothing.
/// Solo play never reaches OnVersionOutcome at all, because an empty PeerVersionTable produces no outcome (GW-U-1257).
/// </summary>
public sealed class ConfigSyncLocalPlayTests
{
    // GW-U-1413 (AC-4): in LocalWorld and Menu, every tick after the first transition returns None.
    [Fact]
    public void LocalPlayTicksReturnOnlyNoneAfterTheFirstTransition()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());

        Assert.Equal(ConfigSyncTick.ReloadLocalCategories, session.Tick(ConfigSyncMode.LocalWorld));
        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(ConfigSyncTick.None, session.Tick(ConfigSyncMode.LocalWorld));
        }

        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(ConfigSyncTick.None, session.Tick(ConfigSyncMode.Menu));
        }
    }

    // GW-U-1413 (R1, V3): a payload or a client accept in local play changes nothing.
    [Fact]
    public void ReceiveAndAcceptInLocalPlayChangeNothing()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());
        session.Tick(ConfigSyncMode.LocalWorld);
        session.LoadLocalCategories(ConfigSyncFixtures.LocalText);
        CategoryCatalog before = session.Categories;

        ConfigSyncOutcome received = session.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.LocalWorld);
        ConfigSyncOutcome accepted = session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.LocalWorld);

        Assert.Equal(ConfigSyncAction.Rejected, received.Action);
        Assert.Equal(ConfigSyncRejectReason.NotClient, received.Reason);
        Assert.Equal(ConfigSyncAction.None, accepted.Action);
        Assert.False(session.Settings.HasHost);
        Assert.Null(session.HostPeerId);
        Assert.Same(before, session.Categories);
    }

    // GW-U-1413 (AC-4): a client that accepted the host but never receives a payload keeps its local values and catalog through 1000 ticks.
    [Fact]
    public void AClientThatNeverReceivesAPayloadKeepsItsLocalValues()
    {
        var local = ConfigSyncFixtures.Local();
        var session = new ConfigSyncSession(local);
        session.Tick(ConfigSyncMode.Client);
        session.LoadLocalCategories(ConfigSyncFixtures.LocalText);
        session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);
        CategoryCatalog before = session.Categories;
        var settings = new BaseStorageSettings(session.Settings);

        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(ConfigSyncTick.None, session.Tick(ConfigSyncMode.Client));
        }

        Assert.False(session.Settings.HasHost);
        Assert.True(settings.CraftFromBase);
        Assert.Equal(64, settings.MaxBaseStations);
        Assert.Same(before, session.Categories);
        Assert.Equal(new[] { "Stone" }, session.Categories.CategoryNames);
    }
}
