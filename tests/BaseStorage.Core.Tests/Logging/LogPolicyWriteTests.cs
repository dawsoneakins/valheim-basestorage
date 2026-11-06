namespace BaseStorage.Core.Tests.Logging;

using System;
using BaseStorage.Core.Abstractions;

/// <summary>
/// AC-1 classification (log-policy-contract.md v1, sections 3.2 and 3.3): Write gives one entry per call with the mapped
/// severity, and IsEnabled asks the sink once. Texts are the section 2 examples, verbatim (ADR-GWIN-023).
/// </summary>
public sealed class LogPolicyWriteTests
{
    // GW-U-203
    [Theory]
    [InlineData(0, LogSeverity.Info, "Dons-QOL 0.1.0 loaded")]
    [InlineData(1, LogSeverity.Info, "Config synced from host: 6 host-synced settings applied")]
    [InlineData(2, LogSeverity.Info, "Organize: moved 412 items in 37 stacks across 9 chests")]
    [InlineData(3, LogSeverity.Info, "Deposit: stored 128 items in 4 chests, 2 favourite stacks kept")]
    [InlineData(4, LogSeverity.Debug, "move 30 CopperOre 3141592653:42 -> 3141592653:57")]
    [InlineData(5, LogSeverity.Debug, "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch' applied")]
    [InlineData(6, LogSeverity.Debug, "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch.Postfix' stack trace: System.NullReferenceException: Object reference not set to an instance of an object. at BaseStorage.Plugin.Patches.ContainerHoverPatch.Postfix(Container __instance)")]
    [InlineData(7, LogSeverity.Debug, "[station.actions.organize-button] template missing; control not created")]
    [InlineData(8, LogSeverity.Debug, "Base cache rebuilt: 12 stations, 48 containers")]
    public void WriteGivesOneEntryWithTheMappedSeverityAndTheSameMessageInstance(int category, LogSeverity expected, string text)
    {
        var sink = new RecordingSink();

        LogPolicyApi.Write(sink, category, text);

        Assert.Single(sink.Entries);
        Assert.Equal(expected, sink.Entries[0].Severity);
        Assert.Same(text, sink.Entries[0].Message);
        Assert.Empty(sink.IsEnabledCalls);
    }

    // GW-U-204
    [Fact]
    public void WriteEdgeRowsGiveExactlyOneEntryEachAndNeverThrow()
    {
        var sink = new RecordingSink();

        LogPolicyApi.Write(sink, 99, "unclassified");
        LogPolicyApi.Write(sink, Cat.MoveDetail, null);
        LogPolicyApi.Write(sink, Cat.OrganizeSummary, string.Empty);
        LogPolicyApi.Write(sink, Cat.Detail, "line one\nline two");

        Assert.Equal(
            new[]
            {
                (LogSeverity.Debug, "unclassified"),
                (LogSeverity.Debug, string.Empty),
                (LogSeverity.Info, string.Empty),
                (LogSeverity.Debug, "line one\nline two"),
            },
            sink.Entries);
    }

    // GW-U-204
    [Fact]
    public void WriteToANullSinkReturnsNormally()
    {
        LogPolicyApi.Write(null, Cat.Load, "Dons-QOL 0.1.0 loaded");
    }

    // GW-U-204
    [Fact]
    public void WriteToAThrowingSinkMakesOneAttemptAndReturnsNormally()
    {
        var sink = new RecordingSink { ThrowOnLog = true };

        LogPolicyApi.Write(sink, Cat.Load, "Dons-QOL 0.1.0 loaded");

        Assert.Equal(1, sink.LogCalls);
    }

    // GW-U-205
    [Fact]
    public void IsEnabledAsksTheSinkOnceWithTheMappedSeverity()
    {
        var debugOff = new RecordingSink { Enabled = s => s != LogSeverity.Debug };
        Assert.False(LogPolicyApi.IsEnabled(debugOff, Cat.MoveDetail));
        Assert.Equal(new[] { LogSeverity.Debug }, debugOff.IsEnabledCalls);

        var debugOn = new RecordingSink { Enabled = _ => true };
        Assert.True(LogPolicyApi.IsEnabled(debugOn, Cat.MoveDetail));
        Assert.Equal(new[] { LogSeverity.Debug }, debugOn.IsEnabledCalls);

        var infoOn = new RecordingSink { Enabled = s => s == LogSeverity.Info };
        Assert.True(LogPolicyApi.IsEnabled(infoOn, Cat.OrganizeSummary));
        Assert.Equal(new[] { LogSeverity.Info }, infoOn.IsEnabledCalls);

        var undefinedOff = new RecordingSink { Enabled = s => s != LogSeverity.Debug };
        Assert.False(LogPolicyApi.IsEnabled(undefinedOff, 42));
        Assert.Equal(new[] { LogSeverity.Debug }, undefinedOff.IsEnabledCalls);
    }

    // GW-U-205
    [Fact]
    public void IsEnabledIsFalseForANullSinkAndForAThrowingSinkWithoutAnException()
    {
        Assert.False(LogPolicyApi.IsEnabled(null, Cat.Load));

        var throwing = new RecordingSink { ThrowOnIsEnabled = true };
        Assert.False(LogPolicyApi.IsEnabled(throwing, Cat.Load));
        Assert.Single(throwing.IsEnabledCalls);
    }
}
