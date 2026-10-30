namespace BaseStorage.Core.Tests.HostSync.Session;

using System.Linq;
using BaseStorage.Core.HostSync.Session;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Settings;

/// <summary>S-E006-004 AC-3 (contract 4.2 T1-T4, rule S3, 4.4 "leave" rows): the session ends when the mode is not Client, and the local values and catalog come back.</summary>
public sealed class ConfigSyncTickTests
{
    // GW-U-1410 (T1, T2, T4): after an applied sync, Tick(Menu) clears the host, restores the local values and catalog, and a repeat gives None.
    [Fact]
    public void LeavingTheHostClearsTheAppliedLayerAndTheHostCatalog()
    {
        FakeSettingsSource local = ConfigSyncFixtures.Local();
        var session = new ConfigSyncSession(local);
        var settings = new BaseStorageSettings(session.Settings);
        session.Tick(ConfigSyncMode.Client);
        session.LoadLocalCategories(ConfigSyncFixtures.LocalText);
        session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);
        session.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.Client);

        ConfigSyncTick left = session.Tick(ConfigSyncMode.Menu);

        Assert.Equal(ConfigSyncTick.HostCleared, left);
        Assert.False(session.Settings.HasHost);
        Assert.Null(session.HostPeerId);
        Assert.True(settings.CraftFromBase);
        Assert.Equal(64, settings.MaxBaseStations);
        Assert.Equal(new[] { "Stone" }, session.Categories.CategoryNames);
        Assert.Equal(ConfigSyncTick.None, session.Tick(ConfigSyncMode.Menu));
    }

    // GW-U-1410 (T1, T3, T4): a client that goes to LocalWorld without a Menu tick clears the host and asks for the local file in one tick.
    [Fact]
    public void ClientToLocalWorldClearsAndReloadsInOneTick()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());
        session.Tick(ConfigSyncMode.Client);
        session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);
        session.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.Client);

        ConfigSyncTick tick = session.Tick(ConfigSyncMode.LocalWorld);

        Assert.Equal(ConfigSyncTick.HostCleared | ConfigSyncTick.ReloadLocalCategories, tick);
        Assert.False(session.Settings.HasHost);
        Assert.Null(session.HostPeerId);
        Assert.Equal(ConfigSyncMode.LocalWorld, session.Mode);
    }

    // GW-U-1410 (T3): ReloadLocalCategories is returned only on a transition into LocalWorld or Client.
    [Fact]
    public void ReloadIsAskedOnlyOnATransitionIntoLocalWorldOrClient()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());

        Assert.Equal(ConfigSyncTick.ReloadLocalCategories, session.Tick(ConfigSyncMode.LocalWorld));
        Assert.Equal(ConfigSyncTick.None, session.Tick(ConfigSyncMode.LocalWorld));
        Assert.Equal(ConfigSyncTick.None, session.Tick(ConfigSyncMode.Menu));
        Assert.Equal(ConfigSyncTick.ReloadLocalCategories, session.Tick(ConfigSyncMode.Client));
        Assert.Equal(ConfigSyncTick.None, session.Tick(ConfigSyncMode.Client));
    }

    // GW-U-1410 (2.1 "any" row): an undefined mode value counts as Menu.
    [Fact]
    public void AnUndefinedModeCountsAsMenu()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());
        session.Tick(ConfigSyncMode.Client);
        session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);
        session.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.Client);

        Assert.Equal(ConfigSyncTick.HostCleared, session.Tick((ConfigSyncMode)99));
        Assert.Equal(ConfigSyncMode.Menu, session.Mode);
    }

    // GW-U-1411 (4.2 LoadLocalCategories): the local text gives its catalog; while a host payload is applied the host catalog wins; after Menu the local catalog returns.
    [Fact]
    public void LocalCategoriesComeFromTheTextAndTheHostCatalogWinsWhileApplied()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());
        session.LoadLocalCategories(ConfigSyncFixtures.LocalText);
        Assert.Equal(new[] { "Stone" }, session.Categories.CategoryNames);

        session.Tick(ConfigSyncMode.Client);
        session.OnVersionOutcome(ConfigSyncFixtures.ClientAccept(), ConfigSyncMode.Client);
        session.Receive(1, ConfigSyncFixtures.H(), ConfigSyncMode.Client);
        session.LoadLocalCategories("Clay: Clay\n");
        Assert.Equal(new[] { "Wood", "Ores" }, session.Categories.CategoryNames);

        session.Tick(ConfigSyncMode.Menu);
        Assert.Equal(new[] { "Clay" }, session.Categories.CategoryNames);
    }

    // GW-U-1411: a null or malformed local text throws nothing and gives an empty local catalog.
    [Fact]
    public void NullAndMalformedLocalTextGiveAnEmptyLocalCatalog()
    {
        var session = new ConfigSyncSession(ConfigSyncFixtures.Local());

        session.LoadLocalCategories(null);
        Assert.Empty(session.Categories.CategoryNames);

        session.LoadLocalCategories("Bad line");
        Assert.Empty(session.Categories.CategoryNames);
    }
}
