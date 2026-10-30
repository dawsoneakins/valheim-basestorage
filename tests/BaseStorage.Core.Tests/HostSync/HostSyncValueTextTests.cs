namespace BaseStorage.Core.Tests.HostSync;

using System.Collections.Generic;
using BaseStorage.Core.HostSync;
using BaseStorage.Core.Settings;

/// <summary>AC-6.2 (contract 3.2, rule H2): the canonical text per SettingKind, and IsCanonical as TryCanonicalize returning the same string.</summary>
public sealed class HostSyncValueTextTests
{
    // GW-U-1003 (3.2): inputs with a canonical form, per kind.
    [Theory]
    [MemberData(nameof(CanonicalRows))]
    public void TextWithAFormCanonicalises(SettingKind kind, string input, string expected)
    {
        Assert.True(HostSyncValueText.TryCanonicalize(kind, input, out string canonical));
        Assert.Equal(expected, canonical);
        Assert.True(HostSyncValueText.IsCanonical(kind, expected));
        Assert.Equal(input == expected, HostSyncValueText.IsCanonical(kind, input));
    }

    public static IEnumerable<object[]> CanonicalRows()
    {
        yield return [SettingKind.Bool, "true", "true"];
        yield return [SettingKind.Bool, "false", "false"];
        yield return [SettingKind.Bool, "TRUE", "true"];
        yield return [SettingKind.Bool, "False", "false"];
        yield return [SettingKind.Int, "64", "64"];
        yield return [SettingKind.Int, "0", "0"];
        yield return [SettingKind.Int, "064", "64"];
        yield return [SettingKind.Int, "+5", "5"];
        yield return [SettingKind.Int, "-0", "0"];
        yield return [SettingKind.Int, "-12", "-12"];
        yield return [SettingKind.Int, "2147483647", "2147483647"];
        yield return [SettingKind.Int, "-2147483648", "-2147483648"];
        yield return [SettingKind.ItemList, "a, b ,,c", "a,b,c"];
        yield return [SettingKind.ItemList, "@food,@mead,@ammo,Hammer,Hoe,Cultivator,FishingRod,@bait", "@food,@mead,@ammo,Hammer,Hoe,Cultivator,FishingRod,@bait"];
        yield return [SettingKind.ItemList, "", ""];
        yield return [SettingKind.ItemList, " , ", ""];
        yield return [SettingKind.Hotkey, "", ""];
        yield return [SettingKind.Hotkey, "LeftAlt + Q", "LeftAlt + Q"];
        yield return [SettingKind.Hotkey, " Q ", " Q "];
        yield return [SettingKind.Hotkey, "ö", "ö"];
    }

    // GW-U-1003 (3.2): inputs without a canonical form, per kind: false with an empty string, and never canonical.
    [Theory]
    [MemberData(nameof(NoFormRows))]
    public void TextWithoutAFormIsRefused(SettingKind kind, string? input)
    {
        Assert.False(HostSyncValueText.TryCanonicalize(kind, input, out string canonical));
        Assert.Equal(string.Empty, canonical);
        Assert.False(HostSyncValueText.IsCanonical(kind, input));
    }

    public static IEnumerable<object?[]> NoFormRows()
    {
        yield return [SettingKind.Bool, null];
        yield return [SettingKind.Bool, ""];
        yield return [SettingKind.Bool, "1"];
        yield return [SettingKind.Bool, "yes"];
        yield return [SettingKind.Bool, " true"];
        yield return [SettingKind.Bool, "true "];
        yield return [SettingKind.Bool, "true\n"];
        yield return [SettingKind.Int, null];
        yield return [SettingKind.Int, ""];
        yield return [SettingKind.Int, "1.5"];
        yield return [SettingKind.Int, " 7"];
        yield return [SettingKind.Int, "7 "];
        yield return [SettingKind.Int, "99999999999"];
        yield return [SettingKind.Int, "2147483648"];
        yield return [SettingKind.Int, "٦"];
        yield return [SettingKind.Int, "1,000"];
        yield return [SettingKind.Int, "0x10"];
        yield return [SettingKind.Int, "64\n"];
        yield return [SettingKind.ItemList, null];
        yield return [SettingKind.ItemList, "a\nb"];
        yield return [SettingKind.ItemList, "a\r,b"];
        yield return [SettingKind.ItemList, "a\u007F"];
        yield return [SettingKind.Hotkey, null];
        yield return [SettingKind.Hotkey, "Q\t"];
        yield return [SettingKind.Hotkey, "Q\n"];
        yield return [SettingKind.Hotkey, "\0"];
        yield return [(SettingKind)99, "x"];
        yield return [(SettingKind)(-1), "true"];
    }

    // GW-U-1003 (H2): canonical text never contains a control character, for every kind and every defined default.
    [Fact]
    public void EveryCatalogDefaultIsCanonicalForItsKind()
    {
        foreach (SettingDefinition row in SettingsCatalog.All)
        {
            Assert.True(HostSyncValueText.IsCanonical(row.Kind, row.DefaultText), row.Key + " default is not canonical.");
        }
    }
}
