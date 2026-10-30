namespace BaseStorage.Core.Tests.Building;

using System.Globalization;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Tests.Crafting;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Building;

/// <summary>GW-U-1624 and GW-U-1625 (AC-7, AC-2; contract 5.4 NoticeKeyFor, section 6 BK3, BK4, BK5, BK6): the notice keys and the Debug lines.</summary>
public sealed class BuildMessagesTests
{
    private const string MaterialsMoved = "message.craft.materials-moved";

    // Edited by S-E008-001 (ADR-GWIN-153, FA2): Placed gives the success line; the three refusal ends give the materials-moved key; the other three give none.
    [Theory]
    [InlineData(BuildEnd.Failed, MaterialsMoved)]
    [InlineData(BuildEnd.PreFlightFailed, MaterialsMoved)]
    [InlineData(BuildEnd.Unavailable, MaterialsMoved)]
    [InlineData(BuildEnd.Placed, StringKeys.MessageCraftUsedBase)]
    [InlineData(BuildEnd.Returned, null)]
    [InlineData(BuildEnd.Abandoned, null)]
    [InlineData(BuildEnd.PlacementRefused, null)]
    public void TheNoticeKeyFollowsTheEnd(BuildEnd end, string? key)
    {
        Assert.Equal(key, BuildSession.NoticeKeyFor(end));
    }

    // GW-U-1624 (5.4 NoticeKeyFor): an undefined end throws ArgumentOutOfRangeException with ParamName "end".
    [Fact]
    public void AnUndefinedEndThrowsWithTheParameterName()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => BuildSession.NoticeKeyFor((BuildEnd)99));

        Assert.Equal("end", ex.ParamName);
    }

    // GW-U-1624 (BK3): a reservation failure renders with the container id.
    [Fact]
    public void AReservationFailureRendersWithTheContainer()
    {
        Assert.Equal("Build 3: Failed (ReservationFailed at 2718281828:7)", BuildMessages.Failed(3, ConsumptionOutcome.ReservationFailed, CraftFixtures.C7));
    }

    // GW-U-1624 (BK3): a cancelled execution with no container renders without the " at" clause.
    [Fact]
    public void ACancelledFailureRendersWithoutAContainer()
    {
        Assert.Equal("Build 6: Failed (Cancelled)", BuildMessages.Failed(6, ConsumptionOutcome.Cancelled, default));
    }

    // GW-U-1624 (BK3): an undefined outcome renders as its invariant integer.
    [Fact]
    public void AnUndefinedOutcomeRendersAsItsInteger()
    {
        Assert.Equal("Build 3: Failed (99)", BuildMessages.Failed(3, (ConsumptionOutcome)99, default));
    }

    // GW-U-1624 (BK4): a returned commit renders the number of stacks put back.
    [Fact]
    public void AReturnedCommitRendersTheStacksPutBack()
    {
        Assert.Equal("Build 7: Returned, 4 stack(s) put back", BuildMessages.Returned(7, 4));
    }

    // GW-U-1624 (BK6): the suppression line is exact.
    [Fact]
    public void TheSuppressionLineIsExact()
    {
        Assert.Equal("Build: placement suppressed, inventory does not cover the piece", BuildMessages.Suppressed());
    }

    // GW-U-1624 (5.4, 6): the rendering is invariant under de-DE and ar-SA.
    [Fact]
    public void TheRenderingIsInvariantUnderOtherCultures()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            foreach (string name in new[] { "de-DE", "ar-SA" })
            {
                CultureInfo.CurrentCulture = new CultureInfo(name);

                Assert.Equal("Build 3: Failed (ReservationFailed at 2718281828:7)", BuildMessages.Failed(3, ConsumptionOutcome.ReservationFailed, CraftFixtures.C7));
                Assert.Equal("Build 7: Returned, 4 stack(s) put back", BuildMessages.Returned(7, 4));
                Assert.Equal("Build 3: Failed (99)", BuildMessages.Failed(3, (ConsumptionOutcome)99, default));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // GW-U-1625 (BK5, AC-2): the vanilla line names the reason.
    [Fact]
    public void TheVanillaLineNamesTheNotInBaseReason()
    {
        Assert.Equal("Build: vanilla (NotInBase)", BuildMessages.Vanilla(BuildGateReason.NotInBase));
    }

    // GW-U-1625 (BK5): every reason renders by name, and an undefined value renders as its invariant integer.
    [Fact]
    public void EveryReasonRendersByNameAndAnUndefinedOneAsItsInteger()
    {
        foreach (BuildGateReason reason in Enum.GetValues(typeof(BuildGateReason)).Cast<BuildGateReason>())
        {
            Assert.Equal("Build: vanilla (" + reason + ")", BuildMessages.Vanilla(reason));
        }

        Assert.Equal("Build: vanilla (99)", BuildMessages.Vanilla((BuildGateReason)99));
    }
}
