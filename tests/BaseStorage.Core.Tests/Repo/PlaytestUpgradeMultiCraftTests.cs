namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// GW-U-1724 to GW-U-1729 (AC-8; contract upgrade-multicraft 8 P1-P9): the docs/PLAYTEST.md section "Upgrade and multi-craft from base
/// (AC-3.5)". The section is read CRLF-normalised. Its append-only rule is checked against the merge base with main, the way GW-U-1549 does
/// it, so the check is stack-safe on a stacked branch.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestUpgradeMultiCraftTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string Heading = "## Upgrade and multi-craft from base (AC-3.5)";
    private const string RequirementHeading = "## Requirement display (AC-3.2)";
    private const string Solo = "### Solo";
    private const string Host = "### Host plus one friend";
    private const string RequirementSentence = "Upgrade and multi-craft amounts are covered by their own section when they are added.";

    // GW-U-1724 (AC-8, P1): exactly one Upgrade and multi-craft heading, directly after the Requirement display section, and it is the last level-2 section.
    // Edited by integration (S-E006-004): Config sync is appended after this section, so it is no longer the last level-2 section.
    [Fact]
    public void TheHeadingIsOnceDirectlyAfterRequirementDisplay()
    {
        string text = RepoRoot.Normalise(RepoRoot.ReadText(PlaytestFile));
        MatchCollection headings = Regex.Matches(text, @"^## Upgrade and multi-craft from base \(AC-3\.5\)[ \t]*$", RegexOptions.Multiline);
        Assert.Single(headings);

        string[] levelTwo = Regex.Matches(text, @"(?m)^## .*$").Select(match => match.Value.TrimEnd()).ToArray();
        int index = Array.IndexOf(levelTwo, Heading);
        Assert.True(index > 0, "The Upgrade and multi-craft section is missing or first.");
        Assert.Equal(RequirementHeading, levelTwo[index - 1]);
    }

    // GW-U-1725 (AC-8, P2): the preamble quotes the Craft from base heading and names the path, the display, the modifier key, the default, the setting, the notice, the log lines and the upgrader stations.
    [Fact]
    public void ThePreambleStatesTheSharedPathTheDisplayTheKeyTheSettingAndTheNotice()
    {
        string preamble = Section().Split(new[] { Solo }, StringSplitOptions.None)[0];

        foreach (string literal in new[]
        {
            "## Craft from base (AC-2.4, AC-3.1, AC-3.4)",
            "<inventory> (+<chests>) / <need>",
            "Left Shift",
            "AltPlace",
            "5 by default",
            "several at once",
            "`CraftFromBase`",
            "[General]",
            "Materials moved, try again",
            "Reserve <id> [craft]",
            "upgrader stations",
        })
        {
            Assert.Contains(literal, preamble, StringComparison.Ordinal);
        }
    }

    // GW-U-1726 (AC-8, P3, P4): Solo is a numbered list from 1 with an Expected line per step, and its steps cover the cases in order.
    [Fact]
    public void TheSoloStepsAreNumberedWithExpectedLinesAndCoverTheCasesInOrder()
    {
        string solo = Subsection(Solo, Host);

        string[] blocks = NumberedSteps(solo);
        Assert.True(blocks.Length >= 7, "Solo needs at least seven numbered steps.");
        for (int i = 0; i < blocks.Length; i++)
        {
            Assert.StartsWith($"{i + 1}. ", blocks[i], StringComparison.Ordinal);
            Assert.Matches(@"(?m)^\s*Expected:", blocks[i]);
        }

        AssertInOrder(solo, "upgrade", "one less", "exact", "40", "39", "CraftFromBase", "30 seconds");
    }

    // GW-U-1727 (AC-8, P3, P5): Host plus one friend is a numbered list from 1 with an Expected line per step, and its steps cover the friend, the race, the swapped roles and the idle step in order.
    [Fact]
    public void TheHostAndFriendStepsAreNumberedAndCoverTheRaceInOrder()
    {
        string host = Subsection(Host, null);

        string[] blocks = NumberedSteps(host);
        Assert.True(blocks.Length >= 5, "Host plus one friend needs at least five numbered steps.");
        for (int i = 0; i < blocks.Length; i++)
        {
            Assert.StartsWith($"{i + 1}. ", blocks[i], StringComparison.Ordinal);
            Assert.Matches(@"(?m)^\s*Expected:", blocks[i]);
        }

        AssertInOrder(host, "route SendRequest", "Left Shift", "ReserveTimeoutMs = 20000", "roles swapped", "30 seconds");
    }

    // GW-U-1728 (AC-8, P6, P7, P8): each subsection ends with the failed-and-skipped and 30-second idle step, the unit-test note is present, the literals are present, setting names are in backticks, and there is no emoji.
    [Fact]
    public void TheWordingHasTheIdleStepsTheLiteralsAndNoEmoji()
    {
        foreach (string subsection in new[] { Subsection(Solo, Host), Subsection(Host, null) })
        {
            string last = NumberedSteps(subsection).Last();
            Assert.Contains("failed and was skipped", last, StringComparison.Ordinal);
            Assert.Contains("30 seconds", last, StringComparison.Ordinal);
        }

        string section = Section();
        Assert.Contains("unit test", section, StringComparison.OrdinalIgnoreCase);
        foreach (string literal in new[]
        {
            "Materials moved, try again",
            "CraftFromBase",
            "Craft <token>: Created",
            "[craft]",
            "route SendRequest",
            "route LocalOwner",
            "ReserveTimeoutMs",
            "<inventory> (+<chests>) / <need>",
            "AltPlace",
            "Left Shift",
        })
        {
            Assert.Contains(literal, section, StringComparison.Ordinal);
        }

        Assert.Contains("`CraftFromBase`", section, StringComparison.Ordinal);
        Assert.Contains("`ReserveTimeoutMs`", section, StringComparison.Ordinal);
        Assert.False(section.Any(ch => char.IsSurrogate(ch) || (ch >= '←' && ch <= '⯿')), "The section contains an emoji or a symbol block character.");
    }

    // GW-U-1729 (AC-8, P9, append-only and stack-safe): the file only grows against the merge base with main, the new heading is in the appended remainder, the level-2 heading before it is Requirement display, and that section still ends with its frozen sentence.
    [Fact]
    public void ThePlaytestFileOnlyGrowsWithTheUpgradeSectionAppendedAfterRequirementDisplay()
    {
        string baseText = RepoRoot.Normalise(MergeBaseFile(PlaytestFile));
        string current = RepoRoot.Normalise(RepoRoot.ReadText(PlaytestFile));

        // Edited by integration (S-E004-004 + S-E004-011): once several stories land together, other stories insert their sections in
        // the middle of the file, and after main carries this section the merge base already holds it. The check is now that no level-2
        // section of the merge base was removed and that the heading appears exactly once (new against the merge base, or carried by it).
        string[] levelTwo = Regex.Matches(current, @"(?m)^## .*$").Select(match => match.Value.TrimEnd()).ToArray();
        string[] baseLevelTwo = Regex.Matches(baseText, @"(?m)^## .*$").Select(match => match.Value.TrimEnd()).ToArray();
        Assert.All(baseLevelTwo, heading => Assert.Contains(heading, levelTwo));
        Assert.Single(Regex.Matches(current, @"(?m)^## Upgrade and multi-craft from base \(AC-3\.5\)[ \t]*$"));
        Assert.True(Regex.Matches(baseText, @"(?m)^## Upgrade and multi-craft from base \(AC-3\.5\)[ \t]*$").Count <= 1, "The merge base carries the Upgrade and multi-craft heading more than once.");

        int index = Array.IndexOf(levelTwo, Heading);
        Assert.True(index > 0, "The Upgrade and multi-craft heading is missing.");
        Assert.Equal(RequirementHeading, levelTwo[index - 1]);

        string requirement = SectionOf(current, RequirementHeading);
        Assert.Contains(RequirementSentence, requirement, StringComparison.Ordinal);
        Assert.EndsWith(RequirementSentence, requirement.TrimEnd(), StringComparison.Ordinal);
    }

    private static string Section() => SectionOf(RepoRoot.Normalise(RepoRoot.ReadText(PlaytestFile)), Heading);

    private static string SectionOf(string text, string heading)
    {
        int start = text.IndexOf(heading, StringComparison.Ordinal);
        Assert.True(start >= 0, $"{PlaytestFile} has no {heading} section.");

        string after = text.Substring(start + heading.Length);
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

    private static string[] NumberedSteps(string subsection) =>
        Regex.Split(subsection, @"(?m)^(?=\d+\. )").Where(block => Regex.IsMatch(block, @"^\d+\. ")).ToArray();

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
