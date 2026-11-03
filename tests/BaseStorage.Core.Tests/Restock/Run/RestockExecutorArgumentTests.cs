namespace BaseStorage.Core.Tests.Restock.Run;

using System;
using System.Linq;
using BaseStorage.Core.Organize.Execution;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Restock.Run;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Tests.Settings;
using static BaseStorage.Core.Tests.Restock.Run.RunFixtures;

/// <summary>GW-U-2599: the executor's constructor and inert-when-complete surface (S-QR-002 contract 3.3).</summary>
public sealed class RestockExecutorArgumentTests
{
    /// <summary>GW-U-2599: each null argument throws ArgumentNullException in order, the constructor reads nothing from the layer, a complete executor steps to 0, and Abort is false once complete.</summary>
    [Fact]
    public void RestockExecutorValidatesAndIsInertWhenComplete()
    {
        var clock = new ManualClock(TimeSpan.FromSeconds(3600));
        var settings = new BaseStorageSettings(new FakeSettingsSource());
        RestockPlan plan = Plan(Carried(), Chests());
        var layer = new FakeRestockLayer(Carried(), Chests());

        Assert.Equal("plan", Assert.Throws<ArgumentNullException>(() => { _ = new RestockExecutor(null!, layer, clock, settings); }).ParamName);
        Assert.Equal("layer", Assert.Throws<ArgumentNullException>(() => { _ = new RestockExecutor(plan, null!, clock, settings); }).ParamName);
        Assert.Equal("clock", Assert.Throws<ArgumentNullException>(() => { _ = new RestockExecutor(plan, layer, null!, settings); }).ParamName);
        Assert.Equal("settings", Assert.Throws<ArgumentNullException>(() => { _ = new RestockExecutor(plan, layer, clock, null!); }).ParamName);
        Assert.Empty(layer.Calls);

        var executor = new RestockExecutor(plan, layer, clock, settings);
        Assert.Empty(layer.Calls);
        Assert.False(executor.IsComplete && executor.Status == ExecutionStatus.Running);
        while (!executor.IsComplete)
        {
            executor.Step();
            clock.Advance(TimeSpan.FromMilliseconds(100));
        }

        int calls = layer.Calls.Count;
        Assert.Equal(0, executor.Step());
        Assert.Equal(calls, layer.Calls.Count);
        Assert.False(executor.Abort());
        Assert.Equal(calls, layer.Calls.Count);
    }

    /// <summary>GW-U-2599: Abort(reason) throws ArgumentOutOfRangeException("reason") for None, ConfirmFailed, RollbackFailed and an undefined value.</summary>
    /// <param name="reason">The rejected reason.</param>
    [Theory]
    [InlineData(RestockStopReason.None)]
    [InlineData(RestockStopReason.ConfirmFailed)]
    [InlineData(RestockStopReason.RollbackFailed)]
    [InlineData((RestockStopReason)9)]
    public void AbortRejectsReasonsOtherThanLockLostAndAborted(RestockStopReason reason)
    {
        var clock = new ManualClock(TimeSpan.FromSeconds(3600));
        var layer = new FakeRestockLayer(Carried(), Chests());
        var executor = new RestockExecutor(Plan(Carried(), Chests()), layer, clock, new BaseStorageSettings(new FakeSettingsSource()));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => { _ = executor.Abort(reason); });
        Assert.Equal("reason", ex.ParamName);
    }
}
