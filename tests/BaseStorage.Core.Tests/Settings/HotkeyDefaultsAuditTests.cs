namespace BaseStorage.Core.Tests.Settings;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Settings;

/// <summary>
/// AC-1, AC-2 (ADR-GWIN-154, contract hotkey-hover-audit-contracts v1 section 1, HK1 to HK4): every hotkey in the product's own catalog
/// is unbound by default and sits in the Keys section, and every key-binding name is a Hotkey. The KeyboardShortcut half of the audit
/// (each hotkey is bound as a BepInEx ConfigEntry of KeyboardShortcut with KeyCode.None, and no other KeyboardShortcut exists) is the
/// Plugin side. It is pinned by the existing PluginSettingsSourceScanTests (KeyboardShortcutEmptyIsTheUnboundDefaultOfTheHotkeyBranchOnly,
/// NoKeyIsBoundAnywhereAndTheHotkeyEntryTypeIsOnlyInTheBindingFile, KeyboardShortcutIsNamedOnlyInTheBindingFile). Those are reused, not copied.
/// </summary>
public sealed class HotkeyDefaultsAuditTests
{
    // GW-U-2200 (AC-1): every catalog hotkey has an empty default (unbound) and sits in the Keys section.
    [Fact]
    public void EveryCatalogHotkeyIsInTheKeysSectionAndUnboundByDefault()
    {
        IReadOnlyList<string> violations = Violations(CatalogRows(), KeyBindingNames());

        Assert.True(violations.Count == 0, "Hotkey audit violations: " + Describe(violations));
    }

    // GW-U-2201 (AC-1): the hotkey set comes from the catalog and holds the three known hotkeys; the exact count is not pinned.
    [Fact]
    public void TheHotkeySetComesFromTheCatalogAndContainsTheThreeKnownHotkeys()
    {
        string[] hotkeys = HotkeysFromCatalog();
        string[] known = { SettingKeys.CycleCategoryKey, SettingKeys.OrganizeKey, SettingKeys.DepositKey, "RestockKey" }; // Edited by S-QR-002 (ADR-GWIN-176)

        Assert.NotEmpty(hotkeys);
        Assert.Empty(known.Except(hotkeys, StringComparer.Ordinal));
    }

    // GW-U-2202 (AC-2): every ...Key binding name is a Hotkey, and every Keys-section row is a Hotkey. The KeyboardShortcut half is the Plugin scans.
    [Fact]
    public void EveryKeyBindingNameAndEveryKeysSectionEntryIsAHotkey()
    {
        var notHotkey = new List<string>();
        foreach (string name in KeyBindingNames())
        {
            SettingDefinition? row = SettingsCatalog.All.FirstOrDefault(d => string.Equals(d.Key, name, StringComparison.Ordinal));
            if (row is null)
            {
                notHotkey.Add(name + ": key-binding name has no catalog row");
            }
            else if (row.Kind != SettingKind.Hotkey)
            {
                notHotkey.Add(name + ": key-binding name is " + row.Kind + ", not Hotkey");
            }
        }

        foreach (SettingDefinition row in SettingsCatalog.All)
        {
            if (string.Equals(row.Section, SettingSections.Keys, StringComparison.Ordinal) && row.Kind != SettingKind.Hotkey)
            {
                notHotkey.Add(row.Key + ": section Keys holds a " + row.Kind + ", not a Hotkey");
            }
        }

        Assert.True(notHotkey.Count == 0, "Key-binding audit violations: " + Describe(notHotkey));
    }

    // GW-U-2203 (AC-1): BaseStorageSettings reads every hotkey as empty text (unbound) over an empty source.
    [Fact]
    public void BaseStorageSettingsReadsEveryHotkeyAsEmptyOverAnEmptySource()
    {
        var settings = new BaseStorageSettings(new FakeSettingsSource());
        var notEmpty = new List<string>();
        foreach (string key in HotkeysFromCatalog())
        {
            string text = settings.GetHotkeyText(key);
            if (text.Length != 0)
            {
                notEmpty.Add(key + " reads '" + text + "'");
            }
        }

        Assert.True(notEmpty.Count == 0, "Hotkeys that are not unbound over an empty source: " + Describe(notEmpty));
    }

    // GW-U-2204 (AC-2): the fail path. A bound default and a key-binding name of another kind are both reported, and nothing else is.
    [Fact]
    public void TheAuditReportsABoundDefaultAndAKeyBindingOfAnotherKind()
    {
        var rows = new (string Section, string Key, SettingKind Kind, string DefaultText)[]
        {
            ("Keys", "SortKey", SettingKind.Hotkey, "F7"),
            ("General", "QuickSortKey", SettingKind.Int, "0"),
        };

        IReadOnlyList<string> violations = Violations(rows, new[] { "QuickSortKey" });

        Assert.True(violations.Count == 2, "Expected exactly two violations but got: " + Describe(violations));
        Assert.Single(violations, v => v.StartsWith("SortKey:", StringComparison.Ordinal));
        Assert.Single(violations, v => v.StartsWith("QuickSortKey:", StringComparison.Ordinal));
    }

    // The shared rule set (ADR-GWIN-154). Each row is one catalog row as (section, key, kind, default text); keyNames are the ...Key binding names.
    private static IReadOnlyList<string> Violations(
        IEnumerable<(string Section, string Key, SettingKind Kind, string DefaultText)> rows,
        IEnumerable<string> keyNames)
    {
        var found = new List<string>();
        var rowList = rows.ToList();
        foreach ((string section, string key, SettingKind kind, string defaultText) in rowList)
        {
            if (kind == SettingKind.Hotkey && defaultText.Length != 0)
            {
                found.Add(key + ": default '" + defaultText + "' is bound, a hotkey must default to unbound");
            }

            if (kind == SettingKind.Hotkey && !string.Equals(section, SettingSections.Keys, StringComparison.Ordinal))
            {
                found.Add(key + ": hotkey is in section " + section + ", not " + SettingSections.Keys);
            }

            if (string.Equals(section, SettingSections.Keys, StringComparison.Ordinal) && kind != SettingKind.Hotkey)
            {
                found.Add(key + ": section " + SettingSections.Keys + " holds a " + kind + ", not a Hotkey");
            }
        }

        foreach (string name in keyNames)
        {
            var matches = rowList.Where(r => string.Equals(r.Key, name, StringComparison.Ordinal)).ToList();
            if (matches.Count == 0)
            {
                found.Add(name + ": key-binding name has no catalog row");
            }

            foreach (var match in matches)
            {
                if (match.Kind != SettingKind.Hotkey)
                {
                    found.Add(name + ": key-binding name is " + match.Kind + ", not Hotkey");
                }
            }
        }

        return found;
    }

    private static IEnumerable<(string Section, string Key, SettingKind Kind, string DefaultText)> CatalogRows() =>
        SettingsCatalog.All.Select(d => (d.Section, d.Key, d.Kind, d.DefaultText));

    private static string[] HotkeysFromCatalog() =>
        SettingsCatalog.All.Where(d => d.Kind == SettingKind.Hotkey).Select(d => d.Key).ToArray();

    // The ...Key constants of SettingKeys, read by reflection (public const string fields whose name ends in Key).
    private static string[] KeyBindingNames() =>
        typeof(SettingKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string) && f.Name.EndsWith("Key", StringComparison.Ordinal))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();

    private static string Describe(IEnumerable<string> items) => string.Join("; ", items);
}
