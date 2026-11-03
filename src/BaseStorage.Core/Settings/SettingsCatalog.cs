namespace BaseStorage.Core.Settings;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>The fixed catalog of the 21 section 7 settings, in table order. Immutable after static initialisation.</summary>
public static class SettingsCatalog
{
    private static readonly SettingDefinition[] Rows =
    {
        new SettingDefinition(SettingSections.General, SettingKeys.CraftFromBase, SettingKind.Bool, "true", true,
            "Craft and build using materials from chests in your base. Host-synced: in multiplayer the host's value is used."),
        new SettingDefinition(SettingSections.General, SettingKeys.ExcludeVehicles, SettingKind.Bool, "true", true,
            "Ignore containers on carts and ships when collecting the base's chests. Host-synced: in multiplayer the host's value is used."),
        new SettingDefinition(SettingSections.General, SettingKeys.KeepChestsCraftable, SettingKind.Bool, "true", true,
            "Allow crafting to use materials from chests marked Keep. Host-synced: in multiplayer the host's value is used."),
        new SettingDefinition(SettingSections.General, SettingKeys.MaxBaseStations, SettingKind.Int, "64", true,
            "Maximum number of crafting stations joined into one base. Host-synced: in multiplayer the host's value is used."),
        new SettingDefinition(SettingSections.General, SettingKeys.BaseCacheSeconds, SettingKind.Int, "2", false,
            "Seconds a resolved base is reused before it is resolved again."),
        new SettingDefinition(SettingSections.General, SettingKeys.RequireSameVersion, SettingKind.Bool, "true", true,
            "Refuse players whose BaseStorage version differs from the host's. Host-synced: in multiplayer the host's value is used."),
        new SettingDefinition(SettingSections.Organize, SettingKeys.AutoAssignThresholdPct, SettingKind.Int, "50", true,
            "Percent of a chest's stacks that must share a category before Organize assigns that category. Host-synced: in multiplayer the host's value is used."),
        new SettingDefinition(SettingSections.Organize, SettingKeys.OrganizeSkipPreview, SettingKind.Bool, "false", false,
            "Start Organize without showing the preview first."),
        new SettingDefinition(SettingSections.Organize, SettingKeys.DepositPreview, SettingKind.Bool, "false", false,
            "Show a preview before Put away inventory moves anything."),
        new SettingDefinition(SettingSections.Organize, SettingKeys.DepositNeverItems, SettingKind.ItemList, "@mead,@ammo,Hammer,Hoe,Cultivator,FishingRod,@bait", false,
            "Items Put away inventory never moves. Comma-separated prefab names or tokens: @mead, @ammo (arrows and bolts), @bait. Star an item as a favourite to keep it on you."),
        new SettingDefinition(SettingSections.Organize, SettingKeys.MovesPerFrame, SettingKind.Int, "20", false,
            "Maximum item moves per frame while Organize or Put away inventory runs."),
        new SettingDefinition(SettingSections.Organize, SettingKeys.OrganizeLockSeconds, SettingKind.Int, "30", true,
            "Seconds before an abandoned Organize lock on a base expires. Host-synced: in multiplayer the host's value is used."),
        new SettingDefinition(SettingSections.Network, SettingKeys.ReserveTimeoutMs, SettingKind.Int, "1500", true,
            "Milliseconds to wait for a chest reservation before giving up. Host-synced: in multiplayer the host's value is used."),
        new SettingDefinition(SettingSections.General, SettingKeys.StationButtons, SettingKind.Bool, "true", false,
            "Show the Organize base and Put away inventory buttons on crafting station panels."),
        new SettingDefinition(SettingSections.Keys, SettingKeys.CycleCategoryKey, SettingKind.Hotkey, "", false,
            "Hotkey that cycles the category of the chest you are looking at. Unbound by default."),
        new SettingDefinition(SettingSections.Keys, SettingKeys.OrganizeKey, SettingKind.Hotkey, "", false,
            "Hotkey that starts Organize base for the base that contains the player. Unbound by default."),
        new SettingDefinition(SettingSections.Keys, SettingKeys.DepositKey, SettingKind.Hotkey, "", false,
            "Hotkey that starts Put away inventory at the nearest crafting station. Unbound by default."),
        new SettingDefinition(SettingSections.Debug, SettingKeys.DebugLogging, SettingKind.Bool, "false", false,
            "Write per-move detail to the log at Debug level."),
        new SettingDefinition(SettingSections.Organize, SettingKeys.AutoAssignMixedChests, SettingKind.Bool, "true", true,
            "Let Organize give mixed chests a category that is short of space, and move stacks with no room of their own to the nearest related chest. Set to false for the 0.1.0 behaviour. Host-synced: in multiplayer the host's value is used."),
        new SettingDefinition(SettingSections.General, SettingKeys.ShowCategoryTooltip, SettingKind.Bool, "true", false,
            "Show each item's BaseStorage category on its tooltip."),
        new SettingDefinition(SettingSections.Keys, SettingKeys.RestockKey, SettingKind.Hotkey, "", false,
            "Hotkey that starts Restock for the base that contains the player. Unbound by default."),
        new SettingDefinition(SettingSections.General, SettingKeys.ChestLabels, SettingKind.Bool, "true", false,
            "Show chest names and categories as labels in the world, inside a base."),
        new SettingDefinition(SettingSections.General, SettingKeys.ChestLabelDistance, SettingKind.Int, "6", false,
            "How close, in metres, a chest must be for its label to show."),
    };

    /// <summary>All 21 entries, in section 7 table order.</summary>
    public static IReadOnlyList<SettingDefinition> All { get; } = new ReadOnlyCollection<SettingDefinition>(Rows);

    /// <summary>The 9 entries whose host-synced flag is true, in table order.</summary>
    public static IReadOnlyList<SettingDefinition> HostSynced { get; } = new ReadOnlyCollection<SettingDefinition>(Array.FindAll(Rows, r => r.HostSynced));

    /// <summary>The parsed default of DepositNeverItems, with 7 entries.</summary>
    public static IReadOnlyList<string> DepositNeverItemsDefault { get; } = ItemListText.Parse(Array.Find(Rows, r => r.Key == SettingKeys.DepositNeverItems)!.DefaultText);

    /// <summary>Finds the entry for a key. The key comparison is ordinal and case-sensitive.</summary>
    /// <param name="key">The key to find.</param>
    /// <returns>The entry, or null when no catalog key matches.</returns>
    /// <exception cref="ArgumentNullException">The key is null.</exception>
    public static SettingDefinition? Find(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        foreach (SettingDefinition row in Rows)
        {
            if (string.Equals(row.Key, key, StringComparison.Ordinal))
            {
                return row;
            }
        }

        return null;
    }

    /// <summary>Gets the entry for a key that must exist in the catalog.</summary>
    /// <param name="key">The key to get.</param>
    /// <returns>The entry for the key.</returns>
    /// <exception cref="ArgumentNullException">The key is null.</exception>
    /// <exception cref="ArgumentException">The key is not in the catalog.</exception>
    public static SettingDefinition Get(string key)
    {
        return Find(key) ?? throw new ArgumentException("Unknown setting key.", nameof(key));
    }
}
