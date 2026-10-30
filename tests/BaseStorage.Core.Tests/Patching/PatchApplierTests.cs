namespace BaseStorage.Core.Tests.Patching;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Patching;

/// <summary>
/// AC-3: PatchApplier.ApplyAll applies every patch, logs each failure by name and never throws because of a patch.
/// Fixtures E1-E13 of contracts/plugin-boot.md section 1.6 are copied verbatim (ADR-GWIN-013).
/// </summary>
public sealed class PatchApplierTests
{
    private const string HoverName = "BaseStorage.Plugin.Patches.ContainerHoverPatch";
    private const string StationName = "BaseStorage.Plugin.Patches.CraftingStationRangePatch";
    private const string BrokenName = "BaseStorage.Plugin.Patches.HaveRequirementsPatch";
    private const string BrokenEntry =
        "Patch 'BaseStorage.Plugin.Patches.HaveRequirementsPatch' failed to apply: System.InvalidOperationException: Could not find method Player.HaveRequirements(Piece+Requirement[], Boolean, Int32, Int32)";

    // GW-U-066
    [Fact]
    public void SuccessWithDebugOffAppliesEveryPatchAndLogsNothing()
    {
        var sink = new RecordingLogSink { DebugEnabled = false };
        var hover = new FakePatch(HoverName);
        var station = new FakePatch(StationName);

        PatchApplyReport report = ApplyNoThrow(new IPatchUnit[] { hover, station }, sink);

        Assert.Equal(2, report.AppliedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Equal(2, report.Results.Count);
        Assert.Equal(0, report.Results[0].Index);
        Assert.Equal(1, report.Results[1].Index);
        Assert.Equal(HoverName, report.Results[0].Name);
        Assert.True(report.Results[0].Succeeded);
        Assert.Null(report.Results[0].Error);
        Assert.True(report.Results[1].Succeeded);
        Assert.Null(report.Results[1].Error);
        Assert.Equal(1, hover.ApplyCalls);
        Assert.Equal(1, station.ApplyCalls);
        Assert.Empty(sink.Entries);
    }

    // GW-U-067
    [Fact]
    public void SuccessWithDebugOnLogsOneDebugLinePerPatch()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        var hover = new FakePatch(HoverName);
        var station = new FakePatch(StationName);

        PatchApplyReport report = ApplyNoThrow(new IPatchUnit[] { hover, station }, sink);

        Assert.Equal(2, report.AppliedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Equal(
            new (LogSeverity Severity, string Message)[]
            {
                (LogSeverity.Debug, "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch' applied"),
                (LogSeverity.Debug, "Patch 'BaseStorage.Plugin.Patches.CraftingStationRangePatch' applied"),
            },
            sink.Entries.ToArray());
    }

    // GW-U-068
    [Fact]
    public void FailureMidListIsLoggedByNameAndLaterPatchesStillApply()
    {
        var sink = new RecordingLogSink { DebugEnabled = false };
        var exception = new InvalidOperationException("Could not find method Player.HaveRequirements(Piece+Requirement[], Boolean, Int32, Int32)");
        var hover = new FakePatch(HoverName);
        var broken = new FakePatch(BrokenName, exception);
        var station = new FakePatch(StationName);

        PatchApplyReport report = ApplyNoThrow(new IPatchUnit[] { hover, broken, station }, sink);

        Assert.Equal(2, report.AppliedCount);
        Assert.Equal(1, report.FailedCount);
        Assert.False(report.Results[1].Succeeded);
        Assert.Equal(1, report.Results[1].Index);
        Assert.Same(exception, report.Results[1].Error);
        Assert.Equal(
            new (LogSeverity Severity, string Message)[] { (LogSeverity.Error, BrokenEntry) },
            sink.Entries.ToArray());
        Assert.Equal(1, hover.ApplyCalls);
        Assert.Equal(1, broken.ApplyCalls);
        Assert.Equal(1, station.ApplyCalls);
    }

    // GW-U-069
    [Fact]
    public void FailureAtIndexZeroStillAppliesTheNextPatch()
    {
        var sink = new RecordingLogSink();
        var broken = new FakePatch(BrokenName, new InvalidOperationException("Could not find method Player.HaveRequirements(Piece+Requirement[], Boolean, Int32, Int32)"));
        var hover = new FakePatch(HoverName);

        PatchApplyReport report = ApplyNoThrow(new IPatchUnit[] { broken, hover }, sink);

        Assert.Equal(1, report.FailedCount);
        Assert.True(report.Results[1].Succeeded);
        Assert.Equal(
            new (LogSeverity Severity, string Message)[] { (LogSeverity.Error, BrokenEntry) },
            sink.Entries.ToArray());
        Assert.Equal(1, hover.ApplyCalls);
    }

    // GW-U-069
    [Fact]
    public void AllFailingListReportsEveryFailureInInputOrder()
    {
        var sink = new RecordingLogSink();
        var first = new FakePatch("A.First", new InvalidOperationException("one"));
        var second = new FakePatch("A.Second", new InvalidOperationException("two"));
        var third = new FakePatch("A.Third", new InvalidOperationException("three"));

        PatchApplyReport report = ApplyNoThrow(new IPatchUnit[] { first, second, third }, sink);

        Assert.Equal(0, report.AppliedCount);
        Assert.Equal(3, report.FailedCount);
        (LogSeverity Severity, string Message)[] entries = sink.Entries.ToArray();
        Assert.Equal(3, entries.Length);
        Assert.All(entries, entry => Assert.Equal(LogSeverity.Error, entry.Severity));
        Assert.StartsWith("Patch 'A.First' failed to apply: ", entries[0].Message, StringComparison.Ordinal);
        Assert.StartsWith("Patch 'A.Second' failed to apply: ", entries[1].Message, StringComparison.Ordinal);
        Assert.StartsWith("Patch 'A.Third' failed to apply: ", entries[2].Message, StringComparison.Ordinal);
        Assert.Equal(1, first.ApplyCalls);
        Assert.Equal(1, second.ApplyCalls);
        Assert.Equal(1, third.ApplyCalls);
    }

    // GW-U-070
    [Fact]
    public void EmptyListReturnsAnEmptyReportAndLogsNothing()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };

