namespace BaseStorage.Core.Tests.PluginHelper;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Diagnostics;
using BaseStorage.Core.Tests.Patching;
using BaseStorage.Plugin.Patching;

/// <summary>
/// AC-6 and AC-8 (contract section 3, ADR-GWIN-030, ADR-GWIN-031): PatchSafety.Run with an explicit state. The reporter is
/// static state, so the tests share the PatchSafetyStatic collection and reset the helper in Dispose (ADR-GWIN-021).
/// </summary>
[Collection("PatchSafetyStatic")]
public sealed class PatchSafetyStateTests : IDisposable
{
    private const string StateKey = "Fixture.Patch.RunState";

    public void Dispose()
    {
        PatchSafety.Initialize(null);
    }

    // GW-U-520
    [Fact]
    public void StatefulRunWithAStaticLambdaAllocatesNothingAndCallsOncePerInvocation()
    {
        PatchSafety.Initialize(new RateLimitedErrorReporter(new RecordingLogSink { DebugEnabled = true }));
        var state = new object();
        // One delegate instance serves both the warm-up and the measured loop. A second `static` lambda site would
        // allocate its cached delegate on first use, inside the measured window.
        Action<object> hit = static o => StateCounter.Hit(o);
        PatchSafety.Run(StateKey, state, hit);
        StateCounter.Reset();

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10; i++)
        {
            PatchSafety.Run(StateKey, state, hit);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0L, allocated);
        Assert.Equal(10, StateCounter.Calls);
        Assert.Same(state, StateCounter.Last);
    }

    // GW-U-524
    [Fact]
    public void AThrowingBodyIsReportedOnceAndTheRunReturnsNormally()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        PatchSafety.Initialize(new RateLimitedErrorReporter(sink));

        Assert.Null(Record.Exception(() => PatchSafety.Run("K", 41, (Action<int>)(static n => throw new InvalidOperationException("boom " + n)))));

        Assert.Single(sink.Entries, entry => entry.Severity == LogSeverity.Error
            && entry.Message.Contains("Patch 'K' failed and was skipped", StringComparison.Ordinal)
            && entry.Message.Contains("boom 41", StringComparison.Ordinal));

        int entriesBefore = sink.Entries.Count;
        Assert.Null(Record.Exception(() => PatchSafety.Run("K", 41, (Action<int>)(static n => throw new InvalidOperationException("boom " + n)))));
        Assert.Equal(entriesBefore, sink.Entries.Count);
    }

    // GW-U-524
    [Fact]
    public void ANullBodyIsReportedUnderItsKeyAndNeverRethrown()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        PatchSafety.Initialize(new RateLimitedErrorReporter(sink));

        Assert.Null(Record.Exception(() => PatchSafety.Run("K2", 1, (Action<int>)null!)));

        Assert.Single(sink.Entries, entry => entry.Severity == LogSeverity.Error
            && entry.Message.Contains("K2", StringComparison.Ordinal));
    }

    // GW-U-524
    [Fact]
    public void AThrowingBodyReturnsNormallyWithoutAReporter()
    {
        PatchSafety.Initialize(null);

        Assert.Null(Record.Exception(() => PatchSafety.Run("K", 41, (Action<int>)(static n => throw new InvalidOperationException("boom " + n)))));
    }

    // GW-U-524
    [Fact]
    public void ANonThrowingBodyRunsExactlyOnceWithTheGivenState()
    {
        var seen = new List<int>();

        PatchSafety.Run("K", 7, n => seen.Add(n));

        Assert.Equal(new[] { 7 }, seen.ToArray());
    }

    /// <summary>Records each call of the state-passing run so the allocation test can check the call count and the state.</summary>
    private static class StateCounter
    {
        public static int Calls { get; private set; }

        public static object? Last { get; private set; }

        public static void Hit(object state)
        {
            Calls++;
            Last = state;
        }

        public static void Reset()
        {
            Calls = 0;
            Last = null;
        }
    }
}
