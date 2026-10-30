namespace BaseStorage.Core.Tests.StationActions;

using System;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Organize;

/// <summary>
/// GW-U-1767..1771 (AC-3, contract 9.3): the facts read cadence of the station panel (the memo), its zero-allocation steady
/// state, and the Debug lines. The memo and the messages are game-free Plugin types reached through <see cref="StationActionsTypes"/>,
/// so the rows are red until they are implemented.
/// </summary>
public sealed class StationActionsMemoTests
{
    private static readonly StationId S7 = new StationId(OrganizeFixtures.UserId, 7u);
    private static readonly StationId S5 = new StationId(OrganizeFixtures.UserId, 5u);

    // GW-U-1767 (AC-3, 9.3): a new memo is due for its first read.
    [Fact]
    public void ANewMemoIsDueForTheFirstRead()
    {
        object memo = StationActionsTypes.New("StationActionsMemo");

        Assert.True(Due(memo, S7, Seconds(3600)));
    }

    // GW-U-1768 (AC-3, 9.3): read again after one second, on a station change and when the clock goes back; not due at +0.5 s.
    [Fact]
    public void TheFactsCadenceIsOneSecondAndResetsOnStationOrClockChange()
    {
        object memo = StationActionsTypes.New("StationActionsMemo");
        Mark(memo, S7, Seconds(3600));

        Assert.False(Due(memo, S7, Seconds(3600.5)));
        Assert.True(Due(memo, S7, Seconds(3601)));
        Assert.True(Due(memo, S5, Seconds(3600.5)));
        Assert.True(Due(memo, S7, Seconds(3599)));
    }

    // GW-U-1769 (AC-3, 9.3): Invalidate makes the next read due, and a read after it clears that.
    [Fact]
    public void InvalidateForcesOneReadAndMarkReadClearsIt()
    {
        object memo = StationActionsTypes.New("StationActionsMemo");
        Mark(memo, S7, Seconds(3600));

        StationActionsTypes.CallInstance(memo, "Invalidate");
        Assert.True(Due(memo, S7, Seconds(3600)));

        Mark(memo, S7, Seconds(3600));
        Assert.False(Due(memo, S7, Seconds(3600)));
    }

    // GW-U-1770 (AC-3, SA11): FactsDue allocates nothing on the managed heap after a warm-up.
    [Fact]
    public void FactsDueAllocatesNothingAfterAWarmUp()
    {
        object memo = StationActionsTypes.New("StationActionsMemo");
        Func<StationId, TimeSpan, bool> due = StationActionsTypes.Bind<Func<StationId, TimeSpan, bool>>(memo, "FactsDue");
        Action<StationId, TimeSpan> mark = StationActionsTypes.Bind<Action<StationId, TimeSpan>>(memo, "MarkRead");
        mark(S7, Seconds(3600));
        for (int i = 0; i < 100; i++)
        {
            _ = due(S7, Seconds(3600));
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            _ = due(S7, Seconds(3600) + TimeSpan.FromMilliseconds(i % 900));
        }

        long after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0L, after - before);
    }

    // GW-U-1771 (AC-3, 9.3): the click Debug line is the outcome name, or the integer when the value is undefined.
    [Theory]
    [InlineData(OrganizeStartOutcome.Started, "[station.actions.organize-button] Organize Started")]
    [InlineData(OrganizeStartOutcome.LockRefused, "[station.actions.organize-button] Organize LockRefused")]
    [InlineData((OrganizeStartOutcome)99, "[station.actions.organize-button] Organize 99")]
    public void ClickedLinesRenderTheOutcomeExactly(OrganizeStartOutcome outcome, string expected)
    {
        Assert.Equal(expected, (string?)StationActionsTypes.CallStatic("StationActionsMessages", "Clicked", outcome));
    }

    // GW-U-1771 (AC-3, 9.3): the no-room Debug line is the exact contracted text.
    [Fact]
    public void TheNoRoomLineIsTheContractedText()
    {
        Assert.Equal(
            "[station.actions.row] hidden: no room next to the craft controls",
            (string?)StationActionsTypes.Constant("StationActionsMessages", "NoRoom"));
    }

    private static TimeSpan Seconds(double value) => TimeSpan.FromSeconds(value);

    private static bool Due(object memo, StationId station, TimeSpan now) =>
        (bool)StationActionsTypes.CallInstance(memo, "FactsDue", station, now)!;

    private static void Mark(object memo, StationId station, TimeSpan now) =>
        StationActionsTypes.CallInstance(memo, "MarkRead", station, now);
}
