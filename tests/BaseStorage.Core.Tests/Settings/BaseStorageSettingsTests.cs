namespace BaseStorage.Core.Tests.Settings;

using System;
using System.Collections.Generic;
using System.Globalization;
using BaseStorage.Core.Settings;

/// <summary>
/// AC-1 and AC-7 (contract 5.2, 5.3, R8; ADR-GWIN-015): the read path over ISettingsSource. Defaults come from the literal
/// section 7 copy, and the fake records every TryGet call.
/// </summary>
public sealed class BaseStorageSettingsTests
{
    // GW-U-104 (AC-1): defaults are the same under the invariant culture and under cultures that format numbers differently.
    [Theory]
    [MemberData(nameof(CultureNames))]
    public void EveryTypedPropertyEqualsItsParsedSection7DefaultUnderAnyCulture(string cultureName)
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var culture = new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            var settings = new BaseStorageSettings(new FakeSettingsSource());

            Assert.Equal(BoolDefault("CraftFromBase"), settings.CraftFromBase);
            Assert.Equal(BoolDefault("ExcludeVehicles"), settings.ExcludeVehicles);
            Assert.Equal(BoolDefault("KeepChestsCraftable"), settings.KeepChestsCraftable);
            Assert.Equal(IntDefault("MaxBaseStations"), settings.MaxBaseStations);
            Assert.Equal(IntDefault("BaseCacheSeconds"), settings.BaseCacheSeconds);
            Assert.Equal(BoolDefault("RequireSameVersion"), settings.RequireSameVersion);
            Assert.Equal(IntDefault("AutoAssignThresholdPct"), settings.AutoAssignThresholdPct);
            Assert.Equal(BoolDefault("OrganizeSkipPreview"), settings.OrganizeSkipPreview);
            Assert.Equal(BoolDefault("DepositPreview"), settings.DepositPreview);
            Assert.Equal(IntDefault("MovesPerFrame"), settings.MovesPerFrame);
            Assert.Equal(IntDefault("OrganizeLockSeconds"), settings.OrganizeLockSeconds);
            Assert.Equal(IntDefault("ReserveTimeoutMs"), settings.ReserveTimeoutMs);
            Assert.Equal(BoolDefault("StationButtons"), settings.StationButtons);
            Assert.Equal(BoolDefault("DebugLogging"), settings.DebugLogging);
            Assert.Equal(string.Empty, settings.CycleCategoryKey);
            Assert.Equal(string.Empty, settings.OrganizeKey);
            Assert.Equal(string.Empty, settings.DepositKey);

            string[] expectedItems = { "@food", "@mead", "@ammo", "Hammer", "Hoe", "Cultivator", "FishingRod", "@bait" };
            Assert.Equal(expectedItems, settings.DepositNeverItems);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    // GW-U-105 (AC-7): the contract 5.3 examples, verbatim.
    [Fact]
    public void ReadPathExamplesFromContract53()
    {
        var source = new FakeSettingsSource()
            .WithBool("CraftFromBase", false)
            .WithInt("MaxBaseStations", 12)
            .WithInt("ReserveTimeoutMs", 2500)
            .WithText("DepositNeverItems", "Hammer, TrophyDeer ,@food")
            .WithText("OrganizeKey", "O + LeftControl");
        var settings = new BaseStorageSettings(source);

        Assert.False(settings.CraftFromBase);
        Assert.False(settings.GetBool(SettingKeys.CraftFromBase));
        Assert.Equal(12, settings.MaxBaseStations);
        Assert.Equal(2500, settings.ReserveTimeoutMs);
        Assert.True(settings.ExcludeVehicles);
        Assert.Equal(20, settings.MovesPerFrame);
        Assert.Equal(2, settings.BaseCacheSeconds);
        Assert.Equal(50, settings.AutoAssignThresholdPct);
        Assert.Equal(30, settings.OrganizeLockSeconds);
        Assert.False(settings.DebugLogging);
        Assert.Equal(new[] { "Hammer", "TrophyDeer", "@food" }, settings.DepositNeverItems);
        Assert.Equal("O + LeftControl", settings.OrganizeKey);
        Assert.Equal(string.Empty, settings.DepositKey);
    }

    // GW-U-105 (AC-4.9): an empty list in the source is respected and does not fall back to the default.
    [Fact]
    public void AnEmptyItemListInTheSourceGivesAnEmptyListNotTheDefault()
    {
        var source = new FakeSettingsSource().WithText("DepositNeverItems", string.Empty);
        var settings = new BaseStorageSettings(source);

        Assert.Empty(settings.DepositNeverItems);
    }

