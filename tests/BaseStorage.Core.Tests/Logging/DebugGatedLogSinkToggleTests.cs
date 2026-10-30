namespace BaseStorage.Core.Tests.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Diagnostics;
using BaseStorage.Core.Tests.Settings;

/// <summary>
/// AC-2 runtime toggle (log-policy-contract.md v1, section 4.2 and L4, L6): one gate instance, no restart, no caching.
/// The sequence runs once with a captured bool and once through the real BaseStorageSettings read path (ADR-GWIN-023).
/// </summary>
public sealed class DebugGatedLogSinkToggleTests
{
    private const string MoveTwelve = "move 12 CopperOre 3141592653:42 -> 3141592653:57";
    private const string MoveThirty = "move 30 CopperOre 3141592653:42 -> 3141592653:57";
    private const string MoveSeventeen = "move 17 Wood 3141592653:42 -> 3141592653:58";
    private const string OrganizeText = "Organize: moved 42 items in 2 stacks across 2 chests";
    private const string DepositText = "Deposit: stored 17 items in 1 chest, 0 favourite stacks kept";

    /// <summary>A flag that another thread can flip while writers read it.</summary>
    private sealed class SharedFlag
    {
        public volatile bool Value;
    }

    // GW-U-208
    [Fact]
    public void TogglingTheCapturedFlagTakesEffectOnTheNextCallWithoutANewGate()
    {
        bool flag = false;
        var inner = new RecordingSink();
        ILogSink gate = LogPolicyApi.Gate(inner, () => flag);

        flag = false;
        LogPolicyApi.Write(gate, Cat.MoveDetail, MoveThirty);
        Assert.False(gate.IsEnabled(LogSeverity.Debug));
        Assert.Empty(inner.Entries);

        flag = true;
        LogPolicyApi.Write(gate, Cat.MoveDetail, MoveTwelve);
        Assert.True(gate.IsEnabled(LogSeverity.Debug));
        Assert.Equal(new[] { (LogSeverity.Debug, MoveTwelve) }, inner.Entries);

        LogPolicyApi.Write(gate, Cat.OrganizeSummary, OrganizeText);
        Assert.True(gate.IsEnabled(LogSeverity.Debug));
        Assert.Equal(
            new[] { (LogSeverity.Debug, MoveTwelve), (LogSeverity.Info, OrganizeText) },
            inner.Entries);

        flag = false;
        LogPolicyApi.Write(gate, Cat.MoveDetail, MoveSeventeen);
        Assert.False(gate.IsEnabled(LogSeverity.Debug));
        Assert.Equal(
            new[] { (LogSeverity.Debug, MoveTwelve), (LogSeverity.Info, OrganizeText) },
            inner.Entries);

        LogPolicyApi.Write(gate, Cat.DepositSummary, DepositText);
        Assert.False(gate.IsEnabled(LogSeverity.Debug));
        Assert.Equal(
            new[]
            {
                (LogSeverity.Debug, MoveTwelve),
                (LogSeverity.Info, OrganizeText),
                (LogSeverity.Info, DepositText),
            },
            inner.Entries);
    }

    // GW-U-209
    [Fact]
    public void TogglingThroughTheRealSettingsReadPathGivesTheSameEntriesWithNoNewObjects()
    {
        var source = new FakeSettingsSource();
        var settings = new BaseStorageSettings(source);
        var inner = new RecordingSink();
        ILogSink gate = LogPolicyApi.Gate(inner, () => settings.DebugLogging);

        // Step 1: no value in the source, so the catalog default false suppresses Debug.
        int before = source.BoolCalls;
        LogPolicyApi.Write(gate, Cat.MoveDetail, MoveThirty);
        Assert.Equal(before + 1, source.BoolCalls);
        Assert.False(gate.IsEnabled(LogSeverity.Debug));
        Assert.Empty(inner.Entries);

        // Step 2: true enables Debug on the next call.
        source.WithBool(SettingKeys.DebugLogging, true);
        LogPolicyApi.Write(gate, Cat.MoveDetail, MoveTwelve);
        Assert.True(gate.IsEnabled(LogSeverity.Debug));
        Assert.Equal(new[] { (LogSeverity.Debug, MoveTwelve) }, inner.Entries);

        // Step 3: an Info write reads the source zero times.
        before = source.BoolCalls;
        LogPolicyApi.Write(gate, Cat.OrganizeSummary, OrganizeText);
        Assert.Equal(before, source.BoolCalls);
        Assert.Equal(
            new[] { (LogSeverity.Debug, MoveTwelve), (LogSeverity.Info, OrganizeText) },
            inner.Entries);

        // Step 4: false suppresses Debug again.
        source.WithBool(SettingKeys.DebugLogging, false);
        LogPolicyApi.Write(gate, Cat.MoveDetail, MoveSeventeen);
        Assert.False(gate.IsEnabled(LogSeverity.Debug));
        Assert.Equal(
            new[] { (LogSeverity.Debug, MoveTwelve), (LogSeverity.Info, OrganizeText) },
            inner.Entries);

        // Step 5: Info still passes.
        LogPolicyApi.Write(gate, Cat.DepositSummary, DepositText);
        Assert.Equal(
            new[]
            {
                (LogSeverity.Debug, MoveTwelve),
                (LogSeverity.Info, OrganizeText),
                (LogSeverity.Info, DepositText),
            },
            inner.Entries);
    }

    // GW-U-209
    [Fact]
    public void EachDebugCallReadsTheSettingSourceExactlyOnceAndIsNeverCached()
    {
        var source = new FakeSettingsSource();
        var settings = new BaseStorageSettings(source);
        var inner = new RecordingSink();
        ILogSink gate = LogPolicyApi.Gate(inner, () => settings.DebugLogging);

        int before = source.BoolCalls;
        _ = gate.IsEnabled(LogSeverity.Debug);
        Assert.Equal(before + 1, source.BoolCalls);

        before = source.BoolCalls;
        gate.Log(LogSeverity.Debug, MoveTwelve);
        Assert.Equal(before + 1, source.BoolCalls);

        before = source.BoolCalls;
        gate.Log(LogSeverity.Info, OrganizeText);
        Assert.Equal(before, source.BoolCalls);
    }

    // GW-U-214
    [Fact]
    public async Task FlippingTheFlagDuringConcurrentWritesNeverThrowsAndKeepsEveryInfoLine()
    {
        var flag = new SharedFlag();
        var inner = new LockingRecordingLogSink { DebugEnabled = true };
        ILogSink gate = LogPolicyApi.Gate(inner, () => flag.Value);

        var tasks = new Task[4];
        for (int t = 0; t < tasks.Length; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    LogPolicyApi.Write(gate, Cat.MoveDetail, "m");
                }

                LogPolicyApi.Write(gate, Cat.OrganizeSummary, "s");
            });
        }

        for (int i = 0; i < 100; i++)
        {
            flag.Value = !flag.Value;
        }

        await Task.WhenAll(tasks);

        IReadOnlyList<(LogSeverity Severity, string Message)> entries = inner.Entries;
        Assert.Equal(4, entries.Count(e => e.Severity == LogSeverity.Info && e.Message == "s"));
        Assert.All(
            entries.Where(e => !(e.Severity == LogSeverity.Info && e.Message == "s")),
            e => Assert.Equal((LogSeverity.Debug, "m"), e));
    }
}
