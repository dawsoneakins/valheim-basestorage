namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// S-A-001 (AC-11; mixed-chest-category-contracts.md 7.3): CHANGELOG.md lists the setting under [Unreleased] and keeps 0.1.0 as it is,
/// package/README.md has the setting row after the threshold row, and docs/PLAYTEST.md section 16 covers the setting. The PLAYTEST load
/// check and config sync pins (GW-U-2458, PlaytestLoadCheckTests and PlaytestConfigSyncTests) are deferred to the implement phase.
/// </summary>
[Trait("Category", "Repo")]
public sealed class MixedChestDocsTests
{
    private const string ChangelogFile = "CHANGELOG.md";
    private const string PackageReadmeFile = "package/README.md";
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string UnreleasedHeading = "## [Unreleased]";
    private const string ReleaseHeading = "## [0.1.0]";
    private const string ChangedHeading = "### Changed";
    private const string ThresholdRowPrefix = "| `AutoAssignThresholdPct` |";
    private const string ReadmeRow =
        "| `AutoAssignMixedChests` | `[Organize]` | `true` | yes | Let Organize give mixed chests below the threshold a category that is short of space. Set to false to keep them as unassigned sources, as in 0.1.0. |";
    private const string Section16Heading = "### 16. AC-4.4 - Auto-assignment";
    private const string Section17Heading = "### 17. AC-4.5 - Organize plan";
    private const string LevelTwo = "## ";

    private static readonly string[] ChangelogBulletLiterals =
    {
        "`AutoAssignMixedChests`",
        "`[Organize]`",
        "`true`",
        "`false`",
        "Put away inventory is unchanged",
    };

    // GW-U-2455 (AC-11, 7.3): the [Unreleased] section has one Changed bullet with the required literals; 0.1.0 still describes 0.1.0.
    [Fact]
    public void TheChangelogUnreleasedSectionListsTheSettingAndTheReleaseSectionIsUnchanged()
    {
        string[] lines = Lines(RepoRoot.ReadText(ChangelogFile));

        string[] body = Section(lines, UnreleasedHeading, LevelTwo).Where(line => line.Length > 0).ToArray();
        Assert.Equal(11, body.Length); // Edited by the base move, Put away selected, the character menu and the compendium: an Added heading and four bullets follow.
        Assert.Equal(ChangedHeading, body[0]);
        Assert.StartsWith("- ", body[1], StringComparison.Ordinal);
        foreach (string literal in ChangelogBulletLiterals)
        {
            Assert.True(body[1].Contains(literal, StringComparison.Ordinal), "The Unreleased bullet lacks " + literal + ".");
        }

        string released = string.Join("\n", Section(lines, ReleaseHeading, LevelTwo));
        Assert.Contains("18 settings", released, StringComparison.Ordinal);
        Assert.DoesNotContain("AutoAssignMixedChests", released, StringComparison.Ordinal);
    }

    // GW-U-2456 (AC-11, 7.3): the README row is exactly the contract row, directly after the AutoAssignThresholdPct row.
    [Fact]
    public void ThePackageReadmeHasTheSettingRowDirectlyAfterTheThresholdRow()
    {
        string[] lines = Lines(RepoRoot.ReadText(PackageReadmeFile));

        int threshold = Array.FindIndex(lines, line => line.StartsWith(ThresholdRowPrefix, StringComparison.Ordinal));
        Assert.True(threshold >= 0, "The AutoAssignThresholdPct row is missing from package/README.md.");
        string next = threshold + 1 < lines.Length ? lines[threshold + 1] : "(none)";
        Assert.Equal(ReadmeRow, next);
    }

