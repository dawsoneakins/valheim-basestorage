namespace BaseStorage.Core.Tests.Settings;

using System.Collections.Generic;
using BaseStorage.Core.Settings;

/// <summary>
/// An independent literal copy of contract table 2.4 (settings-contract.md v1) and spec section 7 (ADR-GWIN-015).
/// The values are typed here and are never read from SettingsCatalog, SettingKeys or SettingSections.
/// Not a test class.
/// </summary>
internal static class Section7Table
{
    /// <summary>The 20 rows in table order, including StationButtons at index 13 and AutoAssignMixedChests at index 18 (S-A-001).</summary>
    public static readonly (string Section, string Key, SettingKind Kind, string DefaultText, bool HostSynced)[] Rows =
    {
        ("General", "CraftFromBase", SettingKind.Bool, "true", true),
        ("General", "ExcludeVehicles", SettingKind.Bool, "true", true),
        ("General", "KeepChestsCraftable", SettingKind.Bool, "true", true),
        ("General", "MaxBaseStations", SettingKind.Int, "64", true),
        ("General", "BaseCacheSeconds", SettingKind.Int, "2", false),
        ("General", "RequireSameVersion", SettingKind.Bool, "true", true),
        ("Organize", "AutoAssignThresholdPct", SettingKind.Int, "50", true),
        ("Organize", "OrganizeSkipPreview", SettingKind.Bool, "false", false),
        ("Organize", "DepositPreview", SettingKind.Bool, "false", false),
        ("Organize", "DepositNeverItems", SettingKind.ItemList, "@mead,@ammo,Hammer,Hoe,Cultivator,FishingRod,@bait", false),
        ("Organize", "MovesPerFrame", SettingKind.Int, "20", false),
        ("Organize", "OrganizeLockSeconds", SettingKind.Int, "30", true),
        ("Network", "ReserveTimeoutMs", SettingKind.Int, "1500", true),
        ("General", "StationButtons", SettingKind.Bool, "true", false),
        ("Keys", "CycleCategoryKey", SettingKind.Hotkey, "", false),
        ("Keys", "OrganizeKey", SettingKind.Hotkey, "", false),
        ("Keys", "DepositKey", SettingKind.Hotkey, "", false),
        ("Debug", "DebugLogging", SettingKind.Bool, "false", false),
        ("Organize", "AutoAssignMixedChests", SettingKind.Bool, "true", true),
        ("General", "ShowCategoryTooltip", SettingKind.Bool, "true", false),
    };

    /// <summary>The 9 host-synced keys, in table order.</summary>
    public static readonly string[] HostSyncedKeys =
    {
        "CraftFromBase",
        "ExcludeVehicles",
        "KeepChestsCraftable",
        "MaxBaseStations",
        "RequireSameVersion",
        "AutoAssignThresholdPct",
        "OrganizeLockSeconds",
        "ReserveTimeoutMs",
        "AutoAssignMixedChests",
    };

    /// <summary>The row index of a key in <see cref="Rows"/>, found by ordinal comparison of literal keys.</summary>
    public static int IndexOf(string key)
    {
        for (int i = 0; i < Rows.Length; i++)
        {
            if (Rows[i].Key == key)
            {
                return i;
            }
        }

        throw new KeyNotFoundException("Section7Table has no key " + key);
    }

    /// <summary>The row for a key, from the literal table.</summary>
    public static (string Section, string Key, SettingKind Kind, string DefaultText, bool HostSynced) Row(string key) =>
        Rows[IndexOf(key)];

    /// <summary>The row indices 0..18, for theories over every row.</summary>
    public static IEnumerable<object[]> IndexData()
    {
        for (int i = 0; i < Rows.Length; i++)
        {
            yield return new object[] { i };
        }
    }
}
