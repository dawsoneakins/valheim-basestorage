namespace BaseStorage.Core.Tests.Logging;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;

/// <summary>
/// AC-2 gate behaviour (log-policy-contract.md v1, sections 4.1, 4.3): the gate forwards Info and Warning and Error unchanged,
/// drops Debug when the live flag is off, and never throws (ADR-GWIN-022, ADR-GWIN-023).
/// </summary>
public sealed class DebugGatedLogSinkTests
{
    private const string WarningText = "Base station cap reached: 64 stations, using the stations found so far";

    private const string ErrorText =
        "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch.Postfix' failed and was skipped: System.NullReferenceException: Object reference not set to an instance of an object. (repeats suppressed this session)";

    /// <summary>The 4.1 cells: each category's section 2 text, for both flag values.</summary>
    public static IEnumerable<object[]> Section41Cells()
    {
        foreach (bool flag in new[] { false, true })
        {
            yield return new object[] { Cat.Load, LogSeverity.Info, "BaseStorage 0.1.0 loaded", flag };
            yield return new object[] { Cat.ConfigSync, LogSeverity.Info, "Config synced from host: 6 host-synced settings applied", flag };
            yield return new object[] { Cat.OrganizeSummary, LogSeverity.Info, "Organize: moved 412 items in 37 stacks across 9 chests", flag };
            yield return new object[] { Cat.DepositSummary, LogSeverity.Info, "Deposit: stored 128 items in 4 chests, 2 favourite stacks kept", flag };
            yield return new object[] { Cat.MoveDetail, LogSeverity.Debug, "move 30 CopperOre 3141592653:42 -> 3141592653:57", flag };
            yield return new object[] { Cat.PatchDetail, LogSeverity.Debug, "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch' applied", flag };
            yield return new object[] { Cat.SafetyStackTrace, LogSeverity.Debug, "Patch 'X' stack trace: System.Exception: boom", flag };
            yield return new object[] { Cat.UiFallback, LogSeverity.Debug, "[station.actions.organize-button] template missing; control not created", flag };
            yield return new object[] { Cat.Detail, LogSeverity.Debug, "Base cache rebuilt: 12 stations, 48 containers", flag };
            yield return new object[] { 99, LogSeverity.Debug, "unclassified", flag };
        }
    }

    // GW-U-206
    [Theory]
    [MemberData(nameof(Section41Cells))]
    public void GateRecordsExactlyTheSection41CellForEachCategoryAndFlag(int category, LogSeverity severity, string text, bool flag)
    {
        var inner = new RecordingSink();
        var accessor = new CountingAccessor(flag);
        ILogSink gate = LogPolicyApi.Gate(inner, accessor.AsFunc);

        LogPolicyApi.Write(gate, category, text);

        bool recorded = severity == LogSeverity.Info || flag;
        Assert.Equal(recorded ? new[] { (severity, text) } : Array.Empty<(LogSeverity, string)>(), inner.Entries);
        Assert.Equal(recorded, LogPolicyApi.IsEnabled(gate, category));
    }

    // GW-U-207
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonDebugSeveritiesPassThroughUnchangedAndNeverReadTheAccessor(bool flag)
    {
        var inner = new RecordingSink();
        var accessor = new CountingAccessor(flag);
        ILogSink gate = LogPolicyApi.Gate(inner, accessor.AsFunc);

        gate.Log(LogSeverity.Warning, WarningText);
        gate.Log(LogSeverity.Error, ErrorText);
        gate.Log((LogSeverity)7, "x");
        gate.Log((LogSeverity)(-1), "y");
        gate.Log(LogSeverity.Info, null!);

        Assert.Equal(
            new[]
            {
                (LogSeverity.Warning, WarningText),
                (LogSeverity.Error, ErrorText),
                ((LogSeverity)7, "x"),
                ((LogSeverity)(-1), "y"),
                (LogSeverity.Info, string.Empty),
            },
            inner.Entries);
        Assert.Equal(0, accessor.Calls);
        Assert.True(gate.IsEnabled(LogSeverity.Warning));
        Assert.True(gate.IsEnabled(LogSeverity.Error));
        Assert.Equal(0, accessor.Calls);
    }

    // GW-U-207
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NullDebugMessageIsRecordedAsEmptyOnlyWhenDebugIsOnAndReadsTheAccessorOnce(bool flag)
    {
        var inner = new RecordingSink();
        var accessor = new CountingAccessor(flag);
        ILogSink gate = LogPolicyApi.Gate(inner, accessor.AsFunc);

        gate.Log(LogSeverity.Debug, null!);

        Assert.Equal(
            flag ? new[] { (LogSeverity.Debug, string.Empty) } : Array.Empty<(LogSeverity, string)>(),
            inner.Entries);
        Assert.Equal(1, accessor.Calls);

        int before = accessor.Calls;
        bool enabled = gate.IsEnabled(LogSeverity.Debug);
        Assert.Equal(flag, enabled);
        Assert.Equal(1, accessor.Calls - before);
    }

