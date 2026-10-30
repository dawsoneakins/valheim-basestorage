namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// GW-U-2260..2264 (AC-7, AC-8, AC-9, base-fill contract 5.1 to 5.4): the documents describe the base fill status line. Red until
/// S-E010-001 implement writes docs/ADDED-BY-PIPEMASON.md and makes the in-place edits to package/README.md, docs/PLAYTEST.md and CHANGELOG.md.
/// The existing Playtest*, PlayerReadme and PlaytestCoverage pins are not edited and must stay green (ADR-GWIN-157).
/// </summary>
[Trait("Category", "Repo")]
public sealed class BaseFillDocsTests
{
    private const string AddedFile = "docs/ADDED-BY-PIPEMASON.md";
    private const string ReadmeFile = "package/README.md";
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string ChangelogFile = "CHANGELOG.md";
    private const string AddedHeading = "## Base fill statistics (S-E010-001)";
    private const string StationButtonsHeadingPattern = @"^## Station buttons \(AC-2\.4, AC-4\.7, AC-4\.10\)[ \t]*$";
    private const string AcceptanceHeading = "### 22. AC-4.10 - Station is the entry point";
    private const string ChangelogBullet =
        "- Base fill statistics: the status line on a crafting station panel now reads `Base: N stations, M chests, P% full`, where P is how many slots of the base's eligible chests hold an item, rounded to the nearest whole percent (`0% full` when the base has no eligible chest).";

