namespace BaseStorage.Core.Tests.Settings;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Settings;

/// <summary>
/// AC-1, AC-3, AC-4, AC-5, AC-6, AC-8 (contract 2.4, ADR-GWIN-015): the catalog matches the independent section 7 copy
/// row by row. Expected values are literals from <see cref="Section7Table"/>, never read from Core.
/// </summary>
public sealed class SettingsCatalogTableTests
{
    // GW-U-092 (AC-4): a failure names the row index and the key, because each row is compared as one labelled string.
    [Theory]
    [MemberData(nameof(Section7Table.IndexData), MemberType = typeof(Section7Table))]
    public void CatalogRowMatchesTheSection7TableRow(int index)
    {
        var expected = Section7Table.Rows[index];
        SettingDefinition actual = SettingsCatalog.All[index];

        Assert.Equal(
            Describe(index, expected.Section, expected.Key, expected.Kind, expected.DefaultText, expected.HostSynced),
            Describe(index, actual.Section, actual.Key, actual.Kind, actual.DefaultText, actual.HostSynced));
    }

    // GW-U-092 (AC-1): section and kind counts, as one labelled list each.
    [Fact]
    public void SectionAndKindCountsMatchSection7()
    {
        Assert.Equal(20, SettingsCatalog.All.Count);

        string[] sections = SettingsCatalog.All
            .GroupBy(d => d.Section, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => g.Key + "=" + g.Count().ToString(CultureInfo.InvariantCulture))
            .ToArray();
        Assert.Equal(new[] { "Debug=1", "General=8", "Keys=3", "Network=1", "Organize=7" }, sections);

        string[] kinds = SettingsCatalog.All
            .GroupBy(d => d.Kind)
            .OrderBy(g => (int)g.Key)
            .Select(g => g.Key.ToString() + "=" + g.Count().ToString(CultureInfo.InvariantCulture))
            .ToArray();
        Assert.Equal(new[] { "Bool=10", "Int=6", "ItemList=1", "Hotkey=3" }, kinds);
    }

    // GW-U-093 (AC-3): 19 distinct keys, equal to the SettingKeys constants and to the literal table.
    [Fact]
    public void CatalogListsTheNineteenKeysOnceAndMatchesTheSettingKeysConstants()
    {
        string[] catalogKeys = SettingsCatalog.All.Select(d => d.Key).ToArray();
        Assert.Equal(20, catalogKeys.Length);
        Assert.Equal(20, catalogKeys.Distinct(StringComparer.Ordinal).Count());

        string[] constantValues = ConstantValues(typeof(SettingKeys));
        Assert.Equal(Sorted(constantValues), Sorted(catalogKeys));
        Assert.Equal(Sorted(Section7Table.Rows.Select(r => r.Key)), Sorted(catalogKeys));

        string[] sectionNames = ConstantValues(typeof(SettingSections));
        Assert.Equal(5, sectionNames.Length);
        foreach (SettingDefinition definition in SettingsCatalog.All)
        {
            Assert.True(
                sectionNames.Contains(definition.Section, StringComparer.Ordinal),
                $"Key {definition.Key} has section {definition.Section}, which is not one of the SettingSections values.");
        }
    }

    // GW-U-094 (AC-4): no missing and no extra keys, each difference reported by name.
    [Fact]
    public void Section7TableAndCatalogHaveNoMissingAndNoExtraKeys()
    {
        string[] tableKeys = Section7Table.Rows.Select(r => r.Key).ToArray();
        string[] catalogKeys = SettingsCatalog.All.Select(d => d.Key).ToArray();

        string[] missing = tableKeys.Except(catalogKeys, StringComparer.Ordinal).ToArray();
        string[] extra = catalogKeys.Except(tableKeys, StringComparer.Ordinal).ToArray();

        Assert.True(missing.Length == 0, "Keys in section 7 but not in the catalog: " + string.Join(", ", missing));
        Assert.True(extra.Length == 0, "Keys in the catalog but not in section 7: " + string.Join(", ", extra));
    }

    // GW-U-095 (AC-5): the hotkeys are exactly the three Keys-section entries, unbound and local-only.
    [Fact]
    public void HotkeysAreExactlyTheThreeKeysSectionEntriesAndAllUnbound()
    {
        SettingDefinition[] hotkeys = SettingsCatalog.All.Where(d => d.Kind == SettingKind.Hotkey).ToArray();

        Assert.Equal(new[] { "CycleCategoryKey", "OrganizeKey", "DepositKey" }, hotkeys.Select(d => d.Key));
        foreach (SettingDefinition hotkey in hotkeys)
        {
            Assert.Equal(SettingSections.Keys, hotkey.Section);
            Assert.Equal(string.Empty, hotkey.DefaultText);
            Assert.False(hotkey.HostSynced, hotkey.Key + " must not be host-synced.");
        }

        var settings = new BaseStorageSettings(new FakeSettingsSource());
        Assert.Equal(string.Empty, settings.CycleCategoryKey);
        Assert.Equal(string.Empty, settings.OrganizeKey);
        Assert.Equal(string.Empty, settings.DepositKey);
    }

    // GW-U-096 (AC-6): no other hotkey, and no hotkey default is bound. Enum values are contracted.
    [Fact]
    public void ExactlyThreeHotkeyEntriesAndNoHotkeyOutsideTheKeysSection()
    {
        Assert.Equal(3, SettingsCatalog.All.Count(d => d.Kind == SettingKind.Hotkey));
        foreach (SettingDefinition definition in SettingsCatalog.All.Where(d => d.Section != SettingSections.Keys))
        {
            Assert.NotEqual(SettingKind.Hotkey, definition.Kind);
        }

        foreach (SettingDefinition hotkey in SettingsCatalog.All.Where(d => d.Kind == SettingKind.Hotkey))
        {
            Assert.Equal(string.Empty, hotkey.DefaultText);
        }

        Assert.Equal(0, (int)SettingKind.Bool);
        Assert.Equal(1, (int)SettingKind.Int);
        Assert.Equal(2, (int)SettingKind.ItemList);
        Assert.Equal(3, (int)SettingKind.Hotkey);
        Assert.Equal(4, Enum.GetValues(typeof(SettingKind)).Length);
    }

    // GW-U-097 (AC-8): the host-synced set, in order, and its identity with the filtered catalog.
    [Fact]
    public void HostSyncedSetIsTheNineSection7KeysInOrderAndSharesInstances()
    {
        string[] hostSyncedKeys = SettingsCatalog.HostSynced.Select(d => d.Key).ToArray();
        Assert.Equal(Section7Table.HostSyncedKeys, hostSyncedKeys);

        SettingDefinition[] filtered = SettingsCatalog.All.Where(d => d.HostSynced).ToArray();
        Assert.Equal(filtered.Length, SettingsCatalog.HostSynced.Count);
        for (int i = 0; i < filtered.Length; i++)
        {
            Assert.Same(filtered[i], SettingsCatalog.HostSynced[i]);
        }

        Assert.Equal(11, SettingsCatalog.All.Count(d => !d.HostSynced));
    }

    private static string Describe(int index, string section, string key, SettingKind kind, string defaultText, bool hostSynced) =>
        string.Join(
            "|",
            index.ToString(CultureInfo.InvariantCulture),
            section,
            key,
            kind.ToString(),
            "\"" + defaultText + "\"",
            hostSynced ? "true" : "false");

    private static string[] ConstantValues(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();

    private static string[] Sorted(IEnumerable<string> values) =>
        values.OrderBy(value => value, StringComparer.Ordinal).ToArray();
}
