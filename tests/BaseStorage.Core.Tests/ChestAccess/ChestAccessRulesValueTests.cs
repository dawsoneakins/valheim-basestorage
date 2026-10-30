namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.ChestAccess;

/// <summary>GW-U-769 (AC-2): the release-clear rule and the late-response window (contract 2.4 other members, 2.5 last table).</summary>
public sealed class ChestAccessRulesValueTests
{
    // GW-U-769: the four ShouldClearInUse rows of contract 2.5.
    [Theory]
    [InlineData(true, true, true, false, true)]
    [InlineData(true, true, false, false, false)]
    [InlineData(false, true, true, false, false)]
    [InlineData(true, true, true, true, false)]
    [InlineData(true, false, true, false, false)]
    public void ShouldClearInUseMatchesTheContractRow(bool setByAdapter, bool viewValid, bool isOwner, bool openLocally, bool expected)
    {
        Assert.Equal(expected, ChestAccessRules.ShouldClearInUse(setByAdapter, viewValid, isOwner, openLocally));
    }

    // GW-U-769: the late window is twice the timeout, with values below 1 read as 1, and never throws.
    [Theory]
    [InlineData(1500, 30000000L)]
    [InlineData(0, 20000L)]
    [InlineData(-5, 20000L)]
    [InlineData(1, 20000L)]
    [InlineData(int.MaxValue, 42949672940000L)]
    public void LateResponseWindowIsTwiceTheTimeoutInTicks(int reserveTimeoutMs, long expectedTicks)
    {
        Assert.Equal(TimeSpan.FromTicks(expectedTicks), ChestAccessRules.LateResponseWindow(reserveTimeoutMs));
    }

    // GW-U-769: the two contract examples in milliseconds.
    [Fact]
    public void LateResponseWindowExamplesMatchTheContract()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(3000), ChestAccessRules.LateResponseWindow(1500));
        Assert.Equal(TimeSpan.FromMilliseconds(2), ChestAccessRules.LateResponseWindow(0));
    }
}
