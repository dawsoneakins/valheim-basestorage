namespace BaseStorage.Core.Tests.Restock.Run;

using System;
using BaseStorage.Core.Restock.Run;
using BaseStorage.Core.Tests.Organize.Running;
using static BaseStorage.Core.Tests.Restock.Run.RunFixtures;

/// <summary>GW-U-2612: the one result line and its validation (S-QR-002 contract 3.5 and 6.3).</summary>
public sealed class RestockRunTextTests
{
    /// <summary>GW-U-2612: each of the nine rows of 6.3 composes its line verbatim.</summary>
    /// <param name="toppedUpStacks">The topped-up stacks.</param>
    /// <param name="movedItems">The moved items.</param>
    /// <param name="leftBehind">The items left behind.</param>
    /// <param name="stopped">True when the run stopped early.</param>
    /// <param name="expected">The line.</param>
    [Theory]
    [InlineData(3, 42, 0, false, "Restocked 3 stacks, 42 items")]
    [InlineData(2, 60, 0, false, "Restocked 2 stacks, 60 items")]
    [InlineData(1, 1, 0, false, "Restocked 1 stack, 1 item")]
    [InlineData(1, 20, 40, false, "Restocked 1 stack, 20 items; 40 items left behind")]
    [InlineData(3, 42, 1, false, "Restocked 3 stacks, 42 items; 1 item left behind")]
    [InlineData(0, 0, 0, false, "Nothing to restock")]
    [InlineData(0, 0, 30, false, "Nothing restocked; 30 items left behind")]
    [InlineData(0, 0, 0, true, "Nothing restocked; stopped early")]
    [InlineData(1, 25, 15, true, "Restocked 1 stack, 25 items; 15 items left behind; stopped early")]
    public void RestockRunTextComposesExactlyOneLine(int toppedUpStacks, int movedItems, int leftBehind, bool stopped, string expected)
    {
        Assert.Equal(expected, RestockRunText.Summary(toppedUpStacks, movedItems, leftBehind, stopped));
    }

    /// <summary>GW-U-2612: bad arguments throw the contracted exception and name; the result overload equals the four-argument form.</summary>
    [Fact]
    public void RestockRunTextRejectsBadArgumentsAndMatchesTheResultOverload()
    {
        Assert.Equal("toppedUpStacks", Assert.Throws<ArgumentException>(() => { _ = RestockRunText.Summary(0, 5, 0, false); }).ParamName);
        Assert.Equal("toppedUpStacks", Assert.Throws<ArgumentOutOfRangeException>(() => { _ = RestockRunText.Summary(-1, 0, 0, false); }).ParamName);
        Assert.Equal("movedItems", Assert.Throws<ArgumentOutOfRangeException>(() => { _ = RestockRunText.Summary(0, -1, 0, false); }).ParamName);
        Assert.Equal("leftBehind", Assert.Throws<ArgumentOutOfRangeException>(() => { _ = RestockRunText.Summary(0, 0, -1, false); }).ParamName);
        Assert.Equal("result", Assert.Throws<ArgumentNullException>(() => { _ = RestockRunText.Summary(null!); }).ParamName);

        RunHarness harness = Harness();
        harness.Port.Carried = new System.Collections.Generic.List<BaseStorage.Core.Restock.CarriedStack>
        {
            BaseStorage.Core.Tests.Restock.RestockFixtures.H(BaseStorage.Core.Tests.Restock.RestockFixtures.S("Wood", 10)),
        };
        var clock = new BaseStorage.Core.Tests.Reservations.ManualClock(TimeSpan.FromSeconds(3600));
        var layer = new FakeRestockLayer(harness.Port.Carried!, Chests());
        var executor = new BaseStorage.Core.Restock.Run.RestockExecutor(
            Plan(harness.Port.Carried!, Chests()),
            layer,
            clock,
            new BaseStorage.Core.Settings.BaseStorageSettings(new BaseStorage.Core.Tests.Settings.FakeSettingsSource()));
        while (!executor.IsComplete)
        {
            executor.Step();
            clock.Advance(TimeSpan.FromMilliseconds(100));
        }

        var result = executor.Result;
        Assert.Equal(
            RestockRunText.Summary(result.ToppedUpStacks, result.MovedItems, result.LeftBehind, result.Status == BaseStorage.Core.Organize.Execution.ExecutionStatus.Stopped),
            RestockRunText.Summary(result));
    }
}