        PatchApplyReport report = ApplyNoThrow(Array.Empty<IPatchUnit>(), sink);

        Assert.Empty(report.Results);
        Assert.Equal(0, report.AppliedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Empty(sink.Entries);
        Assert.Empty(sink.IsEnabledCalls);
    }

    // GW-U-071
    [Fact]
    public void ImmediateInnerExceptionIsIncludedInTheLogLine()
    {
        var sink = new RecordingLogSink();
        var inner = new MissingMethodException("Method not found: 'Void InventoryGui.UpdateContainer(Player)'.");
        var patch = new FakePatch(
            "BaseStorage.Plugin.Patches.InventoryGuiPatch",
            new Exception("Patching exception in method null", inner));

        PatchApplyReport report = ApplyNoThrow(new IPatchUnit[] { patch }, sink);

        Assert.Equal(1, report.FailedCount);
        Assert.Equal(
            new (LogSeverity Severity, string Message)[]
            {
                (LogSeverity.Error, "Patch 'BaseStorage.Plugin.Patches.InventoryGuiPatch' failed to apply: System.Exception: Patching exception in method null ---> System.MissingMethodException: Method not found: 'Void InventoryGui.UpdateContainer(Player)'."),
            },
            sink.Entries.ToArray());
    }

    // GW-U-071
    [Fact]
    public void OnlyTheImmediateInnerExceptionIsIncludedNotDeeperLevels()
    {
        var sink = new RecordingLogSink();
        var deepest = new FormatException("deep");
        var inner = new InvalidOperationException("inner", deepest);
        var patch = new FakePatch(HoverName, new Exception("outer", inner));

        PatchApplyReport report = ApplyNoThrow(new IPatchUnit[] { patch }, sink);

        Assert.Equal(1, report.FailedCount);
        (LogSeverity Severity, string Message) entry = Assert.Single(sink.Entries);
        Assert.Equal(
            "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch' failed to apply: System.Exception: outer ---> System.InvalidOperationException: inner",
            entry.Message);
        Assert.DoesNotContain("deep", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("FormatException", entry.Message, StringComparison.Ordinal);
    }

    // GW-U-072
    [Fact]
    public void MultiLineExceptionMessageIsLoggedOnOneLine()
    {
        var sink = new RecordingLogSink();
        var patch = new FakePatch(HoverName, new InvalidOperationException("line one\r\nline two\n"));

        PatchApplyReport report = ApplyNoThrow(new IPatchUnit[] { patch }, sink);

        Assert.Equal(1, report.FailedCount);
        Assert.Equal(
            new (LogSeverity Severity, string Message)[]
            {
                (LogSeverity.Error, "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch' failed to apply: System.InvalidOperationException: line one line two"),
            },
            sink.Entries.ToArray());
    }

    // GW-U-072
    [Fact]
    public void EmptyExceptionMessageDropsTheColonSuffix()
    {
        var sink = new RecordingLogSink();
        var patch = new FakePatch(HoverName, new InvalidOperationException(string.Empty));

        ApplyNoThrow(new IPatchUnit[] { patch }, sink);

        Assert.Equal(
            new (LogSeverity Severity, string Message)[]
            {
                (LogSeverity.Error, "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch' failed to apply: System.InvalidOperationException"),
            },
            sink.Entries.ToArray());
    }

    // GW-U-072
    [Fact]
    public void WhitespaceOnlyExceptionMessageCountsAsEmpty()
    {
        var sink = new RecordingLogSink();
        var patch = new FakePatch(HoverName, new InvalidOperationException("\r\n "));

        ApplyNoThrow(new IPatchUnit[] { patch }, sink);

        Assert.Equal(
            new (LogSeverity Severity, string Message)[]
            {
                (LogSeverity.Error, "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch' failed to apply: System.InvalidOperationException"),
            },
            sink.Entries.ToArray());
    }

    // GW-U-072
    [Theory]
    [InlineData("a\rb", "a b")]
    [InlineData("a\nb", "a b")]
    [InlineData("a\r\nb", "a b")]
    [InlineData("a\u0085b", "a b")]
    public void EveryLineBreakBecomesOneSpace(string message, string expected)
    {
        var sink = new RecordingLogSink();
        var patch = new FakePatch(HoverName, new InvalidOperationException(message));

        ApplyNoThrow(new IPatchUnit[] { patch }, sink);

        (LogSeverity Severity, string Message) entry = Assert.Single(sink.Entries);
        Assert.Equal(
            "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch' failed to apply: System.InvalidOperationException: " + expected,
            entry.Message);
        Assert.DoesNotContain('\r', entry.Message);
        Assert.DoesNotContain('\n', entry.Message);
        Assert.DoesNotContain('\u0085', entry.Message);
    }

    // GW-U-072 (U+2028 and U+2029 are written as char casts so that they are never raw line breaks in source)
    [Theory]
    [InlineData(0x2028)]
    [InlineData(0x2029)]
    public void UnicodeLineAndParagraphSeparatorsBecomeOneSpace(int codePoint)
    {
        char separator = (char)codePoint;
        var sink = new RecordingLogSink();
        var patch = new FakePatch(HoverName, new InvalidOperationException("a" + separator + "b"));

        ApplyNoThrow(new IPatchUnit[] { patch }, sink);

        (LogSeverity Severity, string Message) entry = Assert.Single(sink.Entries);
        Assert.Equal(
            "Patch 'BaseStorage.Plugin.Patches.ContainerHoverPatch' failed to apply: System.InvalidOperationException: a b",
            entry.Message);
        Assert.DoesNotContain(separator, entry.Message);
    }

    // GW-U-073
    [Fact]
    public void NullElementIsReportedAsUnnamedAndTheLaterPatchStillApplies()
    {
        var sink = new RecordingLogSink();
        var hover = new FakePatch(HoverName);
        var station = new FakePatch(StationName);

        PatchApplyReport report = ApplyNoThrow(new IPatchUnit[] { hover, null!, station }, sink);

        Assert.Equal(1, report.FailedCount);
        Assert.Equal("<unnamed patch 1>", report.Results[1].Name);
        ArgumentNullException error = Assert.IsType<ArgumentNullException>(report.Results[1].Error);
        Assert.Equal("patches", error.ParamName);
        (LogSeverity Severity, string Message) entry = Assert.Single(sink.Entries);
        Assert.Equal(LogSeverity.Error, entry.Severity);
        Assert.Equal(
            "Patch '<unnamed patch 1>' failed to apply: System.ArgumentNullException: " + SingleLine(error.Message),
            entry.Message);
        Assert.Equal(1, hover.ApplyCalls);
        Assert.Equal(1, station.ApplyCalls);
    }

    // GW-U-073
    [Fact]
    public void WhitespaceNameIsUnnamedAndLogsNothingWithDebugOff()
    {
        var sink = new RecordingLogSink { DebugEnabled = false };
        var patch = new FakePatch("   ");

        PatchApplyReport report = ApplyNoThrow(new IPatchUnit[] { patch }, sink);

        Assert.Equal(1, report.AppliedCount);
        Assert.Equal("<unnamed patch 0>", report.Results[0].Name);
        Assert.Empty(sink.Entries);
    }

    // GW-U-073
    [Fact]
    public void WhitespaceNameIsUnnamedInTheDebugLineWithDebugOn()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        var patch = new FakePatch("   ");

        ApplyNoThrow(new IPatchUnit[] { patch }, sink);

        Assert.Equal(
            new (LogSeverity Severity, string Message)[] { (LogSeverity.Debug, "Patch '<unnamed patch 0>' applied") },
            sink.Entries.ToArray());
    }

