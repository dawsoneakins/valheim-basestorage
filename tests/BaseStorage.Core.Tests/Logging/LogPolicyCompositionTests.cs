namespace BaseStorage.Core.Tests.Logging;

using System;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Diagnostics;
using BaseStorage.Core.Patching;
using BaseStorage.Core.Tests.Patching;

/// <summary>
/// AC-2 composition (log-policy-contract.md v1, section 4.4): the existing Core loggers, driven through the gate, keep their
/// severities and texts. The Debug lines follow the live flag, and no Info line appears (ADR-GWIN-022, ADR-GWIN-023).
/// </summary>
public sealed class LogPolicyCompositionTests
{
    private const string HoverPatch = "BaseStorage.Plugin.Patches.ContainerHoverPatch";
    private const string StationPatch = "BaseStorage.Plugin.Patches.CraftingStationRangePatch";

    // GW-U-211
    [Fact]
    public void ErrorReporterWritesItsStackTraceOnlyWhenDebugIsOnAndNeverWritesInfo()
    {
        const string patchKey = "BaseStorage.Plugin.Patches.ContainerHoverPatch.Postfix";

        var off = new RecordingSink();
        var offReporter = new RateLimitedErrorReporter(LogPolicyApi.Gate(off, () => false));
        offReporter.Report(patchKey, new NullReferenceException());
        Assert.Single(off.Entries);
        Assert.Equal(LogSeverity.Error, off.Entries[0].Severity);
        Assert.StartsWith("Patch '" + patchKey + "' failed and was skipped: ", off.Entries[0].Message, StringComparison.Ordinal);

        var on = new RecordingSink();
        var onReporter = new RateLimitedErrorReporter(LogPolicyApi.Gate(on, () => true));
        onReporter.Report(patchKey, new NullReferenceException());
        Assert.Equal(
            new[] { LogSeverity.Error, LogSeverity.Debug },
            on.Entries.Select(e => e.Severity));
        Assert.StartsWith("Patch '" + patchKey + "' failed and was skipped: ", on.Entries[0].Message, StringComparison.Ordinal);
        Assert.StartsWith("Patch '" + patchKey + "' stack trace: ", on.Entries[1].Message, StringComparison.Ordinal);

        Assert.DoesNotContain(off.Entries, e => e.Severity == LogSeverity.Info);
        Assert.DoesNotContain(on.Entries, e => e.Severity == LogSeverity.Info);
    }

    // GW-U-211
    [Fact]
    public void PatchAppliedLinesAppearOnlyWhenDebugIsOnAndNeverAsInfo()
    {
        var off = new RecordingSink();
        PatchApplier.ApplyAll(
            new IPatchUnit[] { new FakePatch(HoverPatch), new FakePatch(StationPatch) },
            LogPolicyApi.Gate(off, () => false));
        Assert.Empty(off.Entries);

        var on = new RecordingSink();
        PatchApplier.ApplyAll(
            new IPatchUnit[] { new FakePatch(HoverPatch), new FakePatch(StationPatch) },
            LogPolicyApi.Gate(on, () => true));
        Assert.Equal(
            new[]
            {
                (LogSeverity.Debug, "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch' applied"),
                (LogSeverity.Debug, "Patch 'BaseStorage.Plugin.Patches.CraftingStationRangePatch' applied"),
            },
            on.Entries);
    }
}
