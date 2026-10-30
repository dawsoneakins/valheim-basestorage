namespace BaseStorage.Core.Tests.Settings;

using BaseStorage.Core.HostSync;
using BaseStorage.Core.Settings;

/// <summary>
/// GW-U-1811 to GW-U-1813 (AC-3, AC-4; contract craft-from-base-toggle 6, rules L1-L4). Green on arrival: the layered view and
/// the catalog of S-E006-004 already give these answers, so these tests characterise existing behaviour (ADR-GWIN-137).
/// </summary>
public sealed class CraftFromBaseLiveTests
{
    // GW-U-1811 (AC-4, L4): one BaseStorageSettings over one source reads true, false, then true, with no new instance.
    [Fact]
    public void TheSameSettingsInstanceFollowsTrueFalseTrueWithoutANewObject()
    {
        FakeSettingsSource source = new FakeSettingsSource().WithBool(SettingKeys.CraftFromBase, true);
        BaseStorageSettings settings = new(source);
        Assert.True(settings.CraftFromBase);

        source.WithBool(SettingKeys.CraftFromBase, false);
        Assert.False(settings.CraftFromBase);

        source.WithBool(SettingKeys.CraftFromBase, true);
        Assert.True(settings.CraftFromBase);
    }

    // GW-U-1812 (AC-3, L2, L3, NFR security): the host's false wins over a local true, also after a local change, and local applies again after the host is cleared.
    [Fact]
    public void AHostFalseWinsOverALocalTrueUntilTheHostIsCleared()
    {
        FakeSettingsSource local = new FakeSettingsSource().WithBool(SettingKeys.CraftFromBase, true);
        HostSyncPayload host = HostSyncPayload.Capture(new FakeSettingsSource().WithBool(SettingKeys.CraftFromBase, false), string.Empty);
        LayeredSettingsSource layered = new(local);
        layered.TryApplyHost(HostSyncCodec.EncodeBytes(host));
        BaseStorageSettings settings = new(layered);
        Assert.False(settings.CraftFromBase);

        local.WithBool(SettingKeys.CraftFromBase, true);
        Assert.False(settings.CraftFromBase);

        local.WithBool(SettingKeys.CraftFromBase, false);
        layered.ClearHost();
        Assert.False(settings.CraftFromBase);

        local.WithBool(SettingKeys.CraftFromBase, true);
        Assert.True(settings.CraftFromBase);
    }

    // GW-U-1813 (AC-3, L1): the catalog row is General, Bool, default "true", and host-synced.
    [Fact]
    public void TheCatalogRowIsGeneralBoolDefaultTrueAndHostSynced()
    {
        SettingDefinition row = SettingsCatalog.Get(SettingKeys.CraftFromBase);

        Assert.Equal(SettingSections.General, row.Section);
        Assert.Equal(SettingKind.Bool, row.Kind);
        Assert.Equal("true", row.DefaultText);
        Assert.True(row.HostSynced);
        Assert.Contains(row, SettingsCatalog.HostSynced);
    }
}