    // GW-U-073
    [Fact]
    public void ThrowingNameGetterIsAFailureWithoutApplyBeingCalled()
    {
        var sink = new RecordingLogSink();
        var patch = new ThrowingNamePatch(new NotSupportedException("no name"));

        PatchApplyReport report = ApplyNoThrow(new IPatchUnit[] { patch }, sink);

        Assert.Equal(1, report.FailedCount);
        Assert.Equal("<unnamed patch 0>", report.Results[0].Name);
        Assert.Equal(0, patch.ApplyCalls);
        Assert.Equal(
            new (LogSeverity Severity, string Message)[]
            {
                (LogSeverity.Error, "Patch '<unnamed patch 0>' failed to apply: System.NotSupportedException: no name"),
            },
            sink.Entries.ToArray());
    }

    // GW-U-073
    [Fact]
    public void NullNameIsTreatedAsUnnamed()
    {
        var sink = new RecordingLogSink();
        var patch = new CountingNamePatch(null);

        PatchApplyReport report = ApplyNoThrow(new IPatchUnit[] { patch }, sink);

        Assert.Equal(1, report.AppliedCount);
        Assert.Equal("<unnamed patch 0>", report.Results[0].Name);
    }

    // GW-U-073
    [Fact]
    public void MultiLineNameIsShownOnOneLine()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        var patch = new FakePatch("A.B\r\nC");