    // GW-U-210
    [Fact]
    public void ThrowingAccessorMakesIsEnabledDebugFalseWithoutAnException()
    {
        var inner = new RecordingSink();
        var accessor = new CountingAccessor { Throws = new InvalidOperationException("accessor failed") };
        ILogSink gate = LogPolicyApi.Gate(inner, accessor.AsFunc);

        Assert.False(gate.IsEnabled(LogSeverity.Debug));
    }

    // GW-U-210
    [Fact]
    public void ThrowingAccessorDropsDebugWritesButInfoIsStillRecordedWithoutTheAccessor()
    {
        var inner = new RecordingSink();
        var accessor = new CountingAccessor { Throws = new InvalidOperationException("accessor failed") };
        ILogSink gate = LogPolicyApi.Gate(inner, accessor.AsFunc);

        gate.Log(LogSeverity.Debug, "move 1 Wood 1:1 -> 1:2");
        Assert.Empty(inner.Entries);

        int callsBeforeInfo = accessor.Calls;
        gate.Log(LogSeverity.Info, "BaseStorage 0.1.0 loaded");
        Assert.Equal(new[] { (LogSeverity.Info, "BaseStorage 0.1.0 loaded") }, inner.Entries);
        Assert.Equal(callsBeforeInfo, accessor.Calls);
    }

    // GW-U-210
    [Fact]
    public void CountingAccessorIsInvokedOncePerDebugWriteWithNoCaching()
    {
        var inner = new RecordingSink();
        var accessor = new CountingAccessor(true);
        ILogSink gate = LogPolicyApi.Gate(inner, accessor.AsFunc);

        gate.Log(LogSeverity.Debug, "a");
        gate.Log(LogSeverity.Debug, "a");
        gate.Log(LogSeverity.Debug, "a");

        Assert.Equal(
            new[] { (LogSeverity.Debug, "a"), (LogSeverity.Debug, "a"), (LogSeverity.Debug, "a") },
            inner.Entries);
        Assert.Equal(3, accessor.Calls);
    }

    // GW-U-210
    [Fact]
    public void InnerDebugDisabledMakesIsEnabledDebugFalseEvenWhenTheFlagIsTrue()
    {
        var inner = new RecordingSink { Enabled = s => s != LogSeverity.Debug };
        var accessor = new CountingAccessor(true);
        ILogSink gate = LogPolicyApi.Gate(inner, accessor.AsFunc);

        Assert.False(gate.IsEnabled(LogSeverity.Debug));
    }

    // GW-U-210
    [Fact]
    public void InnerLogThrowingIsSwallowedForDebugAndError()
    {
        var inner = new RecordingSink { ThrowOnLog = true };
        var accessor = new CountingAccessor(true);
        ILogSink gate = LogPolicyApi.Gate(inner, accessor.AsFunc);

        gate.Log(LogSeverity.Debug, "a");
        gate.Log(LogSeverity.Error, "b");

        Assert.Equal(2, inner.LogCalls);
    }

    // GW-U-210
    [Fact]
    public void InnerIsEnabledThrowingMakesIsEnabledInfoFalseWithoutAnException()
    {
        var inner = new RecordingSink { ThrowOnIsEnabled = true };
        var accessor = new CountingAccessor(true);
        ILogSink gate = LogPolicyApi.Gate(inner, accessor.AsFunc);

        Assert.False(gate.IsEnabled(LogSeverity.Info));
    }

    // GW-U-210
    [Fact]
    public void ConstructorRejectsANullInnerSinkWithItsParamName()
    {
        var accessor = new CountingAccessor(true);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => LogPolicyApi.Gate(null, accessor.AsFunc));

        Assert.Equal("inner", ex.ParamName);
    }

    // GW-U-210
    [Fact]
    public void ConstructorRejectsANullAccessorWithItsParamName()
    {
        var inner = new RecordingSink();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => LogPolicyApi.Gate(inner, null));

        Assert.Equal("debugEnabled", ex.ParamName);
    }

    // GW-U-210
    [Fact]
    public void BuildingAGateCallsNeitherTheAccessorNorTheInnerSink()
    {
        var inner = new RecordingSink();
        var accessor = new CountingAccessor(true);

        _ = LogPolicyApi.Gate(inner, accessor.AsFunc);

        Assert.Equal(0, accessor.Calls);
        Assert.Empty(inner.Entries);
        Assert.Empty(inner.IsEnabledCalls);
        Assert.Equal(0, inner.LogCalls);
    }
}