    // GW-U-2458 (AC-9.3, 7.3, PLAYTEST section 16): the auto-assignment section names the setting, has a false step and a Host plus one friend
    // step, each step has an Expected line, and the closing idle step stays last. The load check and config sync pins are deferred to implement.
    [Fact]
    public void PlaytestSection16CoversTheSettingWithItsStepsAndExpectedLines()
    {
        string[] section = Section(Lines(RepoRoot.ReadText(PlaytestFile)), Section16Heading, Section17Heading);
        Assert.NotEmpty(section);
        string text = string.Join("\n", section);

        Assert.Contains("AutoAssignMixedChests", text, StringComparison.Ordinal);
        Assert.Contains("[Organize]", text, StringComparison.Ordinal);
        Assert.Contains("`true`", text, StringComparison.Ordinal);
        Assert.Contains("host-synced", text, StringComparison.OrdinalIgnoreCase);

        string[] solo = Block(section, "#### Solo", "#### Host plus one friend");
        string[] host = Block(section, "#### Host plus one friend", null);

        Assert.True(
            section.Any(line => Regex.IsMatch(line, @"^\d+\. ") && line.Contains("AutoAssignMixedChests", StringComparison.Ordinal) && line.Contains("false", StringComparison.Ordinal)),
            "Section 16 has no numbered step that sets AutoAssignMixedChests to false.");
        Assert.True(
            host.Any(line => Regex.IsMatch(line, @"^\d+\. ") && line.Contains("AutoAssignMixedChests", StringComparison.Ordinal) && line.Contains("false", StringComparison.Ordinal)),
            "The Host plus one friend block has no step that sets AutoAssignMixedChests to false on the host.");

        foreach (string[] block in new[] { solo, host })
        {
            string[] steps = block.Where(line => Regex.IsMatch(line, @"^\d+\. ")).ToArray();
            Assert.True(steps.Length > 0, "A PLAYTEST block has no numbered step.");
            Assert.Equal(steps.Length, CountExpectedAfterSteps(block));
        }

        string lastSoloStep = solo.Where(line => Regex.IsMatch(line, @"^\d+\. ")).Last();
        Assert.Contains("stand still", lastSoloStep, StringComparison.Ordinal);
    }

    private static string[] Lines(string text) => RepoRoot.Normalise(text).Split('\n');

    /// <summary>The lines of a heading's section: after the heading, up to the first line that starts with <paramref name="stopAt"/>.</summary>
    private static string[] Section(string[] lines, string heading, string stopAt)
    {
        int start = Array.FindIndex(lines, line => line.StartsWith(heading, StringComparison.Ordinal));
        if (start < 0)
        {
            return Array.Empty<string>();
        }

        int end = start + 1;
        while (end < lines.Length && !lines[end].StartsWith(stopAt, StringComparison.Ordinal))
        {
            end++;
        }

        return lines[(start + 1)..end];
    }

    /// <summary>The lines from a sub-heading up to the next sub-heading (or to the end of the section when stop is null).</summary>
    private static string[] Block(string[] section, string heading, string? stop)
    {
        int start = Array.FindIndex(section, line => line.StartsWith(heading, StringComparison.Ordinal));
        if (start < 0)
        {
            return Array.Empty<string>();
        }

        int end = start + 1;
        while (end < section.Length && !(stop is not null ? section[end].StartsWith(stop, StringComparison.Ordinal) : section[end].StartsWith("#### ", StringComparison.Ordinal)))
        {
            end++;
        }

        return section[(start + 1)..end];
    }

    /// <summary>How many numbered steps are followed, before the next numbered step, by an Expected line.</summary>
    private static int CountExpectedAfterSteps(string[] block)
    {
        int count = 0;
        int open = 0;
        bool sawExpected = false;
        foreach (string line in block)
        {
            if (Regex.IsMatch(line, @"^\d+\. "))
            {
                if (open > 0 && sawExpected)
                {
                    count++;
                }

                open++;
                sawExpected = false;
            }
            else if (line.StartsWith("Expected:", StringComparison.Ordinal))
            {
                sawExpected = true;
            }
        }

        if (open > 0 && sawExpected)
        {
            count++;
        }

        return count;
    }
}