        PatchApplyReport report = ApplyNoThrow(new IPatchUnit[] { patch }, sink);

        Assert.Equal("A.B C", report.Results[0].Name);
        Assert.Equal(
            new (LogSeverity Severity, string Message)[] { (LogSeverity.Debug, "Patch 'A.B C' applied") },
            sink.Entries.ToArray());
    }

    // GW-U-074
    [Theory]
    [InlineData("NullReferenceException")]
    [InlineData("TypeLoadException")]
    [InlineData("MissingMethodException")]
    [InlineData("ReflectionTypeLoadException")]
    [InlineData("CustomPatchException")]
    [InlineData("MessageThrowingException")]
    public void EveryExceptionTypeIsCaughtInTheMiddleOfAThreeElementList(string kind)
    {
        Exception thrown = CreateException(kind);
        var sink = new RecordingLogSink();
        var first = new FakePatch(HoverName);
        var broken = new FakePatch(BrokenName, thrown);
        var last = new FakePatch(StationName);

        PatchApplyReport report = ApplyNoThrow(new IPatchUnit[] { first, broken, last }, sink);

        Assert.Equal(1, report.FailedCount);
        Assert.Same(thrown, report.Results[1].Error);
        (LogSeverity Severity, string Message) entry = Assert.Single(sink.Entries);
        Assert.Equal(LogSeverity.Error, entry.Severity);
        Assert.Contains(BrokenName, entry.Message, StringComparison.Ordinal);
        Assert.Contains(thrown.GetType().FullName!, entry.Message, StringComparison.Ordinal);
        Assert.Equal(1, first.ApplyCalls);
        Assert.Equal(1, last.ApplyCalls);
    }

    // GW-U-075
    [Fact]
    public void NullPatchesListThrowsForPatchesBeforeAnythingIsLogged()
    {
        var sink = new RecordingLogSink();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => PatchApplier.ApplyAll(null!, sink));

        Assert.Equal("patches", ex.ParamName);
        Assert.Empty(sink.Entries);
    }

    // GW-U-075
    [Fact]
    public void NullLogSinkThrowsForLogAndNothingIsApplied()
    {
        var hover = new FakePatch(HoverName);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => PatchApplier.ApplyAll(new IPatchUnit[] { hover }, null!));

        Assert.Equal("log", ex.ParamName);
        Assert.Equal(0, hover.ApplyCalls);
    }

    // GW-U-075
    [Fact]
    public void WhenBothArgumentsAreNullPatchesIsReportedFirst()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => PatchApplier.ApplyAll(null!, null!));

        Assert.Equal("patches", ex.ParamName);
    }

    // GW-U-076 (a)
    [Fact]
    public void ApplyCallsHappenInIndexOrder()
    {
        var sink = new RecordingLogSink();
        var order = new List<string>();
        var patches = new IPatchUnit[]
        {
            new OrderedPatch("A", order),
            new OrderedPatch("B", order),
            new OrderedPatch("C", order),
        };

        ApplyNoThrow(patches, sink);

        Assert.Equal(new[] { "A", "B", "C" }, order);
    }

    // GW-U-076 (b)
    [Fact]
    public void NameIsReadExactlyOncePerElement()
    {
        var sink = new RecordingLogSink { DebugEnabled = true };
        var patch = new CountingNamePatch(HoverName);

        ApplyNoThrow(new IPatchUnit[] { patch }, sink);

        Assert.Equal(1, patch.NameReads);
        Assert.Equal(1, patch.ApplyCalls);
    }

    // GW-U-076 (c)
    [Fact]
    public void ErrorIsLoggedEvenWhenEverySeverityIsDisabled()
    {
        var sink = new RecordingLogSink { DisableAll = true };
        var broken = new FakePatch(BrokenName, new InvalidOperationException("boom"));

        ApplyNoThrow(new IPatchUnit[] { broken }, sink);

        (LogSeverity Severity, string Message) entry = Assert.Single(sink.Entries);
        Assert.Equal(LogSeverity.Error, entry.Severity);
        Assert.Contains(BrokenName, entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(LogSeverity.Error, sink.IsEnabledCalls);
    }

    // GW-U-076 (d)
    [Fact]
    public void DebugSeverityIsConsultedAndNoDebugLineIsWrittenWhenOff()
    {
        var sink = new RecordingLogSink { DebugEnabled = false };
        var hover = new FakePatch(HoverName);

        ApplyNoThrow(new IPatchUnit[] { hover }, sink);

        Assert.Contains(LogSeverity.Debug, sink.IsEnabledCalls);
        Assert.DoesNotContain(sink.Entries, entry => entry.Severity == LogSeverity.Debug);
    }

    // GW-U-076 (e)
    [Fact]
    public void CallingApplyAllTwiceAppliesEachPatchTwiceWithIndependentReports()
    {
        var sink = new RecordingLogSink();
        var hover = new FakePatch(HoverName);
        var station = new FakePatch(StationName);
        IPatchUnit[] patches = { hover, station };

        PatchApplyReport first = ApplyNoThrow(patches, sink);
        PatchApplyReport second = ApplyNoThrow(patches, sink);

        Assert.Equal(2, hover.ApplyCalls);
        Assert.Equal(2, station.ApplyCalls);
        Assert.NotSame(first, second);
        Assert.Equal(2, first.Results.Count);
        Assert.Equal(2, second.Results.Count);
        Assert.NotSame(first.Results[0], second.Results[0]);
    }

    // GW-U-076 (f)
    [Fact]
    public void MixedListKeepsCountsAndResultsConsistent()
    {
        var sink = new RecordingLogSink();
        IPatchUnit[] patches =
        {
            new FakePatch("A"),
            new FakePatch("B", new InvalidOperationException("one")),
            new FakePatch("C"),
            new FakePatch("D", new InvalidOperationException("two")),
            new FakePatch("E"),
        };

        PatchApplyReport report = ApplyNoThrow(patches, sink);

        Assert.Equal(patches.Length, report.Results.Count);
        Assert.Equal(3, report.AppliedCount);
        Assert.Equal(2, report.FailedCount);
        Assert.Equal(patches.Length, report.AppliedCount + report.FailedCount);
        Assert.Equal(Enumerable.Range(0, patches.Length), report.Results.Select(r => r.Index));
    }

    private static Exception CreateException(string kind) => kind switch
    {
        "NullReferenceException" => new NullReferenceException("nre"),
        "TypeLoadException" => new TypeLoadException("tle"),
        "MissingMethodException" => new MissingMethodException("mme"),
        "ReflectionTypeLoadException" => new ReflectionTypeLoadException(Array.Empty<Type?>(), Array.Empty<Exception?>()),
        "CustomPatchException" => new CustomPatchException("custom"),
        "MessageThrowingException" => new MessageThrowingException(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown exception kind."),
    };

    private static string SingleLine(string text) =>
        text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Replace('\u0085', ' ').Replace((char)0x2028, ' ').Replace((char)0x2029, ' ').Trim();

    private static PatchApplyReport ApplyNoThrow(IReadOnlyList<IPatchUnit> patches, ILogSink sink)
    {
        PatchApplyReport? report = null;
        Exception? thrown = Record.Exception(() => report = PatchApplier.ApplyAll(patches, sink));
        Assert.Null(thrown);
        Assert.NotNull(report);
        return report;
    }
}