    // GW-U-2260 (AC-7, contract 5.1): the summary file has the Pipemason title, a preamble of 1 to 4 lines, and one base fill section with the required literals.
    [Fact]
    public void AddedByPipemasonHasTheTitlePreambleAndTheFillSection()
    {
        Assert.True(RepoRoot.Exists(AddedFile), AddedFile + " is missing.");
        string text = RepoRoot.ReadText(AddedFile);
        string[] lines = text.Split('\n');

        Assert.Equal("# Added by Pipemason", lines[0]);

        int heading = Array.IndexOf(lines, AddedHeading);
        Assert.True(heading > 0, "The heading " + AddedHeading + " is missing.");
        Assert.Equal(1, lines.Count(line => line == AddedHeading));

        int preambleLines = lines.Take(heading).Count(line => line.Trim().Length > 0) - 1;
        Assert.InRange(preambleLines, 1, 4);

        string section = string.Join("\n", lines.Skip(heading));
        foreach (string literal in new[] { "Base: N stations, M chests, P% full", "eligible", "occupied", "nearest whole percent", "0% full", "Keep", "StationButtons" })
        {
            Assert.Contains(literal, section, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("BepInEx/", text, StringComparison.Ordinal);
        foreach (char character in text)
        {
            Assert.False(char.IsSurrogate(character), "The file has a surrogate pair.");
            Assert.False(character >= '☀' && character <= '➿', "The file has a symbol from U+2600 to U+27BF.");
        }
    }

    // GW-U-2261 (AC-8, contract 5.2): the Station buttons section of the README shows the new example, the P% full wording and the rounding rule.
    [Fact]
    public void ReadmeStationButtonsShowsTheFillExample()
    {
        string text = RepoRoot.ReadText(ReadmeFile);
        int chestCategories = text.IndexOf("## Chest categories", StringComparison.Ordinal);
        int start = text.IndexOf("## Station buttons", StringComparison.Ordinal);
        int end = text.IndexOf("## Known limits", StringComparison.Ordinal);
        Assert.True(chestCategories >= 0 && start > chestCategories && end > start, "The Station buttons section must sit between Chest categories and Known limits.");

        string section = text.Substring(start, end - start);
        Assert.Contains("`Base: 6 stations, 41 chests, 64% full`", section, StringComparison.Ordinal);
        Assert.Contains("P% full", section, StringComparison.Ordinal);
        Assert.Contains("eligible", section, StringComparison.Ordinal);
        Assert.Contains("nearest whole percent", section, StringComparison.Ordinal);
        Assert.DoesNotContain("`Base: 6 stations, 41 chests`", section, StringComparison.Ordinal);
    }

    // GW-U-2262 (AC-9, contract 5.3 (a)): the Station buttons preamble names the fill, and the Solo steps 2, 3, 7 and 10 show their fill values.
    [Fact]
    public void PlaytestStationButtonsShowsTheFillInThePreambleAndTheSoloSteps()
    {
        string section = StationButtonsSection();
        string preamble = section.Substring(0, section.IndexOf("### Solo", StringComparison.Ordinal));
        Assert.Contains("`Base: N stations, M chests, P% full`", preamble, StringComparison.Ordinal);

        string solo = Between(section, "### Solo", "### Host plus one friend");
        Assert.Contains("`Base: 1 station, 0 chests, 0% full`", ExpectedOf(StepBlock(solo, 2)), StringComparison.Ordinal);
        Assert.Contains("`Base: 1 station, 3 chests, <P>% full`", ExpectedOf(StepBlock(solo, 3)), StringComparison.Ordinal);
        Assert.Contains("`Base: 2 stations, 3 chests, <P>% full`", ExpectedOf(StepBlock(solo, 7)), StringComparison.Ordinal);
        Assert.Contains("`Base: 1 station, 0 chests, 0% full`", ExpectedOf(StepBlock(solo, 10)), StringComparison.Ordinal);
    }

    // GW-U-2263 (AC-9, contract 5.3 (b), playtest-coverage 7): acceptance script 22 shows the fill on step 1 and walks the fill in step 3 of both runs.
    [Fact]
    public void AcceptanceScriptTwentyTwoWalksTheFillInStepThree()
    {
        string section = AcceptanceSection();
        string solo = Between(section, "#### Solo", "#### Host plus one friend");
        string host = Between(section, "#### Host plus one friend", null);

        Assert.Contains("`Base: N stations, M chests, P% full`", ExpectedOf(StepBlock(solo, 1)), StringComparison.Ordinal);

        string soloThree = ExpectedOf(StepBlock(solo, 3));
        int at = 0;
        foreach (string literal in new[]
                 {
                     "`Base: 1 station, 3 chests, 0% full`",
                     "`Base: 1 station, 3 chests, 3% full`",
                     "`Base: 1 station, 3 chests, 7% full`",
                     "`Base: 1 station, 3 chests, 100% full`",
                     "`Base: 1 station, 2 chests, 100% full`",
                 })
        {
            int found = soloThree.IndexOf(literal, at, StringComparison.Ordinal);
            Assert.True(found >= 0, "Solo step 3 lacks " + literal + " in order.");
            at = found + literal.Length;
        }

        Assert.DoesNotContain("steps of", StepBlock(solo, 3), StringComparison.Ordinal);
        Assert.Contains("`Base: 1 station, 3 chests, 3% full`", ExpectedOf(StepBlock(host, 3)), StringComparison.Ordinal);
        Assert.DoesNotContain("steps of", StepBlock(host, 3), StringComparison.Ordinal);
    }

    // GW-U-2264 (AC-7, contract 5.4, source AC-9.3): the CHANGELOG bullet sits under Unreleased and Added, directly after the Put away bullet.
    [Fact]
    public void ChangelogAddsTheFillBulletDirectlyAfterPutAway()
    {
        string[] lines = RepoRoot.ReadText(ChangelogFile).Split('\n');
        // Edited by S-E009-001 (quality-audit QA4.7): the bullets now sit under the 0.1.0 release.
        int unreleased = Array.FindIndex(lines, line => line.StartsWith("## [0.1.0]", StringComparison.Ordinal));
        Assert.True(unreleased >= 0, "The 0.1.0 heading is missing.");

        int putAway = Array.FindIndex(lines, line => line.StartsWith("- Put away inventory button next to Organize base", StringComparison.Ordinal));
        Assert.True(putAway > unreleased, "The Put away bullet is not under 0.1.0.");

        int nextRelease = Array.FindIndex(lines, unreleased + 1, line => line.StartsWith("## ", StringComparison.Ordinal));
        Assert.True(nextRelease < 0 || putAway < nextRelease, "The Put away bullet is under a later release.");

        Assert.Equal(ChangelogBullet, lines[putAway + 1]);
    }

    private static string StationButtonsSection()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        Match heading = Regex.Match(text, StationButtonsHeadingPattern, RegexOptions.Multiline);
        Assert.True(heading.Success, "The Station buttons heading is missing.");

        Match next = new Regex("^## ", RegexOptions.Multiline).Match(text, heading.Index + heading.Length);
        int end = next.Success ? next.Index : text.Length;
        return text.Substring(heading.Index, end - heading.Index);
    }

    private static string AcceptanceSection()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        int start = text.IndexOf(AcceptanceHeading, StringComparison.Ordinal);
        Assert.True(start >= 0, AcceptanceHeading + " is missing.");

        Match next = new Regex("^#{2,3} ", RegexOptions.Multiline).Match(text, start + 1);
        int end = next.Success ? next.Index : text.Length;
        return text.Substring(start, end - start);
    }

    private static string Between(string text, string startMarker, string? endMarker)
    {
        int start = text.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, startMarker + " is missing.");

        int end = endMarker is null ? -1 : text.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        return end >= 0 ? text.Substring(start, end - start) : text.Substring(start);
    }

    private static string StepBlock(string subsection, int step)
    {
        string[] lines = subsection.Split('\n');
        int index = Array.FindIndex(lines, line => line.StartsWith(step + ". ", StringComparison.Ordinal));
        Assert.True(index >= 0, "Step " + step + " is missing.");

        int end = index + 1;
        while (end < lines.Length && !Regex.IsMatch(lines[end], @"^\d+\. "))
        {
            end++;
        }

        return string.Join("\n", lines.Skip(index).Take(end - index));
    }

    private static string ExpectedOf(string block)
    {
        int at = block.IndexOf("Expected:", StringComparison.Ordinal);
        Assert.True(at >= 0, "A step has no Expected line.");
        return block.Substring(at);
    }
}
