namespace BaseStorage.Core.Tests.Building;

using System.Globalization;
using BaseStorage.Plugin.Building;

/// <summary>GW-U-1602 and GW-U-1603 (AC-7, AC-2; contract 3.1): the pinned names and values of the build enums.</summary>
public sealed class BuildEnumTests
{
    // GW-U-1602 (3.1): exactly the seven BuildEnd names and values over int.
    [Fact]
    public void BuildEndHasExactlyTheSevenPinnedNamesAndValues()
    {
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(BuildEnd)));
        Assert.Equal(
            new[]
            {
                "Placed=0", "Returned=1", "Failed=2", "PreFlightFailed=3", "Unavailable=4", "Abandoned=5", "PlacementRefused=6",
            },
            Enum.GetValues(typeof(BuildEnd)).Cast<BuildEnd>().Select(value => value + "=" + ((int)value).ToString(CultureInfo.InvariantCulture)));
    }

    // GW-U-1603 (3.1): exactly the ten BuildGateReason names and values over int.
    [Fact]
    public void BuildGateReasonHasExactlyTheTenPinnedNamesAndValues()
    {
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(BuildGateReason)));
        Assert.Equal(
            new[]
            {
                "UsesBase=0", "SettingOff=1", "NotLocalPlayer=2", "NoCost=3", "CraftPending=4", "NotInBase=5", "Mode=6", "FreeBuild=7",
                "UpgraderResource=8", "Unreadable=9",
            },
            Enum.GetValues(typeof(BuildGateReason)).Cast<BuildGateReason>().Select(value => value + "=" + ((int)value).ToString(CultureInfo.InvariantCulture)));
    }
}
