namespace BaseStorage.Core.Tests.Settings;

using BaseStorage.Core.Settings;

/// <summary>
/// S-A-001 (AC-7; mixed-chest-category-contracts.md 5.1 and 5.2): the AutoAssignMixedChests catalog row, its host-sync membership and
/// its typed read with the default true. Expected values are literals from section 5.
/// </summary>
public sealed class AutoAssignMixedChestsSettingTests
{
    private const string Description =
        "Let Organize give mixed chests below the threshold a category that is short of space. Set to false to keep them as unassigned sources, as in 0.1.0. Host-synced: in multiplayer the host's value is used.";

    // GW-U-2430 (AC-7, 5.1): the catalog row is the last row, host-synced, default true, with the exact 201-character description.
    [Fact]
    public void TheCatalogRowIsTheLastRowHostSyncedWithTheExactDescription()
    {
        Assert.Equal(20, SettingsCatalog.All.Count);

        SettingDefinition row = SettingsCatalog.All[18];
        Assert.Equal(SettingSections.Organize, row.Section);
        Assert.Equal(SettingKeys.AutoAssignMixedChests, row.Key);
        Assert.Equal(SettingKind.Bool, row.Kind);
        Assert.Equal("true", row.DefaultText);
        Assert.True(row.HostSynced);
        Assert.Equal(Description, row.Description);
        Assert.Equal(201, row.Description.Length);
        Assert.Same(row, SettingsCatalog.Get("AutoAssignMixedChests"));
    }

    // GW-U-2430 (AC-7, 5.1, 5.2): the host-synced list ends with the new key, and the lookup is case-sensitive ordinal.
    [Fact]
    public void TheHostSyncedListEndsWithTheNewKeyAndTheLookupIsOrdinal()
    {
        Assert.Equal(9, SettingsCatalog.HostSynced.Count);
        Assert.Equal("AutoAssignMixedChests", SettingsCatalog.HostSynced[8].Key);
        Assert.Null(SettingsCatalog.Find("autoassignmixedchests"));
    }

    // GW-U-2431 (AC-7, 5.2): the typed read is true with no stored value, false when stored false, and the key constant is its name.
    [Fact]
    public void TheTypedReadDefaultsToTrueAndFollowsTheStoredValue()
    {
        Assert.Equal("AutoAssignMixedChests", SettingKeys.AutoAssignMixedChests);

        Assert.True(new BaseStorageSettings(new FakeSettingsSource()).AutoAssignMixedChests);
        Assert.False(new BaseStorageSettings(new FakeSettingsSource().WithBool("AutoAssignMixedChests", false)).AutoAssignMixedChests);
    }
}
