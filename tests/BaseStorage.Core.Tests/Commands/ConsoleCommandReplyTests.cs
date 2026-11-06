namespace BaseStorage.Core.Tests.Commands;

using System;
using System.Globalization;
using BaseStorage.Core.Commands;
using BaseStorage.Core.Organize.Run;

/// <summary>
/// GW-U-2302 to GW-U-2306 (AC-2, AC-3, AC-6, AC-1; organize-console-command contract 2.3, 2.4 and 8.2, rows R1-R14): the reply key and
/// the one reply line of each start outcome, the usage and description texts, and the consistency facts against the HUD notices.
/// Red until <c>BaseStorage.Core.Commands.ConsoleCommandText</c> exists. Not a test row.
/// </summary>
public sealed class ConsoleCommandReplyTests
{
    // GW-U-2302 (R1, R12): NotInBase is the not-in-base key and text, whatever the state and the holder name.
    [Theory]
    [InlineData(OrganizeRunState.Idle, null)]
    [InlineData(OrganizeRunState.Running, "Erik")]
    [InlineData(OrganizeRunState.Previewing, "Astrid")]
    public void NotInBaseGivesTheNotInBaseReply(OrganizeRunState state, string? holder)
    {
        Assert.Equal("message.base.not-in-base", ConsoleCommandText.ReplyKey(OrganizeStartOutcome.NotInBase, state));
        Assert.Equal("Not in a base", ConsoleCommandText.Reply(OrganizeStartOutcome.NotInBase, state, holder));
    }

    // GW-U-2303 (R2-R6): LockRefused and AlreadyActive give the in-progress key. The holder is substituted, a blank or null holder
    // falls back to "another player", and a line break collapses to one space.
    [Theory]
    [InlineData(OrganizeStartOutcome.LockRefused, OrganizeRunState.Idle, "Astrid", "Organize in progress by Astrid")]
    [InlineData(OrganizeStartOutcome.AlreadyActive, OrganizeRunState.Previewing, "Erik", "Organize in progress by Erik")]
    [InlineData(OrganizeStartOutcome.LockRefused, OrganizeRunState.Idle, null, "Organize in progress by another player")]
    [InlineData(OrganizeStartOutcome.LockRefused, OrganizeRunState.Idle, "   ", "Organize in progress by another player")]
    [InlineData(OrganizeStartOutcome.LockRefused, OrganizeRunState.Idle, "Bad\nName", "Organize in progress by Bad Name")]
    public void InProgressRepliesNameTheHolderOrFallBack(OrganizeStartOutcome outcome, OrganizeRunState state, string? holder, string expected)
    {
        Assert.Equal("message.base.in-progress", ConsoleCommandText.ReplyKey(outcome, state));
        Assert.Equal(expected, ConsoleCommandText.Reply(outcome, state, holder));
    }

    // GW-U-2304 (R7-R11, R14): the preview, the started line, the cancelled line (also for an undefined state), no chests and no player.
    // The holder name is ignored by Started.
    [Theory]
    [InlineData(OrganizeStartOutcome.Started, OrganizeRunState.Previewing, null, "console.organize.preview", "Organize preview open, close the console to confirm")]
    [InlineData(OrganizeStartOutcome.Started, OrganizeRunState.Running, "Erik", "console.organize.started", "Organize started")]
    [InlineData(OrganizeStartOutcome.Started, OrganizeRunState.Idle, null, "message.organize.cancelled", "Organize cancelled")]
    [InlineData(OrganizeStartOutcome.NoChests, OrganizeRunState.Idle, null, "station.actions.disabled-no-chests", "No chests in this base")]
    [InlineData(OrganizeStartOutcome.NoPlayer, OrganizeRunState.Idle, null, "console.organize.no-player", "Not in a world")]
    [InlineData(OrganizeStartOutcome.Started, (OrganizeRunState)7, null, "message.organize.cancelled", "Organize cancelled")]
    public void StartedNoChestsAndNoPlayerRepliesAreChosenByState(OrganizeStartOutcome outcome, OrganizeRunState state, string? holder, string key, string text)
    {
        Assert.Equal(key, ConsoleCommandText.ReplyKey(outcome, state));
        Assert.Equal(text, ConsoleCommandText.Reply(outcome, state, holder));
    }

    // GW-U-2305 (R13): an undefined outcome throws ArgumentOutOfRangeException with ParamName "outcome" from both functions.
    [Fact]
    public void AnUndefinedOutcomeThrowsFromBothFunctions()
    {
        var outcome = (OrganizeStartOutcome)99;

        ArgumentOutOfRangeException key = Assert.Throws<ArgumentOutOfRangeException>(() => ConsoleCommandText.ReplyKey(outcome, OrganizeRunState.Idle));
        Assert.Equal("outcome", key.ParamName);

        ArgumentOutOfRangeException reply = Assert.Throws<ArgumentOutOfRangeException>(() => ConsoleCommandText.Reply(outcome, OrganizeRunState.Idle, null));
        Assert.Equal("outcome", reply.ParamName);
    }

    // GW-U-2306 (2.4): the usage line, the command word, and the console lines equal the HUD notice texts (CC3).
    [Fact]
    public void TheConsoleLinesAreTheHudNoticeTexts()
    {
        Assert.Equal("Usage: dons-qol organize", ConsoleCommandText.Usage());
        Assert.Equal("Usage: " + ConsoleCommandText.CommandName + " " + ConsoleCommandText.OrganizeVerb, ConsoleCommandText.Usage());
        Assert.Equal(ConsoleCommandText.CommandName, ConsoleCommandText.CommandName.ToLowerInvariant());
        Assert.DoesNotMatch(@"\s", ConsoleCommandText.CommandName);

        Assert.Equal("Not in a base", ConsoleCommandText.Reply(OrganizeStartOutcome.NotInBase, OrganizeRunState.Idle, null));
        Assert.Equal("Organize in progress by Astrid", ConsoleCommandText.Reply(OrganizeStartOutcome.LockRefused, OrganizeRunState.Idle, "Astrid"));
    }

    // GW-U-2306 (2.4, U2): Usage and Description are the same under de-DE and tr-TR.
    [Theory]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    public void UsageAndDescriptionDoNotChangeWithTheCulture(string culture)
    {
        CultureInfo savedCulture = CultureInfo.CurrentCulture;
        CultureInfo savedUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo chosen = new CultureInfo(culture);
            CultureInfo.CurrentCulture = chosen;
            CultureInfo.CurrentUICulture = chosen;

            Assert.Equal("Usage: dons-qol organize", ConsoleCommandText.Usage());
            Assert.Equal("organize - start an Organize for the base you are in", ConsoleCommandText.Description());
        }
        finally
        {
            CultureInfo.CurrentCulture = savedCulture;
            CultureInfo.CurrentUICulture = savedUiCulture;
        }
    }
}
