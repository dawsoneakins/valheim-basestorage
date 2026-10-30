namespace BaseStorage.Core.Tests.HostSync;

using System;
using System.Linq;
using BaseStorage.Core.HostSync;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Settings;

/// <summary>
/// AC-6.2, spec section 7 (rule H1): the codec covers exactly the Host-synced rows of the section 7 table, checked against the
/// independent Section7Table copy and against SettingsCatalog, so a row added later with hostSynced true is synced without an edit here.
/// </summary>
public sealed class HostSyncCatalogCoverageTests
{
    // GW-U-1010 (section 7): the payload keys are exactly the spec's host-synced keys, in table order, independent of SettingsCatalog.
    [Fact]
    public void PayloadKeysAreExactlyTheSpecHostSyncedKeys()
    {
        HostSyncPayload defaults = HostSyncFixtures.Defaults();

        Assert.Equal(Section7Table.HostSyncedKeys, defaults.Settings.Select(pair => pair.Key).ToArray());
        Assert.Equal(Section7Table.Rows.Where(row => row.HostSynced).Select(row => row.Key).ToArray(), defaults.Settings.Select(pair => pair.Key).ToArray());
        Assert.Equal(9, defaults.Settings.Count);
    }

    // GW-U-1010 (section 7): the default text of every synced row equals the spec default.
    [Fact]
    public void PayloadDefaultsAreTheSpecDefaults()
    {
        HostSyncPayload defaults = HostSyncFixtures.Defaults();

        foreach (var row in Section7Table.Rows.Where(row => row.HostSynced))
        {
            Assert.Equal(row.DefaultText, defaults.ValueText(row.Key));
        }
    }

    // GW-U-1010 (H1): every SettingsCatalog.HostSynced row is captured from the source and survives a round trip with a changed value.
    [Fact]
    public void EveryCatalogHostSyncedRowRoundTripsWithAChangedValue()
    {
        HostSyncPayload changed = HostSyncFixtures.Changed();
        HostSyncPayload? decoded = HostSyncCodec.Decode(HostSyncCodec.Encode(changed));

        Assert.NotNull(decoded);
        foreach (SettingDefinition row in SettingsCatalog.HostSynced)
        {
            Assert.NotEqual(row.DefaultText, decoded!.ValueText(row.Key));
            Assert.Equal(changed.ValueText(row.Key), decoded.ValueText(row.Key));
        }
    }

    // GW-U-1010 (H1): every synced row is answered by the layered view with the host's changed value through the typed read path.
    [Fact]
    public void EveryCatalogHostSyncedRowIsAnsweredFromTheHost()
    {
        var view = new LayeredSettingsSource(new FakeSettingsSource());
        var settings = new BaseStorageSettings(view);
        view.ApplyHost(HostSyncFixtures.Changed());

        foreach (SettingDefinition row in SettingsCatalog.HostSynced)
        {
            string actual = row.Kind switch
            {
                SettingKind.Bool => settings.GetBool(row.Key) ? "true" : "false",
                SettingKind.Int => settings.GetInt(row.Key).ToString(System.Globalization.CultureInfo.InvariantCulture),
                SettingKind.ItemList => ItemListText.Format(settings.GetItemList(row.Key)),
                SettingKind.Hotkey => settings.GetHotkeyText(row.Key),
                _ => throw new InvalidOperationException("Unknown kind " + row.Kind),
            };

            Assert.NotEqual(row.DefaultText, actual);
            Assert.Equal(view.Host!.ValueText(row.Key), actual);
        }
    }

    // GW-U-1010 (H1): no non-synced row is answered from the host, whatever the local source holds.
    [Fact]
    public void NoNonSyncedRowIsAnsweredFromTheHost()
    {
        var local = new FakeSettingsSource();
        var view = new LayeredSettingsSource(local);
        view.ApplyHost(HostSyncFixtures.Changed());

        foreach (SettingDefinition row in SettingsCatalog.All.Where(row => !row.HostSynced))
        {
            Assert.False(view.TryGetBool(row.Key, out _));
            Assert.False(view.TryGetInt(row.Key, out _));
            Assert.False(view.TryGetText(row.Key, out _));
            Assert.False(view.Host!.TryGetValueText(row.Key, out _));
        }

        Assert.Equal(3 * SettingsCatalog.All.Count(row => !row.HostSynced), local.BoolCalls + local.IntCalls + local.TextCalls);
    }

    // GW-U-1010 (H1): the catalog's HostSynced list is the only source: its count and keys match the All rows flagged HostSynced.
    [Fact]
    public void CatalogHostSyncedListMatchesTheFlaggedRows()
    {
        Assert.Equal(SettingsCatalog.All.Where(row => row.HostSynced).Select(row => row.Key), SettingsCatalog.HostSynced.Select(row => row.Key));
        Assert.All(SettingsCatalog.HostSynced, row => Assert.True(row.Kind == SettingKind.Bool || row.Kind == SettingKind.Int || row.Kind == SettingKind.ItemList || row.Kind == SettingKind.Hotkey));
    }
}
