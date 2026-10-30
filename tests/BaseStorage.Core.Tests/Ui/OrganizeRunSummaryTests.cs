namespace BaseStorage.Core.Tests.Ui;

using System;
using System.Collections.Generic;
using System.Globalization;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Ui;

/// <summary>S-E004-009 AC-2: the one end-of-run MessageHud line (spec AC-4.8, AC-8.1; ui-strings contract 10.2 and 10.3).</summary>
public sealed class OrganizeRunSummaryTests
{
    // U+0085, U+2028 and U+2029 are written as char casts: C# treats them as newlines inside source text.
    private static readonly char[] BreakCharacters = ['\r', '\n', (char)0x85, (char)0x2028, (char)0x2029];

    private static OrganizeSkipEntry InUse(string? name) => new(SkipReason.InUse, name);

    private static OrganizeRunReport Report(int moved, int noRoom, bool stopped = false, string? reason = null, params OrganizeSkipEntry[] skipped) =>
        new(moved, skipped, noRoom, stopped, reason);

    // GW-U-987 (AC-2): the golden plan after a complete run gives the contract 10.3 row G line.
    [Fact]
    public void GoldenRunRendersOneLine()
    {
        OrganizePlan plan = OrganizeSummaryFixtures.Plan(OrganizeSummaryFixtures.Golden());
        OrganizeRunReport report = OrganizeRunReport.FromPlan(plan, plan.Moves.Count, stoppedEarly: false, stopReason: null);

        Assert.Equal(214, report.MovedStacks);
        Assert.Equal(4, report.NoRoomStacks);
        Assert.Single(report.Skipped);
        Assert.Equal("Erik", report.Skipped[0].PlayerName);
        Assert.False(report.StoppedEarly);
        Assert.Null(report.StopReason);

        Assert.Equal(OrganizeSummaryFixtures.GoldenSummary, OrganizeSummaryFormatter.Summary(report));
    }

    // GW-U-988 (AC-2): singular, plural and zero forms of every fragment (contract 10.3 rows M0 to M2).
    [Theory]
    [InlineData(0, 0, "Nothing moved")]
    [InlineData(1, 0, "Organized 1 stack")]
    [InlineData(2, 0, "Organized 2 stacks")]
    [InlineData(0, 1, "Nothing moved; 1 stack had no room")]
    [InlineData(1, 1, "Organized 1 stack; 1 stack had no room")]
    [InlineData(37, 4, "Organized 37 stacks; 4 stacks had no room")]
    public void MovedAndNoRoomUseTheirPluralForms(int moved, int noRoom, string expected)
    {
        Assert.Equal(expected, OrganizeSummaryFormatter.Summary(Report(moved, noRoom)));
    }

    // GW-U-988 (AC-2): skipped fragments per reason group, in use first, names distinct and nameless in-use chests say "in use".
    [Fact]
    public void SkippedFragmentsAreGroupedByReason()
    {
        Assert.Equal(
            "Organized 12 stacks; 1 chest skipped (in use by Erik)",
            OrganizeSummaryFormatter.Summary(Report(12, 0, skipped: InUse("Erik"))));
        Assert.Equal(
            "Organized 12 stacks; 1 chest skipped (in use)",
            OrganizeSummaryFormatter.Summary(Report(12, 0, skipped: InUse(null))));
        Assert.Equal(
            "Organized 12 stacks; 3 chests skipped (in use by Erik, Freya)",
            OrganizeSummaryFormatter.Summary(Report(12, 0, skipped: [InUse("Erik"), InUse("Freya"), InUse("Erik")])));
        Assert.Equal(
            "Nothing moved; 2 chests skipped (in use by Freya); 1 chest skipped (Keep); 2 chests skipped (not eligible); 1 chest skipped (unclear contents); 3 stacks had no room",
            OrganizeSummaryFormatter.Summary(Report(
                0,
                3,
                skipped:
                [
                    new OrganizeSkipEntry(SkipReason.InvalidContents, null),
                    new OrganizeSkipEntry(SkipReason.Ineligible, null),
                    InUse(" "),
                    new OrganizeSkipEntry(SkipReason.Keep, null),
                    InUse("Freya"),
                    new OrganizeSkipEntry(SkipReason.Ineligible, null),
                ])));
    }

    // GW-U-989 (AC-2): a run that stopped early names the reason after the moved fragment, or says so without one.
    [Fact]
    public void StoppedRunsReportTheStop()
    {
        Assert.Equal(
            "Organized 12 stacks; stopped early: chest changed; 1 chest skipped (in use by Erik); 4 stacks had no room",
            OrganizeSummaryFormatter.Summary(Report(12, 4, stopped: true, reason: "chest changed", InUse("Erik"))));
        Assert.Equal("Organized 12 stacks; stopped early", OrganizeSummaryFormatter.Summary(Report(12, 0, stopped: true)));
        Assert.Equal("Nothing moved; stopped early", OrganizeSummaryFormatter.Summary(Report(0, 0, stopped: true, reason: "  ")));
        Assert.Equal("Organized 12 stacks", OrganizeSummaryFormatter.Summary(Report(12, 0, stopped: false, reason: "ignored")));
    }

