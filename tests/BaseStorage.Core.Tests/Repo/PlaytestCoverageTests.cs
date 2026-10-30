namespace BaseStorage.Core.Tests.Repo;

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// S-E007-003 (contracts/playtest-coverage-contracts.md v1, PC1 to PC6; ADR-GWIN-144, ADR-GWIN-145): docs/PLAYTEST.md has one
/// "Acceptance script" level-2 section, placed between Favourites and Base membership, with one numbered level-3 section per
/// acceptance criterion from AC-2.1 to AC-6.3. Each section has the right form and no empty subsection, and every delegated
/// heading exists. The file is parsed as text with CRLF normalised.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestCoverageTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string BeforeHeading = "## Favourites";
    private const string AfterHeading = "## Base membership (AC-2.3, AC-2.5, AC-5.3)";
    private const string SoloSubsection = "Solo";
    private const string HostSubsection = "Host plus one friend";
    private const string DeskSubsection = "Solo and host plus one friend";
    private const string AdrPath = "docs/adr/0002-container-ownership-and-access-model.md";
    private const string SimulationCommand =
        "dotnet test tests/BaseStorage.Core.Tests -c Release --filter FullyQualifiedName~BaseStorage.Core.Tests.Simulation";
    private const string SimulationFile = "tests/BaseStorage.Core.Tests/Simulation/SimulationTests.cs";

    // Contract PC2 (table 2): section number N is the 1-based index.
    private static readonly (string Id, string Title)[] Required =
    [
        ("AC-2.1", "Station graph"),
        ("AC-2.2", "Base membership"),
        ("AC-2.3", "Eligible containers"),
        ("AC-2.4", "Starting point"),
        ("AC-2.5", "Range cap and cache"),
        ("AC-2.6", "Inspect"),
        ("AC-3.1", "Availability"),
        ("AC-3.2", "Display"),
        ("AC-3.3", "Consumption order"),
        ("AC-3.4", "Atomic consumption"),
        ("AC-3.5", "Upgrades and multi-craft"),
        ("AC-3.6", "Toggle"),
        ("AC-4.1", "Category catalog"),
        ("AC-4.2", "Category on the chest"),
        ("AC-4.3", "Manual assignment"),
        ("AC-4.4", "Auto-assignment"),
        ("AC-4.5", "Organize plan"),
        ("AC-4.6", "Keep chests"),
        ("AC-4.7", "Preview and confirm"),
        ("AC-4.8", "Execution"),
        ("AC-4.9", "Deposit (put away inventory)"),
        ("AC-4.10", "Station is the entry point"),
        ("AC-5.1", "Ownership model, documented"),
        ("AC-5.2", "Reservation protocol"),
        ("AC-5.3", "In-use chests"),
        ("AC-5.4", "One Organize per base"),
        ("AC-5.5", "Conservation under concurrency"),
        ("AC-6.1", "Version check"),
        ("AC-6.2", "Host-synced config"),
        ("AC-6.3", "Single-player parity"),
    ];

    // Contract PC3.3: the desk-check form is only for these two ids.
    private static readonly string[] DeskCheckIds = ["AC-5.1", "AC-5.5"];

    private static readonly string[] AdrLiterals = [AdrPath];
    private static readonly string[] SimulationLiterals = [SimulationCommand, SimulationFile];

    private static readonly Regex ScriptHeadingLine = new(@"^## Acceptance script \(AC-2\.1 to AC-6\.3\)[ \t]*$");
    private static readonly Regex SectionHeadingLine = new(@"^### (\d+)\. (AC-\d+\.\d+) - (.+?)[ \t]*$");
    private static readonly Regex SubsectionLine = new(@"^#### (Solo|Host plus one friend|Solo and host plus one friend)[ \t]*$");
    private static readonly Regex StepLine = new(@"^(\d+)\. ");
    private static readonly Regex BacktickedHeading = new(@"`(## [^`\n]+)`");

    // GW-U-2050
    [Fact]
    public void TheAcceptanceScriptSitsBetweenFavouritesAndBaseMembership()
    {
        ScriptReport report = ParseFile();
        AssertScriptFound(report);

        Assert.True(
            report.PreviousHeading == BeforeHeading && report.NextHeading == AfterHeading,
            "The Acceptance script section must sit directly between '## Favourites' and '## Base membership'.");

        int nonBlank = report.Preamble.Split('\n').Count(line => line.Trim().Length > 0);
        Assert.True(
            nonBlank is >= 1 and <= 6 && report.Preamble.Contains("PlaytestCoverageTests", System.StringComparison.Ordinal),
            "The Acceptance script preamble must be 1 to 6 non-blank lines and name PlaytestCoverageTests.");
    }

    // GW-U-2051
    [Fact]
    public void EveryE2ToE6AcIdHasASection()
    {
        ScriptReport report = ParseFile();

        Assert.True(
            report.Missing.Count == 0,
            "docs/PLAYTEST.md has no playtest section for: " + string.Join(", ", report.Missing));
    }

    // GW-U-2052
    [Fact]
    public void NoSectionIsEmpty()
    {
        ScriptReport report = ParseFile();
        AssertScriptFound(report);

        Assert.True(
            report.Empty.Count == 0,
            "Empty playtest sections: " + string.Join(", ", report.Empty));
    }

    // GW-U-2053
    [Fact]
    public void SectionsAreNumberedInSpecOrderWithTheirTitles()
    {
        ScriptReport report = ParseFile();
        AssertScriptFound(report);

        Assert.True(
            report.Order.Count == 0,
            "Playtest sections out of order or misnamed: " + string.Join("; ", report.Order));
    }

    // GW-U-2054
    [Fact]
    public void ReferencedHeadingsExist()
    {
        ScriptReport report = ParseFile();
        AssertScriptFound(report);

        Assert.True(
            report.BadReferences.Count == 0,
            "Acceptance script references missing headings: " + string.Join(", ", report.BadReferences));
    }

    // GW-U-2055
    [Fact]
    public void TheParserGivesTheSameResultForLfAndCrlf()
    {
        ScriptReport lf = Parse(TwoSectionSample);
        ScriptReport crlf = Parse(TwoSectionSample.Replace("\n", "\r\n"));

        Assert.Equal(lf.Missing, crlf.Missing);
        Assert.Equal(lf.Empty, crlf.Empty);
        Assert.Equal(lf.Order, crlf.Order);
        Assert.Equal(lf.BadReferences, crlf.BadReferences);

        Assert.Equal(28, lf.Missing.Count);
        Assert.Equal(new[] { "AC-2.2 (no Host plus one friend subsection)" }, lf.Empty);
    }

    // GW-U-2056
    [Fact]
    public void TheParserNamesEveryMissingIdOfAnEmptyScript()
    {
        ScriptReport report = Parse("## Acceptance script (AC-2.1 to AC-6.3)\n");

        Assert.Equal(Required.Select(required => required.Id), report.Missing);
    }

    private const string TwoSectionSample =
        "## Acceptance script (AC-2.1 to AC-6.3)\n\n" +
        "### 1. AC-2.1 - Station graph\n\n#### Solo\n\n1. Build a workbench.\nExpected: hovering it shows `[In base: 1 stations]`.\n\n" +
        "#### Host plus one friend\n\n1. The friend hovers the host's workbench.\nExpected: the same text.\n\n" +
        "### 2. AC-2.2 - Base membership\n\n#### Solo\n\n1. Place a chest 5 m from the workbench.\nExpected: hovering it shows `[In base: 1 stations]`.\n";

    private static void AssertScriptFound(ScriptReport report)
    {
        Assert.True(report.HeadingProblem is null, report.HeadingProblem);
    }

    private static ScriptReport ParseFile() => Parse(RepoRoot.ReadText(PlaytestFile));

    /// <summary>
    /// Parses the Acceptance script section. Applies CRLF to LF first, so a CRLF string gives the same report.
    /// Missing and Empty follow table 2 order; Order and BadReferences follow document order.
    /// </summary>
    private static ScriptReport Parse(string raw)
    {
        string[] lines = raw.Replace("\r\n", "\n").Split('\n');

        List<int> starts = [];
        for (int i = 0; i < lines.Length; i++)
        {
            if (ScriptHeadingLine.IsMatch(lines[i]))
            {
                starts.Add(i);
            }
        }

        if (starts.Count != 1)
        {
            return new ScriptReport(
                "docs/PLAYTEST.md has no single '## Acceptance script (AC-2.1 to AC-6.3)' heading.",
                null,
                null,
                string.Empty,
                Required.Select(required => required.Id).ToList(),
                [],
                [],
                []);
        }

        int start = starts[0];
        int end = start + 1;
        while (end < lines.Length && !lines[end].StartsWith("## ", System.StringComparison.Ordinal))
        {
            end++;
        }

        string? previous = null;
        for (int i = start - 1; i >= 0; i--)
        {
            if (lines[i].StartsWith("## ", System.StringComparison.Ordinal))
            {
                previous = TrimTrailingBlanks(lines[i]);
                break;
            }
        }

        string? next = end < lines.Length ? TrimTrailingBlanks(lines[end]) : null;
        string[] section = lines[(start + 1)..end];

        List<string> order = [];
        List<Heading> headings = [];
        for (int i = 0; i < section.Length; i++)
        {
            if (!section[i].StartsWith("### ", System.StringComparison.Ordinal))
            {
                continue;
            }

            string line = TrimTrailingBlanks(section[i]);
            Match match = SectionHeadingLine.Match(line);
            if (match.Success)
            {
                headings.Add(new Heading(match.Groups[2].Value, line, i));
            }
            else
            {
                order.Add($"unrecognised heading '{line}'");
            }
        }

        int slots = System.Math.Max(headings.Count, Required.Length);
        for (int i = 0; i < slots; i++)
        {
            string? expected = i < Required.Length ? $"### {i + 1}. {Required[i].Id} - {Required[i].Title}" : null;
            string? found = i < headings.Count ? headings[i].Line : null;
            if (expected == found)
            {
                continue;
            }

            if (expected is null)
            {
                order.Add($"expected nothing, found '{found}'");
            }
            else if (found is null)
            {
                order.Add($"expected '{expected}', found nothing");
            }
            else
            {
                order.Add($"expected '{expected}', found '{found}'");
            }
        }

        List<string> missing = [];
        List<string> empty = [];
        foreach (var (id, _) in Required)
        {
            int index = headings.FindIndex(heading => heading.Id == id);
            if (index < 0)
            {
                missing.Add(id);
                continue;
            }

            string? reason = EmptyReason(id, BodyOf(section, headings[index].Index));
            if (reason is not null)
            {
                empty.Add($"{id} ({reason})");
            }
        }

        HashSet<string> levelTwo = lines
            .Where(line => line.StartsWith("## ", System.StringComparison.Ordinal))
            .Select(TrimTrailingBlanks)
            .ToHashSet(System.StringComparer.Ordinal);
        List<string> badReferences = [];
        foreach (Match match in BacktickedHeading.Matches(string.Join("\n", section)))
        {
            string target = TrimTrailingBlanks(match.Groups[1].Value);
            if (!levelTwo.Contains(target) && !badReferences.Contains(target))
            {
                badReferences.Add(target);
            }
        }

        string preamble = string.Join("\n", section.TakeWhile(line => !line.StartsWith("### ", System.StringComparison.Ordinal)));

        return new ScriptReport(null, previous, next, preamble, missing, empty, order, badReferences);
    }

    /// <summary>
    /// The first broken 3.2 or 3.3 rule of one section body, or null when the body is well formed.
    /// </summary>
    private static string? EmptyReason(string id, string[] body)
    {
        if (!body.Any(line => StepLine.IsMatch(line)))
        {
            return "no steps";
        }

        List<(string Name, int Index)> subsections = [];
        for (int i = 0; i < body.Length; i++)
        {
            Match match = SubsectionLine.Match(body[i]);
            if (match.Success)
            {
                subsections.Add((match.Groups[1].Value, i));
            }
        }

        bool desk = DeskCheckIds.Contains(id);
        bool hasDeskSubsection = subsections.Any(sub => sub.Name == DeskSubsection);
        bool hasGameSubsection = subsections.Any(sub => sub.Name != DeskSubsection);

        if (!desk && hasDeskSubsection)
        {
            return "desk-check form is only for AC-5.1 and AC-5.5";
        }

        if (desk && hasGameSubsection)
        {
            return "needs the desk-check form";
        }

        if (desk && !body.Any(line => line.StartsWith("Not observable in game: ", System.StringComparison.Ordinal)))
        {
            return "no 'Not observable in game:' line";
        }

        string[] needed = desk ? [DeskSubsection] : [SoloSubsection, HostSubsection];
        foreach (string name in needed)
        {
            int count = subsections.Count(sub => sub.Name == name);
            if (count == 0)
            {
                return $"no {name} subsection";
            }

            if (count > 1)
            {
                return $"{name} subsection twice";
            }
        }

        if (!desk && subsections.Single(sub => sub.Name == SoloSubsection).Index
            > subsections.Single(sub => sub.Name == HostSubsection).Index)
        {
            return "Solo must come before Host plus one friend";
        }

        int firstSubsection = subsections.Count == 0 ? body.Length : subsections[0].Index;
        for (int i = 0; i < firstSubsection; i++)
        {
            if (StepLine.IsMatch(body[i]))
            {
                return "step outside a subsection";
            }
        }

        for (int s = 0; s < subsections.Count; s++)
        {
            int from = subsections[s].Index + 1;
            int to = s + 1 < subsections.Count ? subsections[s + 1].Index : body.Length;
            string? problem = SubsectionProblem(subsections[s].Name, body[from..to]);
            if (problem is not null)
            {
                return problem;
            }
        }

        if (desk)
        {
            string[] literals = id == "AC-5.1" ? AdrLiterals : SimulationLiterals;
            string joined = string.Join("\n", body);
            foreach (string literal in literals)
            {
                if (!joined.Contains(literal, System.StringComparison.Ordinal))
                {
                    return $"missing '{literal}'";
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Steps of one subsection are numbered 1..n with no gap, and each step chunk contains "Expected:".
    /// </summary>
    private static string? SubsectionProblem(string name, string[] lines)
    {
        List<int> steps = [];
        for (int i = 0; i < lines.Length; i++)
        {
            if (StepLine.IsMatch(lines[i]))
            {
                steps.Add(i);
            }
        }

        if (steps.Count == 0)
        {
            return $"{name}: no steps";
        }

        for (int k = 0; k < steps.Count; k++)
        {
            string number = StepLine.Match(lines[steps[k]]).Groups[1].Value;
            string expectedNumber = (k + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (number != expectedNumber)
            {
                return $"{name}: step {number} follows step {k}";
            }

            int to = k + 1 < steps.Count ? steps[k + 1] : lines.Length;
            bool hasExpected = false;
            for (int i = steps[k]; i < to; i++)
            {
                hasExpected |= lines[i].Contains("Expected:", System.StringComparison.Ordinal);
            }

            if (!hasExpected)
            {
                return $"{name}: step {number} has no Expected line";
            }
        }

        return null;
    }

    /// <summary>The lines after a heading at <paramref name="index"/>, up to the next "### " line or the section end.</summary>
    private static string[] BodyOf(string[] section, int index)
    {
        int end = index + 1;
        while (end < section.Length && !section[end].StartsWith("### ", System.StringComparison.Ordinal))
        {
            end++;
        }

        return section[(index + 1)..end];
    }

    private static string TrimTrailingBlanks(string line) => line.TrimEnd(' ', '\t');

    private sealed record Heading(string Id, string Line, int Index);

    private sealed record ScriptReport(
        string? HeadingProblem,
        string? PreviousHeading,
        string? NextHeading,
        string Preamble,
        IReadOnlyList<string> Missing,
        IReadOnlyList<string> Empty,
        IReadOnlyList<string> Order,
        IReadOnlyList<string> BadReferences);
}
