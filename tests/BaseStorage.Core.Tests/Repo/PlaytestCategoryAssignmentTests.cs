namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;

/// <summary>
/// GW-U-1320 (S-E004-004 AC-4, spec AC-7.3, AC-9.3): docs/PLAYTEST.md holds the "Category assignment" section before "Chest reservation",
/// with Solo and Host plus one friend steps that each end in an Expected line and name the button, both clicks, the hotkey and the notices;
/// package/README.md gains the player-facing section; CHANGELOG.md names the button. The game is never launched by the pipeline.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestCategoryAssignmentTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string HeadingPattern = @"^## Category assignment \(AC-4\.3, AC-8\.2\)[ \t]*$";
    private const string ReservationPattern = @"^## Chest reservation \(AC-5\.2, AC-5\.3, AC-6\.3\)[ \t]*$";
    private const string HoverPattern = @"^## Hover inspect \(AC-2\.6, AC-8\.3\)[ \t]*$";

    // GW-U-1320: exactly one heading, after Hover inspect and before Chest reservation.
    [Fact]
    public void TheSectionHeadingIsExactAndSitsBetweenHoverInspectAndChestReservation()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        MatchCollection headings = Regex.Matches(text, HeadingPattern, RegexOptions.Multiline);
        Match hover = Regex.Match(text, HoverPattern, RegexOptions.Multiline);
        Match reservation = Regex.Match(text, ReservationPattern, RegexOptions.Multiline);

        Assert.Single(headings);
        Assert.True(hover.Success && reservation.Success, "docs/PLAYTEST.md lost the Hover inspect or Chest reservation section.");
        Assert.True(headings[0].Index > hover.Index, "Category assignment must come after Hover inspect.");
        Assert.True(headings[0].Index < reservation.Index, "Category assignment must come before Chest reservation.");
    }

    // GW-U-1320: Solo then Host plus one friend, each a numbered list that starts at 1 with an Expected line per step.
    [Fact]
    public void SubsectionsAreNumberedFromOneWithAnExpectedLinePerStep()
    {
        string section = Section();
        int solo = section.IndexOf("### Solo", StringComparison.Ordinal);
        int host = section.IndexOf("### Host plus one friend", StringComparison.Ordinal);
        Assert.True(solo > 0 && host > solo, "Solo must come before Host plus one friend.");

        foreach (string subsection in new[] { "### Solo", "### Host plus one friend" })
        {
            string body = Subsection(section, subsection);
            Assert.Matches(@"(?m)^1\. ", body);
            var missing = new List<string>();
            string? open = null;
            foreach (string line in body.Split('\n'))
            {
                string trimmed = line.Trim();
                if (Regex.IsMatch(trimmed, @"^\d+\. "))
                {
                    if (open is not null)
                    {
                        missing.Add(open);
                    }

                    open = trimmed;
                }
                else if (trimmed.StartsWith("Expected:", StringComparison.Ordinal))
                {
                    open = null;
                }
            }

            if (open is not null)
            {
                missing.Add(open);
            }

            Assert.Empty(missing);
        }
    }

    // GW-U-1320: the steps name the control, both clicks, the hotkey setting, the cycle values and the two notices; no emoji.
    [Fact]
    public void TheSectionNamesTheContractLiterals()
    {
        string section = Section();

        foreach (string literal in new[] { "Category", "Auto", "Keep", "Left-click", "Right-click", "`CycleCategoryKey`", "Chest category: ", "Chest category not changed: ", "Take All", "`[Keys]`" })
        {
            Assert.Contains(literal, section, StringComparison.Ordinal);
        }

        Assert.DoesNotMatch(@"[\uD800-\uDFFF]|\p{So}", section);
    }

    // GW-U-1320 (spec AC-9.3): the player-facing README and the changelog name the button and the hotkey.
    [Fact]
    public void ReadmeAndChangelogDescribeTheButtonAndTheHotkey()
    {
        string readme = RepoRoot.ReadText("package/README.md");
        string changelog = RepoRoot.ReadText("CHANGELOG.md");

        Assert.Single(Regex.Matches(readme, @"^## Chest categories[ \t]*$", RegexOptions.Multiline));
        string readmeSection = ReadmeSection(readme);
        foreach (string literal in new[] { "Category", "Auto", "Keep", "Left-click", "Right-click", "`CycleCategoryKey`" })
        {
            Assert.Contains(literal, readmeSection, StringComparison.Ordinal);
        }

        // Edited by S-E009-001 (quality-audit QA4.7): the bullets now sit under the 0.1.0 release.
        Match unreleased = Regex.Match(changelog, @"(?m)^## \[0\.1\.0\]");
        Assert.True(unreleased.Success, "CHANGELOG.md has no 0.1.0 section.");
        string unreleasedText = changelog.Substring(unreleased.Index);
        Assert.Contains("Category button", unreleasedText, StringComparison.Ordinal);
        Assert.Contains("CycleCategoryKey", unreleasedText, StringComparison.Ordinal);
    }

    private static string Section()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        Match heading = Regex.Match(text, HeadingPattern, RegexOptions.Multiline);
        Assert.True(heading.Success, "docs/PLAYTEST.md has no exact '## Category assignment (AC-4.3, AC-8.2)' heading.");
        int start = heading.Index + heading.Length;
        Match next = Regex.Match(text.Substring(start), @"(?m)^## ");
        return next.Success ? text.Substring(start, next.Index) : text.Substring(start);
    }

    private static string Subsection(string section, string heading)
    {
        int start = section.IndexOf(heading, StringComparison.Ordinal);
        Assert.True(start >= 0, "The Category assignment section has no '" + heading + "' subsection.");
        int next = section.IndexOf("\n### ", start + heading.Length, StringComparison.Ordinal);
        return next < 0 ? section.Substring(start) : section.Substring(start, next - start);
    }

    private static string ReadmeSection(string readme)
    {
        Match heading = Regex.Match(readme, @"^## Chest categories[ \t]*$", RegexOptions.Multiline);
        int start = heading.Index + heading.Length;
        Match next = Regex.Match(readme.Substring(start), @"(?m)^## ");
        return next.Success ? readme.Substring(start, next.Index) : readme.Substring(start);
    }
}
