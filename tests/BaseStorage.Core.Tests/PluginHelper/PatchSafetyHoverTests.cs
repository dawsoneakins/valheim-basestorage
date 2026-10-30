namespace BaseStorage.Core.Tests.PluginHelper;

using System;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Diagnostics;
using BaseStorage.Core.Tests.Patching;
using BaseStorage.Plugin.Patching;

/// <summary>
/// AC-1 (ADR-GWIN-032): the PatchSafety value overload keeps the vanilla text on failure and allocates nothing on success. It shares the
/// reporter with the other PatchSafety tests, so it uses the same collection and resets the helper in Dispose (ADR-GWIN-021).
/// </summary>
[Collection("PatchSafetyStatic")]
public sealed class PatchSafetyHoverTests : IDisposable
{
    public void Dispose()
    {
        PatchSafety.Initialize(null);
    }

    // GW-U-537 (AC-1): a successful body returns its result.
    [Fact]
    public void TheValueOverloadReturnsTheBodyResult()
    {
        PatchSafety.Initialize(new RateLimitedErrorReporter(new RecordingLogSink { DebugEnabled = true }));

        string result = PatchSafety.Run("K", 7, "vanilla", static (s, t) => t + s);

        Assert.Equal("vanilla7", result);
    }

    // GW-U-537 (AC-1): a throwing body returns the vanilla instance, reports once under the key, and a second failure writes nothing.
    [Fact]
    public void AThrowingBodyReturnsTheVanillaInstanceAndReportsOnce()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        PatchSafety.Initialize(new RateLimitedErrorReporter(sink));
        string current = "vanilla";
        var body = (Func<int, string, string>)(static (s, t) => throw new InvalidOperationException("boom " + s));

        string result = PatchSafety.Run("K", 7, current, body);

        Assert.Same(current, result);
        Assert.Single(sink.Entries, entry => entry.Severity == LogSeverity.Error
            && entry.Message.Contains("Patch 'K' failed and was skipped", StringComparison.Ordinal)
            && entry.Message.Contains("boom 7", StringComparison.Ordinal));

        int entriesBefore = sink.Entries.Count;
        string again = PatchSafety.Run("K", 7, current, body);

        Assert.Same(current, again);
        Assert.Equal(entriesBefore, sink.Entries.Count);
    }

    // GW-U-537 (AC-1): a null body is a failure, so the vanilla instance comes back and the null is reported once.
    [Fact]
    public void ANullBodyReturnsTheVanillaInstanceAndIsReportedOnce()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        PatchSafety.Initialize(new RateLimitedErrorReporter(sink));
        string current = "vanilla";

        string result = PatchSafety.Run<int, string>("K3", 7, current, null!);

        Assert.Same(current, result);
        Assert.Single(sink.Entries, entry => entry.Severity == LogSeverity.Error
            && entry.Message.Contains("K3", StringComparison.Ordinal));
    }

    // GW-U-537 (AC-1): after a warm-up, a successful static-lambda call allocates nothing outside the body.
    [Fact]
    public void ASuccessfulStaticLambdaAllocatesNothingOutsideTheBody()
    {
        PatchSafety.Initialize(new RateLimitedErrorReporter(new RecordingLogSink { DebugEnabled = true }));
        string current = "vanilla";
        Func<int, string, string> identity = static (s, t) => t;
        _ = PatchSafety.Run("K4", 7, current, identity);

        long before = GC.GetAllocatedBytesForCurrentThread();
        string last = current;
        for (int i = 0; i < 10; i++)
        {
            last = PatchSafety.Run("K4", 7, current, identity);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0L, allocated);
        Assert.Same(current, last);
    }
}
