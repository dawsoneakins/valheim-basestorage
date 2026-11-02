namespace BaseStorage.Core.Tests.Settings;

using System;
using BaseStorage.Core.Settings;

/// <summary>
/// AC-2 (contract 3, rules D1 to D4): every catalog description is one trimmed line of at most 240 characters, with the
/// host-synced suffix exactly on the host-synced rows. The recommended wording is not asserted (ADR-GWIN-015).
/// </summary>
public sealed class SettingDescriptionTests
{
    private const string HostSuffix = " Host-synced: in multiplayer the host's value is used.";
    private const int MaxLength = 240;

    // GW-U-099 (D1, D2, D3)
    [Theory]
    [MemberData(nameof(Section7Table.IndexData), MemberType = typeof(Section7Table))]
    public void DescriptionIsOneTrimmedLineAndCarriesTheHostSuffixExactlyWhenHostSynced(int index)
    {
        var row = Section7Table.Rows[index];
        string description = SettingsCatalog.Get(row.Key).Description;

        Assert.True(description.Length > 0, $"Row {index} {row.Key}: the description is empty.");
        Assert.True(description == description.Trim(), $"Row {index} {row.Key}: the description has leading or trailing whitespace.");
        Assert.True(
            description.IndexOfAny(new[] { '\r', '\n', '\u0085', (char)0x2028, (char)0x2029 }) < 0,
            $"Row {index} {row.Key}: the description has a line break character.");
        Assert.True(
            description.Length <= MaxLength,
            $"Row {index} {row.Key}: the description has {description.Length} characters, more than {MaxLength}.");

        if (row.HostSynced)
        {
            Assert.EndsWith(HostSuffix, description, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("Host-synced", description);
        }
    }

    // GW-U-099 (D4): the three hotkeys say they are unbound.
    [Fact]
    public void HotkeyDescriptionsSayUnboundByDefault()
    {
        foreach (string key in new[] { SettingKeys.CycleCategoryKey, SettingKeys.OrganizeKey, SettingKeys.DepositKey })
        {
            Assert.Contains("Unbound by default", SettingsCatalog.Get(key).Description);
        }
    }

    // GW-U-099 (D4): the DepositNeverItems description names the separator and the three class tokens.
    [Fact]
    public void DepositNeverItemsDescriptionNamesTheSeparatorAndTheClassTokens()
    {
        string description = SettingsCatalog.Get(SettingKeys.DepositNeverItems).Description;

        Assert.Contains(",", description);
        Assert.DoesNotContain("@food", description);
        Assert.Contains("@mead", description);
        Assert.Contains("@ammo", description);
        Assert.Contains("@bait", description);
    }
}
