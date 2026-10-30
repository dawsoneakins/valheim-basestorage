namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;

/// <summary>
/// AC-7 and AC-8 (spec non-goal: the game is not launched by the pipeline): docs/PLAYTEST.md has a "Category file" section that
/// lists the fresh-install, write-failure and keep-existing scenarios of contract 6.1.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestCategoryFileTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";

    // GW-U-472 (AC-7)
    [Fact]
    public void CategoryFileSectionHasTheFreshInstallAndWriteFailureScenarios()
    {
        string section = CategoryFileSection();

        Assert.Contains("# BaseStorage categories file (BaseStorage.categories.txt)", section);
        Assert.Contains("Wrote default category file '", section);
        Assert.Contains("item(s)", section);
        Assert.Contains("Could not write default category file '", section);
    }

    // GW-U-472 (AC-8)
    [Fact]
    public void CategoryFileSectionHasTheKeepExistingScenarios()
    {
        string section = CategoryFileSection();

        Assert.Matches("(?i)edited", section);
        Assert.Matches("(?i)0[- ]byte", section);
        Assert.Matches("(?i)deleted", section);
        Assert.Contains("already exists; left unchanged", section);
        Assert.Contains("DebugLogging", section);
    }

    private static string CategoryFileSection()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        Match heading = Regex.Match(text, @"^## Category file[ \t]*$", RegexOptions.Multiline);
        Assert.True(heading.Success, "docs/PLAYTEST.md has no '## Category file' section.");

        int start = heading.Index + heading.Length;
        Match next = Regex.Match(text.Substring(start), @"^## ", RegexOptions.Multiline);
        return next.Success ? text.Substring(start, next.Index) : text.Substring(start);
    }
}
