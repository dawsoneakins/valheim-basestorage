namespace BaseStorage.Core.Tests.Crafting;

using System.Globalization;
using BaseStorage.Plugin.Crafting;

/// <summary>GW-U-1102 (AC-2, contract 3.1): the craft enums have exactly the contracted names and values.</summary>
public sealed class CraftEnumTests
{
    // GW-U-1102: CraftPhase has three members.
    [Fact]
    public void CraftPhaseHasTheContractedNamesAndValues()
    {
        Assert.Equal(new[] { ("Idle", 0), ("Executing", 1), ("Committing", 2) }, Pairs<CraftPhase>());
    }

    // GW-U-1102: CraftEnd has six members.
    [Fact]
    public void CraftEndHasTheContractedNamesAndValues()
    {
        Assert.Equal(
            new[] { ("Created", 0), ("Returned", 1), ("Failed", 2), ("PreFlightFailed", 3), ("Unavailable", 4), ("Abandoned", 5) },
            Pairs<CraftEnd>());
    }

    // GW-U-1102: CraftGateReason has ten members.
    [Fact]
    public void CraftGateReasonHasTheContractedNamesAndValues()
    {
        Assert.Equal(
            new[]
            {
                ("UsesBase", 0), ("SettingOff", 1), ("Discover", 2), ("NotLocalPlayer", 3), ("NoCost", 4),
                ("NoStation", 5), ("UpgraderStation", 6), ("OnlyOneIngredient", 7), ("NotInBase", 8), ("Unreadable", 9),
            },
            Pairs<CraftGateReason>());
    }

    private static (string Name, int Value)[] Pairs<TEnum>()
        where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Select(value => (Enum.GetName(value)!, Convert.ToInt32(value, CultureInfo.InvariantCulture))).ToArray();
}
