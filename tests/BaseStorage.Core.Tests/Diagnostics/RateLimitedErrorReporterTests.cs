namespace BaseStorage.Core.Tests.Diagnostics;

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Diagnostics;
using BaseStorage.Core.Tests.Patching;

/// <summary>
/// AC-3..AC-6: RateLimitedErrorReporter logs the first failure per patch key at Error (with its stack trace at Debug),
/// suppresses repeats for the session, never throws, and is safe under concurrency. Expected text is copied from ADR-GWIN-020.
/// </summary>
public sealed class RateLimitedErrorReporterTests
{
    private const string Key = "BaseStorage.Plugin.Patches.ContainerHoverPatch.Postfix";
    private const string ErrorLine = "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch.Postfix' failed and was skipped: System.InvalidOperationException: boom (repeats suppressed this session)";
    private const string DebugPrefix = "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch.Postfix' stack trace: ";
    private const string ErrorPrefixA = "Patch 'X.Patch.Postfix' failed and was skipped: ";
    private const string ErrorPrefixB = "Patch 'X.Patch.Prefix' failed and was skipped: ";

    // GW-U-120
    [Fact]
    public void FirstReportWithDebugOnWritesTheErrorLineAndTheStackTraceLine()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        var reporter = new RateLimitedErrorReporter(sink);

        bool wrote = reporter.Report(Key, ThrowFromNamedFrame());

        Assert.True(wrote);
        Assert.Equal(2, sink.Entries.Count);
        Assert.Equal(LogSeverity.Error, sink.Entries[0].Severity);
        Assert.Equal(ErrorLine, sink.Entries[0].Message);

