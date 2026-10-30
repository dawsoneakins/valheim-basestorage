namespace BaseStorage.Core.Tests.Bases;

using System;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Settings;

/// <summary>AC-12 (contract 4, X-14): the two eligibility settings as plain values, with value equality and the settings read path.</summary>
public sealed class EligibilityOptionsTests
{
    // GW-U-220 (AC-12): the constructor round-trips all four pairs.
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void ConstructorRoundTripsBothValues(bool excludeVehicles, bool keepChestsCraftable)
    {
        var options = new EligibilityOptions(excludeVehicles, keepChestsCraftable);

        Assert.Equal(excludeVehicles, options.ExcludeVehicles);
        Assert.Equal(keepChestsCraftable, options.KeepChestsCraftable);
    }

    // GW-U-220 (AC-12): default(EligibilityOptions) is (false, false), which is not the catalog default.
    [Fact]
    public void DefaultValueIsFalseFalse()
    {
        EligibilityOptions options = default;

        Assert.False(options.ExcludeVehicles);
        Assert.False(options.KeepChestsCraftable);
    }

    // GW-U-220 (AC-12): equality covers Equals(T), Equals(object), == and != and the hash code.
    [Fact]
    public void EqualityCoversEqualsOperatorsAndHashCode()
    {
        var trueFalse = new EligibilityOptions(true, false);
        var sameTrueFalse = new EligibilityOptions(true, false);
        var falseTrue = new EligibilityOptions(false, true);

        Assert.True(trueFalse.Equals(sameTrueFalse));
        Assert.False(trueFalse.Equals(falseTrue));
        Assert.False(trueFalse.Equals((object?)null));
        Assert.False(trueFalse.Equals((object)1));
        Assert.True(trueFalse.Equals((object)sameTrueFalse));

        Assert.True(trueFalse == sameTrueFalse);
        Assert.False(trueFalse != sameTrueFalse);
        Assert.False(trueFalse == falseTrue);
        Assert.True(trueFalse != falseTrue);

        Assert.Equal(trueFalse.GetHashCode(), sameTrueFalse.GetHashCode());
    }

    // GW-U-220 (AC-12): with an empty source, FromSettings returns the catalog defaults (true, true).
    [Fact]
    public void FromEmptySourceReturnsTheCatalogDefaults()
    {
        var source = new FakeSettingsSource();

        EligibilityOptions options = EligibilityOptions.FromSettings(new BaseStorageSettings(source));

        Assert.Equal(new EligibilityOptions(true, true), options);
        Assert.True(options.ExcludeVehicles);
        Assert.True(options.KeepChestsCraftable);
    }

    // GW-U-220 (AC-12, contract 4): FromSettings reads each value exactly once, which is 2 Bool calls on the fake.
    [Fact]
    public void FromSettingsReadsTheSourceValuesOnce()
    {
        var source = new FakeSettingsSource()
            .WithBool(SettingKeys.ExcludeVehicles, false)
            .WithBool(SettingKeys.KeepChestsCraftable, true);

        EligibilityOptions options = EligibilityOptions.FromSettings(new BaseStorageSettings(source));

        Assert.False(options.ExcludeVehicles);
        Assert.True(options.KeepChestsCraftable);
        Assert.Equal(2, source.BoolCalls);
    }

    // GW-U-220 (AC-12, X-14): a null settings argument is rejected with the parameter name.
    [Fact]
    public void FromSettingsRejectsNullSettings()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
        {
            EligibilityOptions.FromSettings(null!);
        });

        Assert.Equal("settings", ex.ParamName);
    }
}