    // GW-U-990 (AC-2, AC-3): one line without markup, single-lined names and reasons, no outer whitespace (contract S10, N3).
    [Fact]
    public void SummaryIsOneLineWithoutMarkup()
    {
        OrganizeRunReport[] reports =
        [
            Report(12, 4, stopped: true, reason: "chest\nchanged", InUse("Erik\r\nthe Red")),
            Report(0, 0),
            OrganizeRunReport.FromPlan(OrganizeSummaryFixtures.Plan(OrganizeSummaryFixtures.Groups()), 0, false, null),
        ];

        Assert.Equal(
            "Organized 12 stacks; stopped early: chest changed; 1 chest skipped (in use by Erik the Red); 4 stacks had no room",
            OrganizeSummaryFormatter.Summary(reports[0]));

        foreach (OrganizeRunReport report in reports)
        {
            string text = OrganizeSummaryFormatter.Summary(report);
            Assert.Equal(-1, text.IndexOfAny(BreakCharacters));
            Assert.DoesNotContain("<", text, StringComparison.Ordinal);
            Assert.Equal(text.Trim(), text);
            Assert.NotEqual(string.Empty, text);
        }
    }

    // GW-U-991 (AC-3): the same text under de-DE, tr-TR and en-US (contract U2).
    [Theory]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    [InlineData("en-US")]
    public void SummaryIsInvariantAcrossCultures(string cultureName)
    {
        CultureInfo savedCurrent = CultureInfo.CurrentCulture;
        CultureInfo savedUi = CultureInfo.CurrentUICulture;
        try
        {
            var culture = new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            Assert.Equal(
                "Organized 1234 stacks; 1 chest skipped (in use by Erik); 5678 stacks had no room",
                OrganizeSummaryFormatter.Summary(Report(1234, 5678, skipped: InUse("Erik"))));
        }
        finally
        {
            CultureInfo.CurrentCulture = savedCurrent;
            CultureInfo.CurrentUICulture = savedUi;
        }
    }

    // GW-U-992 (AC-2): the report copies its inputs, normalises blank names and reasons, and validates its arguments (R11).
    [Fact]
    public void ReportCopiesAndValidatesItsInputs()
    {
        var entries = new List<OrganizeSkipEntry> { InUse("Erik") };
        var report = new OrganizeRunReport(3, entries, 2, true, "halted");
        entries.Add(InUse("Freya"));

        Assert.Single(report.Skipped);
        Assert.Equal(3, report.MovedStacks);
        Assert.Equal(2, report.NoRoomStacks);
        Assert.True(report.StoppedEarly);
        Assert.Equal("halted", report.StopReason);
        Assert.Throws<NotSupportedException>(() => ((IList<OrganizeSkipEntry>)report.Skipped).Add(InUse("Odin")));

        Assert.Null(new OrganizeRunReport(0, [], 0, false, "ignored").StopReason);
        Assert.Null(new OrganizeRunReport(0, [], 0, true, " ").StopReason);
        Assert.Null(InUse("").PlayerName);
        Assert.Null(InUse(" \t").PlayerName);
        Assert.Equal("Erik", InUse("Erik").PlayerName);

        Assert.Equal("movedStacks", Assert.Throws<ArgumentOutOfRangeException>(() => new OrganizeRunReport(-1, [], 0, false, null)).ParamName);
        Assert.Equal("noRoomStacks", Assert.Throws<ArgumentOutOfRangeException>(() => new OrganizeRunReport(0, [], -1, false, null)).ParamName);
        Assert.Equal("skipped", Assert.Throws<ArgumentNullException>(() => new OrganizeRunReport(0, null!, 0, false, null)).ParamName);
        Assert.Equal("kind", Assert.Throws<ArgumentOutOfRangeException>(() => new OrganizeSkipEntry((SkipReason)99, null)).ParamName);
        Assert.Equal("skipped", Assert.Throws<ArgumentNullException>(() => OrganizeSkipEntry.From(null!)).ParamName);
        Assert.Equal("plan", Assert.Throws<ArgumentNullException>(() => OrganizeRunReport.FromPlan(null!, 0, false, null)).ParamName);
        Assert.Equal("report", Assert.Throws<ArgumentNullException>(() => OrganizeSummaryFormatter.Summary(null!)).ParamName);
    }

    // GW-U-992 (AC-2): FromPlan mirrors the plan's skipped chests, in order, and its no-room count.
    [Fact]
    public void FromPlanMirrorsTheSkippedChestsAndNoRoomCount()
    {
        OrganizePlan plan = OrganizeSummaryFixtures.Plan(OrganizeSummaryFixtures.Groups());
        OrganizeRunReport report = OrganizeRunReport.FromPlan(plan, 0, false, null);

        Assert.Equal(plan.Skipped.Count, report.Skipped.Count);
        for (int i = 0; i < plan.Skipped.Count; i++)
        {
            Assert.Equal(plan.Skipped[i].Kind, report.Skipped[i].Kind);
            Assert.Equal(plan.Skipped[i].PlayerName, report.Skipped[i].PlayerName);
        }

        Assert.Equal(plan.NoRoom.Count, report.NoRoomStacks);
        Assert.Equal(
            "Nothing moved; 3 chests skipped (in use by Erik, Freya); 2 chests skipped (not eligible); 2 chests skipped (unclear contents)",
            OrganizeSummaryFormatter.Summary(report));
    }
}