    // GW-U-106 (AC-7): live reads. A value changed after construction shows on the next read.
    [Fact]
    public void ReadsAreLiveAndNothingIsCached()
    {
        var source = new FakeSettingsSource();
        var settings = new BaseStorageSettings(source);
        Assert.Equal(64, settings.MaxBaseStations);

        source.WithInt("MaxBaseStations", 40);
        Assert.Equal(40, settings.MaxBaseStations);
    }

    // GW-U-106 (AC-7): two reads of one property give two TryGetInt calls.
    [Fact]
    public void TwoReadsOfOnePropertyCallTheSourceTwice()
    {
        var source = new FakeSettingsSource();
        var settings = new BaseStorageSettings(source);

        _ = settings.MaxBaseStations;
        _ = settings.MaxBaseStations;

        Assert.Equal(2, source.IntCalls);
        Assert.Equal(0, source.BoolCalls);
        Assert.Equal(0, source.TextCalls);
    }

    // GW-U-106 (AC-7): each typed property calls exactly one TryGet of its kind (Bool, Int, or Text for ItemList and Hotkey).
    [Theory]
    [MemberData(nameof(Section7Table.IndexData), MemberType = typeof(Section7Table))]
    public void EachTypedPropertyCallsExactlyOneTryGetOfItsKind(int index)
    {
        var row = Section7Table.Rows[index];
        var source = new FakeSettingsSource();
        var settings = new BaseStorageSettings(source);

        _ = typeof(BaseStorageSettings).GetProperty(row.Key)!.GetValue(settings);

        (int expectedBool, int expectedInt, int expectedText) = row.Kind switch
        {
            SettingKind.Bool => (1, 0, 0),
            SettingKind.Int => (0, 1, 0),
            _ => (0, 0, 1),
        };
        Assert.Equal((expectedBool, expectedInt, expectedText), (source.BoolCalls, source.IntCalls, source.TextCalls));
    }

    // GW-U-107 (R8): a null source is rejected with the parameter name.
    [Fact]
    public void ConstructorRejectsANullSource()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new BaseStorageSettings(null!));

        Assert.Equal("source", ex.ParamName);
    }

    // GW-U-107 (R8): a null key is rejected before the source is read.
    [Fact]
    public void GetBoolRejectsANullKeyBeforeTheSourceIsRead()
    {
        var source = new FakeSettingsSource();
        var settings = new BaseStorageSettings(source);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => settings.GetBool(null!));

        Assert.Equal("key", ex.ParamName);
        Assert.Equal((0, 0, 0), (source.BoolCalls, source.IntCalls, source.TextCalls));
    }

    // GW-U-107 (S3, kind mismatch): unknown and mismatched keys throw ArgumentException before the source is read.
    [Fact]
    public void UnknownAndKindMismatchedKeysAreRejectedBeforeTheSourceIsRead()
    {
        var source = new FakeSettingsSource();
        var settings = new BaseStorageSettings(source);

        AssertKeyArgumentException(() => settings.GetInt("craftfrombase"));
        AssertKeyArgumentException(() => settings.GetInt(SettingKeys.CraftFromBase));
        AssertKeyArgumentException(() => settings.GetItemList("OrganizeKey"));
        AssertKeyArgumentException(() => settings.GetHotkeyText("DepositNeverItems"));

        Assert.Equal((0, 0, 0), (source.BoolCalls, source.IntCalls, source.TextCalls));
    }

    // GW-U-107 (R8): exceptions from the source propagate, and the same instance comes out.
    [Fact]
    public void SourceExceptionsPropagateUnchanged()
    {
        var boom = new InvalidOperationException("source failed");
        var source = new FakeSettingsSource { Throws = boom };
        var settings = new BaseStorageSettings(source);

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() => settings.MaxBaseStations);

        Assert.Same(boom, thrown);
    }

    /// <summary>The culture names for the defaults theory: invariant, Turkish, Arabic and German.</summary>
    public static IEnumerable<object[]> CultureNames()
    {
        yield return new object[] { string.Empty };
        yield return new object[] { "tr-TR" };
        yield return new object[] { "ar-SA" };
        yield return new object[] { "de-DE" };
    }

    private static void AssertKeyArgumentException(Action read)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(read);

        Assert.Equal("key", ex.ParamName);
    }

    private static bool BoolDefault(string key) => bool.Parse(Section7Table.Row(key).DefaultText);

    private static int IntDefault(string key) => int.Parse(Section7Table.Row(key).DefaultText, CultureInfo.InvariantCulture);
}