        Assert.Equal(LogSeverity.Debug, sink.Entries[1].Severity);
        string debug = sink.Entries[1].Message;
        Assert.StartsWith(DebugPrefix + "System.InvalidOperationException: boom", debug, StringComparison.Ordinal);
        Assert.Contains("ThrowFromNamedFrame", debug, StringComparison.Ordinal);
        Assert.Equal(-1, debug.IndexOfAny(new[] { '\r', '\n', '\u0085', (char)0x2028, (char)0x2029 }));
    }

    // GW-U-121
    [Fact]
    public void FirstReportWithDebugOffWritesOnlyTheErrorLine()
    {
        var sink = new RecordingLogSink { DebugEnabled = false };
        var reporter = new RateLimitedErrorReporter(sink);

        bool wrote = reporter.Report(Key, ThrowFromNamedFrame());

        Assert.True(wrote);
        var entry = Assert.Single(sink.Entries);
        Assert.Equal(LogSeverity.Error, entry.Severity);
        Assert.Equal(ErrorLine, entry.Message);
        Assert.Contains(LogSeverity.Debug, sink.IsEnabledCalls);
        Assert.DoesNotContain(LogSeverity.Error, sink.IsEnabledCalls);
    }

    // GW-U-121
    [Fact]
    public void ErrorLineIsStillWrittenWhenEveryLevelIsDisabled()
    {
        var sink = new RecordingLogSink { DisableAll = true };
        var reporter = new RateLimitedErrorReporter(sink);

        bool wrote = reporter.Report(Key, ThrowFromNamedFrame());

        Assert.True(wrote);
        var entry = Assert.Single(sink.Entries);
        Assert.Equal(LogSeverity.Error, entry.Severity);
        Assert.Equal(ErrorLine, entry.Message);
    }

    // GW-U-122 (a)
    [Fact]
    public void InnerExceptionIsAppendedWithTheArrowSeparator()
    {
        var sink = new RecordingLogSink();
        var reporter = new RateLimitedErrorReporter(sink);
        var error = new InvalidOperationException("outer", new FormatException("inner"));

        reporter.Report(Key, error);

        Assert.Equal(
            "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch.Postfix' failed and was skipped: System.InvalidOperationException: outer ---> System.FormatException: inner (repeats suppressed this session)",
            sink.Entries[0].Message);
    }

    // GW-U-122 (b)
    [Fact]
    public void MultiLineMessageIsFlattenedToOneLine()
    {
        var sink = new RecordingLogSink();
        var reporter = new RateLimitedErrorReporter(sink);

        reporter.Report(Key, new InvalidOperationException("line one\r\nline two"));

        Assert.Equal(
            "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch.Postfix' failed and was skipped: System.InvalidOperationException: line one line two (repeats suppressed this session)",
            sink.Entries[0].Message);
    }

    // GW-U-122 (c)
    [Fact]
    public void EmptyMessageGivesTheTypeNameWithoutAColon()
    {
        var sink = new RecordingLogSink();
        var reporter = new RateLimitedErrorReporter(sink);

        reporter.Report(Key, new InvalidOperationException(string.Empty));

        Assert.Equal(
            "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch.Postfix' failed and was skipped: System.InvalidOperationException (repeats suppressed this session)",
            sink.Entries[0].Message);
    }

    // GW-U-122 (d)
    [Fact]
    public void KeyLineBreaksAreFlattenedInTheDisplayedName()
    {
        var sink = new RecordingLogSink();
        var reporter = new RateLimitedErrorReporter(sink);

        reporter.Report("A.B\r\nC", ThrowFromNamedFrame());

        Assert.Equal(
            "Patch 'A.B C' failed and was skipped: System.InvalidOperationException: boom (repeats suppressed this session)",
            sink.Entries[0].Message);
    }

    // GW-U-122 (e)
    [Fact]
    public void NullEmptyAndBlankKeysShareOneUnnamedSlot()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        var reporter = new RateLimitedErrorReporter(sink);
        var error = new InvalidOperationException("boom");

        Assert.True(reporter.Report(null, error));
        Assert.False(reporter.Report(string.Empty, error));
        Assert.False(reporter.Report("   ", error));

        Assert.Equal(
            "Patch '<unnamed patch>' failed and was skipped: System.InvalidOperationException: boom (repeats suppressed this session)",
            sink.Entries[0].Message);
        Assert.Equal(2, sink.Entries.Count);
    }

    // GW-U-122 (f)
    [Fact]
    public void ExceptionWhoseMessageGetterThrowsIsLoggedAsItsTypeNameOnly()
    {
        var sink = new RecordingLogSink();
        var reporter = new RateLimitedErrorReporter(sink);

        reporter.Report(Key, new MessageThrowingException());

        Assert.Equal(
            "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch.Postfix' failed and was skipped: BaseStorage.Core.Tests.Patching.MessageThrowingException (repeats suppressed this session)",
            sink.Entries[0].Message);
    }

    // GW-U-122 (g)
    [Fact]
    public void ExceptionThatWasNeverThrownStillGetsOneDebugLineWithItsTypeName()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        var reporter = new RateLimitedErrorReporter(sink);

        reporter.Report(Key, new InvalidOperationException("boom"));

        Assert.Equal(2, sink.Entries.Count);
        Assert.Equal(LogSeverity.Debug, sink.Entries[1].Severity);
        Assert.StartsWith(DebugPrefix, sink.Entries[1].Message, StringComparison.Ordinal);
        Assert.Contains("System.InvalidOperationException", sink.Entries[1].Message, StringComparison.Ordinal);
    }

    // GW-U-123
    [Fact]
    public void RepeatsForTheSameKeyAreSuppressedAtEveryLevel()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        var reporter = new RateLimitedErrorReporter(sink);
        Assert.True(reporter.Report(Key, new InvalidOperationException("first")));
        Assert.Equal(2, sink.Entries.Count);
        int consultedBefore = sink.IsEnabledCalls.Count;

        for (int i = 0; i < 99; i++)
        {
            Assert.False(reporter.Report(Key, VaryingException(i)));
        }

        Assert.Equal(2, sink.Entries.Count);
        Assert.Equal(consultedBefore, sink.IsEnabledCalls.Count);
    }

    // GW-U-125
    [Fact]
    public void DifferentKeysAreRateLimitedIndependently()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        var reporter = new RateLimitedErrorReporter(sink);
        var error = new InvalidOperationException("boom");

        Assert.True(reporter.Report("X.Patch.Postfix", error));
        Assert.True(reporter.Report("X.Patch.Prefix", error));
        Assert.False(reporter.Report("X.Patch.Postfix", error));
        Assert.False(reporter.Report("X.Patch.Prefix", error));

        Assert.Equal(4, sink.Entries.Count);
        Assert.Equal(LogSeverity.Error, sink.Entries[0].Severity);
        Assert.StartsWith(ErrorPrefixA, sink.Entries[0].Message, StringComparison.Ordinal);
        Assert.Equal(LogSeverity.Debug, sink.Entries[1].Severity);
        Assert.StartsWith("Patch 'X.Patch.Postfix' stack trace: ", sink.Entries[1].Message, StringComparison.Ordinal);
        Assert.Equal(LogSeverity.Error, sink.Entries[2].Severity);
        Assert.StartsWith(ErrorPrefixB, sink.Entries[2].Message, StringComparison.Ordinal);
        Assert.Equal(LogSeverity.Debug, sink.Entries[3].Severity);
        Assert.StartsWith("Patch 'X.Patch.Prefix' stack trace: ", sink.Entries[3].Message, StringComparison.Ordinal);
    }

    // GW-U-125
    [Fact]
    public void KeysAreCaseSensitiveAndOrdinal()
    {
        var sink = new RecordingLogSink { DebugEnabled = false };
        var reporter = new RateLimitedErrorReporter(sink);
        var error = new InvalidOperationException("boom");

        Assert.True(reporter.Report("X.Patch.Postfix", error));
        Assert.True(reporter.Report("x.patch.postfix", error));
        Assert.Equal(2, sink.Entries.Count);
    }

    // GW-U-126
    [Fact]
    public void ConcurrentReportsLogOncePerKeyAndNeverThrow()
    {
        var sink = new LockingRecordingLogSink { DebugEnabled = true };
        var reporter = new RateLimitedErrorReporter(sink);
        var error = new InvalidOperationException("boom");
        int trueCount = 0;

        Exception? failure = Record.Exception(() => Parallel.For(
            0,
            4000,
            new ParallelOptions { MaxDegreeOfParallelism = 8 },
            i =>
            {
                if (reporter.Report("K" + (i % 8).ToString(), error))
                {
                    Interlocked.Increment(ref trueCount);
                }
            }));

        Assert.Null(failure);
        var errors = sink.Entries.Where(entry => entry.Severity == LogSeverity.Error).Select(entry => entry.Message).ToArray();
        Assert.Equal(8, errors.Length);
        for (int k = 0; k < 8; k++)
        {
            string prefix = "Patch 'K" + k.ToString() + "' failed and was skipped: ";
            Assert.Single(errors, message => message.StartsWith(prefix, StringComparison.Ordinal));
        }

        Assert.Equal(8, sink.Entries.Count(entry => entry.Severity == LogSeverity.Debug));
        Assert.Equal(8, trueCount);
    }

    // GW-U-127
    [Fact]
    public void ResetSessionLetsEveryKeyBeLoggedAgain()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        var reporter = new RateLimitedErrorReporter(sink);
        var error = new InvalidOperationException("boom");

        Assert.True(reporter.Report("X.Patch.Postfix", error));
        Assert.True(reporter.Report("X.Patch.Prefix", error));
        reporter.ResetSession();
        Assert.True(reporter.Report("X.Patch.Postfix", error));

        Assert.Equal(
            new[] { LogSeverity.Error, LogSeverity.Debug, LogSeverity.Error, LogSeverity.Debug, LogSeverity.Error, LogSeverity.Debug },
            sink.Entries.Select(entry => entry.Severity).ToArray());
        Assert.StartsWith(ErrorPrefixA, sink.Entries[4].Message, StringComparison.Ordinal);

        Assert.False(reporter.Report("X.Patch.Postfix", error));
        Assert.Equal(6, sink.Entries.Count);
        Assert.True(reporter.Report("X.Patch.Prefix", error));
        Assert.Equal(8, sink.Entries.Count);
        Assert.StartsWith(ErrorPrefixB, sink.Entries[6].Message, StringComparison.Ordinal);
    }

    // GW-U-127
    [Fact]
    public void ResetSessionOnAFreshReporterAndTwiceInARowDoesNotThrow()
    {
        var reporter = new RateLimitedErrorReporter(new RecordingLogSink());

        Assert.Null(Record.Exception(() => reporter.ResetSession()));
        Assert.Null(Record.Exception(() => reporter.ResetSession()));
    }

    // GW-U-128
    [Fact]
    public void ConstructorRejectsANullSink()
    {
        var error = Assert.Throws<ArgumentNullException>(() => new RateLimitedErrorReporter(null!));

        Assert.Equal("log", error.ParamName);
    }

    // GW-U-128
    [Fact]
    public void ThrowingSinkNeverEscapesReport()
    {
        var reporter = new RateLimitedErrorReporter(new ThrowingLogSink());
        var error = new InvalidOperationException("boom");

        Assert.Null(Record.Exception(() => reporter.Report(Key, error)));
        Assert.Null(Record.Exception(() => reporter.Report(Key, error)));
    }

    // GW-U-128
    [Fact]
    public void NullErrorReturnsFalseWritesNothingAndDoesNotMarkTheKey()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        var reporter = new RateLimitedErrorReporter(sink);

        Assert.False(reporter.Report(Key, null));
        Assert.Empty(sink.Entries);

        Assert.True(reporter.Report(Key, new InvalidOperationException("boom")));
        Assert.Equal(2, sink.Entries.Count);
    }

    // GW-U-128
    [Fact]
    public void ToStringThatThrowsStillGivesTheErrorLineAndAFallbackDebugLine()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        var reporter = new RateLimitedErrorReporter(sink);
        var odd = new ToStringThrowingException("odd");

        Assert.Null(Record.Exception(() => reporter.Report(Key, odd)));

        Assert.Equal(2, sink.Entries.Count);
        Assert.Equal(
            "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch.Postfix' failed and was skipped: BaseStorage.Core.Tests.Diagnostics.ToStringThrowingException: odd (repeats suppressed this session)",
            sink.Entries[0].Message);
        Assert.Equal(LogSeverity.Debug, sink.Entries[1].Severity);
        Assert.StartsWith(DebugPrefix, sink.Entries[1].Message, StringComparison.Ordinal);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Exception ThrowFromNamedFrame()
    {
        try
        {
            throw new InvalidOperationException("boom");
        }
        catch (InvalidOperationException ex)
        {
            return ex;
        }
    }

    private static Exception VaryingException(int i) => (i % 3) switch
    {
        0 => new ArgumentException("argument " + i.ToString()),
        1 => new FormatException(),
        _ => new InvalidOperationException("message " + i.ToString()),
    };
}
