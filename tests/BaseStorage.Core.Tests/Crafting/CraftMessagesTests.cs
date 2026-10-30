namespace BaseStorage.Core.Tests.Crafting;

using System.Globalization;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Crafting;

/// <summary>GW-U-1127 (AC-4, contract 5.3 NoticeKeyFor and contract 6 K1-K5): the notice key per end and the Debug line formats.</summary>
public sealed class CraftMessagesTests
{
    private const string MaterialsMoved = "message.craft.materials-moved";

    // GW-U-1127 (5.3): the notice key per end.
    [Theory]
    [InlineData(CraftEnd.Returned, MaterialsMoved)]
    [InlineData(CraftEnd.Failed, MaterialsMoved)]
    [InlineData(CraftEnd.PreFlightFailed, MaterialsMoved)]
    [InlineData(CraftEnd.Unavailable, MaterialsMoved)]
    // Edited by S-E008-001 (ADR-GWIN-153, FA1): Created now gives the success line, not silence.
    [InlineData(CraftEnd.Created, StringKeys.MessageCraftUsedBase)]
    [InlineData(CraftEnd.Abandoned, null)]
    public void NoticeKeyForEachEnd(object end, string? expected)
    {
        Assert.Equal(expected, CraftSession.NoticeKeyFor((CraftEnd)end));
    }

    // GW-U-1127 (5.3): an undefined end throws by name.
    [Fact]
    public void AnUndefinedEndThrowsByName()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => CraftSession.NoticeKeyFor((CraftEnd)99));

        Assert.Equal("end", ex.ParamName);
    }

    // GW-U-1127 (K1): the start line.
    [Fact]
    public void K1RendersTheStartLine()
    {
        Assert.Equal("Craft 3: started, 4 take(s) from 2 chest(s)", CraftMessages.Started(3, 4, 2));
    }

    // GW-U-1127 (K2): the end lines.
    [Theory]
    [InlineData(CraftEnd.Created, "Craft 3: Created")]
    [InlineData(CraftEnd.Abandoned, "Craft 3: Abandoned")]
    [InlineData(CraftEnd.Unavailable, "Craft 3: Unavailable")]
    public void K2RendersTheEndLines(object end, string expected)
    {
        Assert.Equal(expected, CraftMessages.Ended(3, (CraftEnd)end));
    }

    // GW-U-1127 (K3): the failure line, with and without the container.
    [Fact]
    public void K3RendersTheFailureLineWithAndWithoutTheContainer()
    {
        Assert.Equal(
            "Craft 3: Failed (ReservationFailed at 2718281828:7)",
            CraftMessages.Failed(3, ConsumptionOutcome.ReservationFailed, new ContainerId(CraftFixtures.Erik, 7u)));
        Assert.Equal("Craft 6: Failed (Cancelled)", CraftMessages.Failed(6, ConsumptionOutcome.Cancelled, default));
    }

    // GW-U-1127 (K3): an undefined outcome renders as its integer.
    [Fact]
    public void AnUndefinedOutcomeRendersAsItsInteger()
    {
        Assert.Equal("Craft 3: Failed (99)", CraftMessages.Failed(3, (ConsumptionOutcome)99, default));
    }

    // GW-U-1127 (K4): the returned line.
    [Fact]
    public void K4RendersTheReturnedLine()
    {
        Assert.Equal("Craft 7: Returned, 4 stack(s) put back", CraftMessages.Returned(7, 4));
    }

    // GW-U-1127 (K5): the vanilla line, and an undefined reason as its integer.
    [Fact]
    public void K5RendersTheVanillaLine()
    {
        Assert.Equal("Craft: vanilla (NotInBase)", CraftMessages.Vanilla(CraftGateReason.NotInBase));
        Assert.Equal("Craft: vanilla (99)", CraftMessages.Vanilla((CraftGateReason)99));
    }

    // GW-U-1127: the lines are invariant under the current culture.
    [Theory]
    [InlineData("de-DE")]
    [InlineData("ar-SA")]
    public void TheLinesAreInvariantUnderTheCurrentCulture(string culture)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            Assert.Equal("Craft 3: started, 4 take(s) from 2 chest(s)", CraftMessages.Started(3, 4, 2));
            Assert.Equal("Craft 7: Returned, 4 stack(s) put back", CraftMessages.Returned(7, 4));
            Assert.Equal(
                "Craft 3: Failed (ReservationFailed at 2718281828:7)",
                CraftMessages.Failed(3, ConsumptionOutcome.ReservationFailed, new ContainerId(CraftFixtures.Erik, 7u)));
            Assert.Equal("Craft: vanilla (NotInBase)", CraftMessages.Vanilla(CraftGateReason.NotInBase));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
