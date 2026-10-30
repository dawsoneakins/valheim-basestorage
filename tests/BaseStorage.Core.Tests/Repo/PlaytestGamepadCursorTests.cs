namespace BaseStorage.Core.Tests.Repo;

using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// GW-U-2220 to GW-U-2224 (AC-3, AC-4, spec AC-7.3, AC-8.2; contract hotkey-hover-audit-contracts v1 section 3, GP1 to GP4): docs/PLAYTEST.md holds a
/// "Gamepad cursor" section between Category assignment and Organize, with a Solo subsection of numbered steps that each end in an Expected line,
/// that names the station and chest-panel buttons, and that says there is no dedicated gamepad binding in v1. package/README.md keeps its
/// "No gamepad bindings" known limit. The game is never launched by the pipeline.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestGamepadCursorTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string PackageReadme = "package/README.md";
    private const string HeadingPattern = @"^## Gamepad cursor \(AC-8\.2, AC-7\.3\)[ \t]*$";
    private const string PreviousHeading = "## Category assignment (AC-4.3, AC-8.2)";
    private const string NextHeading = "## Organize (AC-2.4, AC-4.7, AC-4.8, AC-5.4)";
    private const string SiblingHeading = "## Organize console command (AC-2.4, AC-5.4)";

    // GW-U-2220 (AC-3): exactly one heading, directly after Category assignment and directly before Organize.
    [Fact]
    public void TheGamepadCursorHeadingIsExactAndSitsBetweenCategoryAssignmentAndOrganize()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        MatchCollection headings = Regex.Matches(text, HeadingPattern, RegexOptions.Multiline);
        Assert.Single(headings);

        MatchCollection level2 = Regex.Matches(text, @"(?m)^## .*$");
        int index = -1;
        for (int i = 0; i < level2.Count; i++)
        {
            if (level2[i].Index == headings[0].Index)
            {
                index = i;
            }
        }

        Assert.True(index > 0 && index < level2.Count - 1, "The Gamepad cursor heading must have a level-2 heading before and after it.");

        // Integration 2026-10-29: the S-E010-002 Organize console command section shares this slot, so it may sit on either side.
        int previous = index - 1;
        int next = index + 1;
        if (level2[previous].Value.StartsWith(SiblingHeading, StringComparison.Ordinal))
        {
            previous--;
        }

        if (next < level2.Count - 1 && level2[next].Value.StartsWith(SiblingHeading, StringComparison.Ordinal))
        {
            next++;
        }

        Assert.True(previous >= 0, "The Gamepad cursor heading must have Category assignment before it.");
        Assert.Equal(PreviousHeading, level2[previous].Value.TrimEnd());
        Assert.Equal(NextHeading, level2[next].Value.TrimEnd());
    }

    // GW-U-2221 (AC-3): the Solo subsection has at least three steps numbered 1..n with no gap, and each step has an Expected line.
    [Fact]
    public void TheSoloStepsAreNumberedFromOneWithAnExpectedLinePerStep()
    {
        string solo = Subsection(Section(), "### Solo");
        var steps = new List<(int Number, bool HasExpected)>();
        foreach (string raw in solo.Split('\n'))
        {
            string line = raw.Trim();
            Match step = Regex.Match(line, @"^(\d+)\. ");
            if (step.Success)
            {
                steps.Add((int.Parse(step.Groups[1].Value, CultureInfo.InvariantCulture), false));
            }
            else if (line.StartsWith("Expected:", StringComparison.Ordinal) && steps.Count > 0)
            {
                int last = steps.Count - 1;
                steps[last] = (steps[last].Number, true);
            }
        }

        Assert.True(steps.Count >= 3, "The Solo subsection needs at least three numbered steps.");
        for (int i = 0; i < steps.Count; i++)
        {
            Assert.True(steps[i].Number == i + 1, "Solo step " + (i + 1) + " is numbered " + steps[i].Number + "; numbers must run 1 to n with no gap.");
        }

        var missing = new List<int>();
        foreach ((int number, bool hasExpected) in steps)
        {
            if (!hasExpected)
            {
                missing.Add(number);
            }
        }

        Assert.True(missing.Count == 0, "Solo steps without an Expected line: " + string.Join(", ", missing));
    }

    // GW-U-2222 (AC-3): the section names the station buttons, the chest-panel Category button and the gamepad cursor.
    [Fact]
    public void TheSectionNamesTheStationAndChestPanelButtons()
    {
        string section = Section();

        foreach (string literal in new[] { "Organize base", "Put away inventory", "Category", "gamepad cursor" })
        {
            Assert.Contains(literal, section, StringComparison.Ordinal);
        }
    }

    // GW-U-2223 (AC-4): the preamble, before the Solo subsection, says there is no dedicated gamepad binding in v1.
    [Fact]
    public void ThePreambleSaysThereIsNoDedicatedGamepadBindingInV1()
    {
        string section = Section();
        int solo = section.IndexOf("### Solo", StringComparison.Ordinal);
        Assert.True(solo > 0, "The Gamepad cursor section has no Solo subsection to end its preamble.");

        Assert.Contains("no dedicated gamepad binding", section.Substring(0, solo), StringComparison.OrdinalIgnoreCase);
    }

    // GW-U-2224 (AC-4): the player README known limits still say there are no gamepad bindings.
    [Fact]
    public void ThePlayerReadmeKnownLimitsStillSaysNoGamepadBindings()
    {
        string limits = KnownLimits(RepoRoot.ReadText(PackageReadme));

        Assert.Contains("No gamepad bindings", limits, StringComparison.Ordinal);
    }

    private static string Section()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        Match heading = Regex.Match(text, HeadingPattern, RegexOptions.Multiline);
        Assert.True(heading.Success, "docs/PLAYTEST.md has no exact '## Gamepad cursor (AC-8.2, AC-7.3)' heading.");
        int start = heading.Index + heading.Length;
        Match next = Regex.Match(text.Substring(start), @"(?m)^## ");
        return next.Success ? text.Substring(start, next.Index) : text.Substring(start);
    }

    private static string Subsection(string section, string heading)
    {
        int start = section.IndexOf(heading, StringComparison.Ordinal);
        Assert.True(start >= 0, "The Gamepad cursor section has no '" + heading + "' subsection.");
        int next = section.IndexOf("\n### ", start + heading.Length, StringComparison.Ordinal);
        return next < 0 ? section.Substring(start) : section.Substring(start, next - start);
    }

    private static string KnownLimits(string readme)
    {
        Match heading = Regex.Match(readme, @"^## Known limits[ \t]*$", RegexOptions.Multiline);
        Assert.True(heading.Success, "package/README.md has no '## Known limits' section.");
        int start = heading.Index + heading.Length;
        Match next = Regex.Match(readme.Substring(start), @"(?m)^## ");
        return next.Success ? readme.Substring(start, next.Index) : readme.Substring(start);
    }
}
