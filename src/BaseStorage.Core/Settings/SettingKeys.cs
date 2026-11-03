namespace BaseStorage.Core.Settings;

/// <summary>The 19 setting keys of the settings catalog. Each value equals its constant name.</summary>
public static class SettingKeys
{
    /// <summary>Craft and build using materials from chests in the base.</summary>
    public const string CraftFromBase = "CraftFromBase";

    /// <summary>Ignore containers on carts and ships when collecting the base's chests.</summary>
    public const string ExcludeVehicles = "ExcludeVehicles";

    /// <summary>Allow crafting to use materials from chests marked Keep.</summary>
    public const string KeepChestsCraftable = "KeepChestsCraftable";

    /// <summary>Maximum number of crafting stations joined into one base.</summary>
    public const string MaxBaseStations = "MaxBaseStations";

    /// <summary>Seconds a resolved base is reused before it is resolved again.</summary>
    public const string BaseCacheSeconds = "BaseCacheSeconds";

    /// <summary>Refuse players whose version differs from the host's.</summary>
    public const string RequireSameVersion = "RequireSameVersion";

    /// <summary>Percent of a chest's stacks that must share a category before Organize assigns it.</summary>
    public const string AutoAssignThresholdPct = "AutoAssignThresholdPct";

    /// <summary>Start Organize without showing the preview first.</summary>
    public const string OrganizeSkipPreview = "OrganizeSkipPreview";

    /// <summary>Show a preview before Put away inventory moves anything.</summary>
    public const string DepositPreview = "DepositPreview";

    /// <summary>Items that Put away inventory never moves.</summary>
    public const string DepositNeverItems = "DepositNeverItems";

    /// <summary>Maximum item moves per frame while Organize or Put away inventory runs.</summary>
    public const string MovesPerFrame = "MovesPerFrame";

    /// <summary>Seconds before an abandoned Organize lock on a base expires.</summary>
    public const string OrganizeLockSeconds = "OrganizeLockSeconds";

    /// <summary>Milliseconds to wait for a chest reservation before giving up.</summary>
    public const string ReserveTimeoutMs = "ReserveTimeoutMs";

    /// <summary>Show the Organize base and Put away inventory buttons on crafting station panels.</summary>
    public const string StationButtons = "StationButtons";

    /// <summary>The key of the setting that adds the category line to item tooltips (S-B-001).</summary>
    public const string ShowCategoryTooltip = "ShowCategoryTooltip";

    /// <summary>Hotkey that cycles the category of the chest being looked at.</summary>
    public const string CycleCategoryKey = "CycleCategoryKey";

    /// <summary>Hotkey that starts Organize base for the base that contains the player.</summary>
    public const string OrganizeKey = "OrganizeKey";

    /// <summary>Hotkey that starts Put away inventory at the nearest crafting station.</summary>
    public const string DepositKey = "DepositKey";

    /// <summary>Write per-move detail to the log at Debug level.</summary>
    public const string DebugLogging = "DebugLogging";

    /// <summary>Let Organize give mixed chests below the threshold a category that is short of space.</summary>
    public const string AutoAssignMixedChests = "AutoAssignMixedChests";

    /// <summary>Shows chest names and categories as labels in the world.</summary>
    public const string ChestLabels = "ChestLabels";

    /// <summary>How close, in metres, a chest must be for its label to show.</summary>
    public const string ChestLabelDistance = "ChestLabelDistance";

    /// <summary>Hotkey that starts Restock for the base that contains the player.</summary>
    public const string RestockKey = "RestockKey";
}
