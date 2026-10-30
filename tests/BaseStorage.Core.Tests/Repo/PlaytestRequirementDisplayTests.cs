namespace BaseStorage.Core.Tests.Repo;

using System.Diagnostics;
using System.Text.RegularExpressions;

/// <summary>
/// GW-U-1546 to GW-U-1549 (AC-7; contract 9 P1-P8): the docs/PLAYTEST.md section "Requirement display (AC-3.2)". The section is read
/// CRLF-normalised. Its append-only rule is checked against the merge base with main, the way GW-C-1503 does. The check is stack-safe: on a stacked branch the appended remainder may begin with the base branch's own sections.
/// </summary>
public sealed class PlaytestRequirementDisplayTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string Heading = "## Requirement display (AC-3.2)";
    private const string CraftHeading = "## Craft from base (AC-2.4, AC-3.1, AC-3.4)";
    private const string BuildHeading = "## Build from base (AC-2.4, AC-3.1, AC-3.4)";

    // GW-U-1546 (P1): exactly one heading, directly after the Craft from base section, and it is the last level-2 section.
    // Edited by integration (S-E003-005 + S-E003-006): Build from base (S-E003-006) sits between Craft from base and Requirement display.
    // Edited by integration (S-E006-004): Config sync is appended after the S-E003 sections, so Upgrade and multi-craft is no longer the last one.
    [Fact]
    public void TheHeadingIsOnceDirectlyAfterBuildFromBaseAndBeforeUpgradeAndMultiCraft()
    {
        string text = RepoRoot.Normalise(RepoRoot.ReadText(PlaytestFile));
        MatchCollection headings = Regex.Matches(text, @"^## Requirement display \(AC-3\.2\)[ \t]*$", RegexOptions.Multiline);
        Assert.Single(headings);

        int craft = text.IndexOf(CraftHeading, StringComparison.Ordinal);
        Assert.True(craft >= 0, "The Craft from base section is missing.");

        string[] afterCraft = Regex.Matches(text.Substring(craft + CraftHeading.Length), @"(?m)^## .*$").Select(match => match.Value.TrimEnd()).Take(2).ToArray();
        Assert.Equal(new[] { BuildHeading, Heading }, afterCraft);

        // Edited by S-E003-007, ADR-GWIN-128: Requirement display is directly followed by Upgrade and multi-craft from base (AC-3.5), which is the last level-2 section.
        Match followedBy = Regex.Match(text.Substring(headings[0].Index + headings[0].Length), @"(?m)^## .*$");
        Assert.Equal("## Upgrade and multi-craft from base (AC-3.5)", followedBy.Value.TrimEnd());
    }

    // GW-U-1546 (P2): the preamble quotes the example, says no-chest lines are vanilla, names CraftFromBase, says no log line is written, and points to Craft from base step 5.
    // Edited by integration (S-E003-005 + S-E003-006): with build-from-base placing no longer counts only the inventory; the note points to Build from base instead.
    [Fact]
    public void ThePreambleStatesTheDisplayRulesAndTheBuildPlacementNote()
    {
        string preamble = Section().Split(new[] { "### Solo" }, StringSplitOptions.None)[0];

        foreach (string literal in new[] { "12 (+40) / 20", "vanilla", "`CraftFromBase`", "[General]", "log", "Craft from base (AC-2.4, AC-3.1, AC-3.4)", "step 5", "Build from base (AC-2.4, AC-3.1, AC-3.4)" })
        {
            Assert.Contains(literal, preamble, StringComparison.Ordinal);
        }
    }

    // GW-U-1547 (P3, P4, P6): Solo is a numbered list from 1, each step has an Expected line, and the steps cover the cases in order.
    [Fact]
    public void TheSoloStepsAreNumberedWithExpectedLinesInOrder()
    {
        string solo = Subsection("### Solo", "### Host plus one friend");

        string[] blocks = Regex.Split(solo, @"(?m)^(?=\d+\. )").Where(block => Regex.IsMatch(block, @"^\d+\. ")).ToArray();
        Assert.True(blocks.Length >= 7, "Solo needs at least seven numbered steps.");
        for (int i = 0; i < blocks.Length; i++)
        {
            Assert.StartsWith($"{i + 1}. ", blocks[i], StringComparison.Ordinal);
            Assert.Matches(@"(?m)^\s*Expected:", blocks[i]);
        }

        AssertInOrder(solo, "surplus", "short", "exact", "no chest", "vanilla", "tooltip", "30 seconds");
    }

    // GW-U-1548 (P5, P6, P7): Host plus one friend covers the friend view, the excluded and re-included chest, +10 Wood, the friend's tooltip and the idle step, in order.
    [Fact]
    public void TheHostPlusFriendStepsCoverTheFriendViewInOrder()
    {
        string host = Subsection("### Host plus one friend", null);

        AssertInOrder(host, "(+<chests>)", "2 seconds", "10 Wood", "tooltip", "30 seconds");
        Assert.Contains("unit test", host, StringComparison.OrdinalIgnoreCase);
    }

    // GW-U-1548 (P7, P8): the required literals appear in the section, the setting names are in backticks, and the section has no emoji.
    [Fact]
    public void TheRequiredLiteralsAppearAndTheSectionHasNoEmoji()
    {
        string section = Section();

        foreach (string literal in new[] { "12 (+40) / 20", "(+<chests>)", "`CraftFromBase`", "failed and was skipped", "30 seconds" })
        {
            Assert.Contains(literal, section, StringComparison.Ordinal);
        }

        Assert.False(section.Any(ch => char.IsSurrogate(ch) || (ch >= '←' && ch <= '⯿')), "The section contains an emoji or a symbol block character.");
    }

    // GW-U-1549 (AC-7, append-only): edited by S-E003-005 to be stack-safe. The branch can sit on a base branch whose own PLAYTEST.md
    // sections are not yet on main, so the appended remainder (against the merge base with main) may start with those sections.
    // Edited by integration (S-E003-005 + S-E003-006 + later stories): once several stories land together, other stories insert
    // their sections in the middle of the file and, after main carries this section, the merge base already holds it. The test
    // now asserts that no level-2 section of the merge base was removed, that the Requirement display heading appears once, that it
    // is new against the merge base unless the merge base already carries it, and that the level-2 heading right before it is
    // Build from base (contract P1 as integrated).
    [Fact]
    public void ThePlaytestFileIsAnAppendOfTheRequirementDisplaySection()
    {
        string baseText = RepoRoot.Normalise(MergeBaseFile(PlaytestFile));
        string current = RepoRoot.Normalise(RepoRoot.ReadText(PlaytestFile));

        string[] baseLevelTwo = Regex.Matches(baseText, @"(?m)^## .*$").Select(match => match.Value.TrimEnd()).ToArray();
        string[] levelTwo = Regex.Matches(current, @"(?m)^## .*$").Select(match => match.Value.TrimEnd()).ToArray();
        Assert.All(baseLevelTwo, heading => Assert.Contains(heading, levelTwo));

        const string HeadingPattern = @"(?m)^## Requirement display \(AC-3\.2\)[ \t]*$";
        Assert.Single(Regex.Matches(current, HeadingPattern));
        Assert.True(Regex.Matches(baseText, HeadingPattern).Count <= 1, "The merge base carries the Requirement display heading more than once.");

        int index = Array.IndexOf(levelTwo, Heading);
        Assert.True(index > 0, "The Requirement display heading is missing.");
        Assert.Equal(BuildHeading, levelTwo[index - 1]);
    }

    // GW-U-1549 (edited pins, Craft from base step counting): the Craft from base section ends at the next level-2 heading, so the requirement steps are not its steps.
    [Fact]
    public void TheCraftFromBaseSectionDoesNotCountTheRequirementSteps()
    {
        string text = RepoRoot.Normalise(RepoRoot.ReadText(PlaytestFile));
        int craft = text.IndexOf(CraftHeading, StringComparison.Ordinal);
        Assert.True(craft >= 0, "The Craft from base section is missing.");

        string afterCraft = text.Substring(craft + CraftHeading.Length);
        Match nextLevelTwo = Regex.Match(afterCraft, @"(?m)^## .*$");
        string craftSection = nextLevelTwo.Success ? afterCraft.Substring(0, nextLevelTwo.Index) : afterCraft;

        Assert.DoesNotContain("Requirement display", craftSection, StringComparison.Ordinal);
        Assert.DoesNotContain("(+<chests>)", craftSection, StringComparison.Ordinal);
    }

    private static string Section()
    {
        string text = RepoRoot.Normalise(RepoRoot.ReadText(PlaytestFile));
        int start = text.IndexOf(Heading, StringComparison.Ordinal);
        Assert.True(start >= 0, $"{PlaytestFile} has no {Heading} section.");

        string after = text.Substring(start + Heading.Length);
        Match next = Regex.Match(after, @"(?m)^## .*$");
        return next.Success ? after.Substring(0, next.Index) : after;
    }

    private static string Subsection(string start, string? end)
    {
        string section = Section();
        int begin = section.IndexOf(start, StringComparison.Ordinal);
        Assert.True(begin >= 0, $"The section has no {start} subsection.");

        string after = section.Substring(begin + start.Length);
        if (end is null)
        {
            return after;
        }

        int stop = after.IndexOf(end, StringComparison.Ordinal);
        Assert.True(stop >= 0, $"The section has no {end} subsection after {start}.");
        return after.Substring(0, stop);
    }

    private static void AssertInOrder(string text, params string[] literals)
    {
        int position = -1;
        foreach (string literal in literals)
        {
            int found = text.IndexOf(literal, position + 1, StringComparison.OrdinalIgnoreCase);
            Assert.True(found > position, $"'{literal}' is missing or out of order.");
            position = found;
        }
    }

    private static string MergeBaseFile(string relative)
    {
        string mergeBase = Git("merge-base HEAD main").Trim();
        return Git("show " + mergeBase + ":" + relative);
    }

    private static string Git(string arguments)
    {
        var start = new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = RepoRoot.FullPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using Process process = Process.Start(start) ?? throw new InvalidOperationException("git did not start.");
        string output = process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.Equal(0, process.ExitCode);
        return output;
    }
}
