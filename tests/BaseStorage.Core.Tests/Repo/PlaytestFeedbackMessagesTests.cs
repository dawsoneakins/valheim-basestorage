namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Tests.Ui;
using BaseStorage.Core.Ui;

/// <summary>
/// GW-U-2130 to GW-U-2135 (AC-3, AC-7.3; contract 6 PT1-PT7, 6.1 and 6.2): the "Feedback messages" section of docs/PLAYTEST.md and
/// the CHANGELOG line. Every row is checked against <see cref="FeedbackCatalog.All"/> and the contract fixture. Trait "Repo".
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestFeedbackMessagesTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string ChangelogFile = "CHANGELOG.md";
    private const string Heading = "## Feedback messages (AC-8.1, AC-7.3)";
    private const string ConfigSyncHeading = "## Config sync (AC-6.2, AC-6.3)";
    private const string TableHeader = "| Outcome | Action | How to trigger | Expected message | Where |";
    private const string Preamble = "Every action ends in exactly one short MessageHud line whose text comes from the strings table (English only). For each row, do the trigger and check that exactly one line appears, with this wording, in this place; numbers and names depend on your base. \"top left\" and \"centre\" are BaseStorage lines; \"game\" means the game's own line and no BaseStorage line; \"none\" means no line at all.";
    private const string VanillaExpected = "the game's own message; no BaseStorage message";
    private const string ChangelogLine = "- Crafting and building with materials from base chests now confirm with one short message, and every action ends in exactly one message from the strings table.";

    /// <summary>Row indices of the 43 outcomes, one theory case each.</summary>
    public static TheoryData<int> RowIndices()
    {
        var data = new TheoryData<int>();
        for (int index = 0; index < FeedbackFixtures.Rows.Length; index++)
        {
            data.Add(index);
        }

        return data;
    }

    // GW-U-2130 (PT1): exactly one heading, immediately before Config sync, which stays the last level-2 heading.
    [Fact]
    public void OneFeedbackHeadingSitsImmediatelyBeforeConfigSync()
    {
        string[] headings = Level2Headings(RepoRoot.ReadText(PlaytestFile));

        Assert.Single(headings, line => line == Heading);
        int index = Array.IndexOf(headings, Heading);
        Assert.True(index + 1 < headings.Length, "The Feedback messages heading has no section after it.");
        Assert.Equal(ConfigSyncHeading, headings[index + 1]);
        Assert.Equal(ConfigSyncHeading, headings[headings.Length - 1]);
    }

    // GW-U-2131 (PT2): the first paragraph under the heading is exactly the contract preamble.
    [Fact]
    public void TheFirstParagraphIsTheContractPreamble()
    {
        string[] section = SectionLines(RepoRoot.ReadText(PlaytestFile).Split('\n'));
        string paragraph = string.Join("\n", section.SkipWhile(line => line.Trim().Length == 0).TakeWhile(line => line.Trim().Length > 0)).Trim();

        Assert.Equal(Preamble, paragraph);
    }

    // GW-U-2132 (PT3): the table header is exact, and a separator row with five columns follows it.
    [Fact]
    public void TheTableHeaderIsExactAndAFiveColumnSeparatorFollowsIt()
    {
        string[] section = SectionLines(RepoRoot.ReadText(PlaytestFile).Split('\n'));
        int header = Array.FindIndex(section, line => line.Trim() == TableHeader);

        Assert.True(header >= 0, "The Feedback messages section has no exact table header.");
        Assert.Matches(@"^\|(\s*-+\s*\|){5}$", section[header + 1].Trim());
    }

    // GW-U-2133 (PT4, PT5): row N is the outcome N in ascending order, with its action, expected line and place.
    [Theory]
    [MemberData(nameof(RowIndices))]
    public void RowRendersItsOutcomeActionExpectedAndWhere(int index)
    {
        string[][] rows = TableRows(RepoRoot.ReadText(PlaytestFile).Split('\n'));
        Assert.Equal(FeedbackFixtures.Rows.Length, rows.Length);

        FeedbackLine line = FeedbackCatalog.All[index];
        FeedbackFixtures.Row fixture = FeedbackFixtures.Rows[index];
        string[] cells = rows[index];

        Assert.Equal(fixture.Outcome, line.Outcome.ToString());
        Assert.Equal($"`{line.Outcome}`", cells[0]);
        Assert.Equal(line.Action.ToString(), cells[1]);
        if (line.Owner == FeedbackLineOwner.Mod)
        {
            Assert.Equal($"`{fixture.Exact}`", cells[3]);
            Assert.Equal(line.Placement == OrganizeNoticePlacement.TopLeft ? "top left" : "centre", cells[4]);
        }
        else if (line.Owner == FeedbackLineOwner.Vanilla)
        {
            Assert.Equal(VanillaExpected, cells[3]);
            Assert.Equal("game", cells[4]);
        }
        else
        {
            Assert.Equal($"no message ({line.Owner})", cells[3]);
            Assert.Equal("none", cells[4]);
        }
    }

    // GW-U-2134 (PT6): the trigger is the 6.1 text verbatim and starts with one of the three allowed prefixes.
    [Theory]
    [MemberData(nameof(RowIndices))]
    public void RowTriggerIsTheContractTextAndStartsWithAnAllowedPrefix(int index)
    {
        string trigger = TableRows(RepoRoot.ReadText(PlaytestFile).Split('\n'))[index][2];

        Assert.Equal(FeedbackFixtures.Rows[index].Trigger, trigger);
        Assert.Matches(@"^(Solo:|Host plus one friend:|Unit test only: )", trigger);
    }

    // GW-U-2135 (6.2): the four contract example rows appear byte-for-byte.
    [Theory]
    [InlineData("| `CraftCreated` | Craft | Solo: at a Workbench in a base, keep the Wood for a recipe only in a base chest and craft it. Then craft five more quickly; the line must not pile up. | `Used materials from base chests` | top left |")]
    [InlineData("| `BuildPlacementRefused` | Build | Solo: in a base, with the Wood only in a base chest, try to place a piece where the ghost is red (overlapping another piece). | the game's own message; no BaseStorage message | game |")]
    [InlineData("| `CategoryInUse` | CategoryChange | Host plus one friend: the host (Erik) keeps a chest open; the friend hovers it and presses `CycleCategoryKey`. | `Chest category not changed: in use by Erik` | centre |")]
    [InlineData("| `OrganizeNoPlayer` | Organize | Unit test only: organize-runner row 2 (no player). | no message (NoHud) | none |")]
    public void TheFourContractExampleRowsAppearByteForByte(string row)
    {
        Assert.Contains(row, RepoRoot.ReadText(PlaytestFile).Split('\n'));
    }

    // GW-U-2135 (CHANGELOG): the Unreleased section ends with a Changed subsection, and it holds the contract line.
    [Fact]
    public void UnreleasedEndsWithAChangedSubsectionHoldingTheContractLine()
    {
        string[] lines = RepoRoot.ReadText(ChangelogFile).Split('\n');
        // Edited by S-E009-001 (quality-audit QA4.7): the bullets now sit under the 0.1.0 release.
        int unreleased = Array.FindIndex(lines, line => line.StartsWith("## [0.1.0]", StringComparison.Ordinal));
        Assert.True(unreleased >= 0, "CHANGELOG.md has no 0.1.0 section.");
        int next = Array.FindIndex(lines, unreleased + 1, line => line.StartsWith("## ", StringComparison.Ordinal));
        string[] section = lines[(unreleased + 1)..(next < 0 ? lines.Length : next)];

        int lastSubsection = Array.FindLastIndex(section, line => line.StartsWith("### ", StringComparison.Ordinal));
        Assert.True(lastSubsection >= 0, "The Unreleased section has no subsection.");
        Assert.Equal("### Changed", section[lastSubsection].TrimEnd());
        string changed = Regex.Replace(string.Join(" ", section.Skip(lastSubsection + 1)), @"\s+", " ");
        Assert.Contains(ChangelogLine, changed, StringComparison.Ordinal);
    }

    private static string[] Level2Headings(string text) =>
        Regex.Matches(text, @"(?m)^## .*$").Select(match => match.Value.TrimEnd()).ToArray();

    /// <summary>The lines of the Feedback messages section, from after its heading up to the next level-2 heading.</summary>
    private static string[] SectionLines(string[] lines)
    {
        int start = Array.FindIndex(lines, line => line.TrimEnd() == Heading);
        Assert.True(start >= 0, "docs/PLAYTEST.md has no Feedback messages heading.");
        int end = start + 1;
        while (end < lines.Length && !lines[end].StartsWith("## ", StringComparison.Ordinal))
        {
            end++;
        }

        return lines.Skip(start + 1).Take(end - start - 1).ToArray();
    }

    /// <summary>The data rows of the table in the section, each as its trimmed cells without the outer pipes.</summary>
    private static string[][] TableRows(string[] lines)
    {
        string[] section = SectionLines(lines);
        int header = Array.FindIndex(section, line => line.Trim() == TableHeader);
        Assert.True(header >= 0, "The Feedback messages section has no exact table header.");

        var rows = new List<string[]>();
        for (int k = header + 2; k < section.Length && section[k].Trim().StartsWith("|", StringComparison.Ordinal); k++)
        {
            rows.Add(section[k].Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToArray());
        }

        return rows.ToArray();
    }
}
