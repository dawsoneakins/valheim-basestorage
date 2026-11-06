namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// GW-U-2326 and GW-U-2327 (AC-5; organize-console-command contract 7.2, 7.3 and 7.4): package/README.md holds the Console command section
/// between Station buttons and Known limits, docs/ADDED-BY-PIPEMASON.md starts with the shared preamble and holds the console section once,
/// and the CHANGELOG bullet is the last bullet of the first Unreleased Added list with Changed still last. Red until the docs are written.
/// Not a test class.
/// </summary>
[Trait("Category", "Repo")]
public sealed class ConsoleCommandDocsTests
{
    private const string ReadmeFile = "package/README.md";
    private const string AddedFile = "docs/ADDED-BY-PIPEMASON.md";
    private const string ChangelogFile = "CHANGELOG.md";
    private const string ReadmeHeading = @"^## Console command[ \t]*$";
    private const string AddedHeading = "## Organize console command (S-E010-002)";
    private const string ChangelogPrefix = "- Console command `dons-qol organize`: ";

    private static readonly string AddedPreamble = string.Join(
        "\n",
        "# Added by Pipemason",
        string.Empty,
        "These features were proposed by the Pipemason program after the must-have acceptance criteria were delivered (spec section 9).",
        "Each is marked `origin: pipemason-proposed` in .pipeline/ROADMAP.md, and package/README.md and docs/PLAYTEST.md describe each one.");

    // GW-U-2326 (7.2): the README section sits between Station buttons and Known limits and names the command, the flag, the key and the texts.
    [Fact]
    public void TheReadmeSectionSitsBetweenStationButtonsAndKnownLimits()
    {
        string text = RepoRoot.ReadText(ReadmeFile);
        MatchCollection headings = Regex.Matches(text, ReadmeHeading, RegexOptions.Multiline);
        Match stationButtons = Regex.Match(text, @"^## Station buttons[ \t]*$", RegexOptions.Multiline);
        Match knownLimits = Regex.Match(text, @"^## Known limits[ \t]*$", RegexOptions.Multiline);

        Assert.Single(headings);
        Assert.True(stationButtons.Success && knownLimits.Success, "Station buttons or Known limits is missing.");
        Assert.True(stationButtons.Index < headings[0].Index && headings[0].Index < knownLimits.Index, "The Console command section must sit between Station buttons and Known limits.");

        string section = text.Substring(headings[0].Index, knownLimits.Index - headings[0].Index);
        Assert.Contains("dons-qol organize", section, StringComparison.Ordinal);
        Assert.Contains("-console", section, StringComparison.Ordinal);
        Assert.Contains("F5", section, StringComparison.Ordinal);
        Assert.Contains("Not in a base", section, StringComparison.Ordinal);
        Assert.Contains("Organize in progress by <name>", section, StringComparison.Ordinal);
        Assert.Contains("Usage: dons-qol organize", section, StringComparison.Ordinal);
        Assert.Contains("in-game console", section, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(text, Regex.Escape("Console and crossplay players are not supported.")));
    }

    // GW-U-2327 (7.3): the file starts with the shared preamble (a prefix check, merge-safe with S-E010-001), and the section appears once.
    [Fact]
    public void TheAddedByPipemasonFileStartsWithThePreambleAndHoldsTheSectionOnce()
    {
        string text = RepoRoot.ReadText(AddedFile);

        Assert.StartsWith(AddedPreamble, text, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(text, "^" + Regex.Escape(AddedHeading) + "[ \t]*$", RegexOptions.Multiline));

        string section = text.Substring(text.IndexOf(AddedHeading, StringComparison.Ordinal));
        Assert.Contains("dons-qol organize", section, StringComparison.Ordinal);
        Assert.Contains("Terminal.InitTerminal", section, StringComparison.Ordinal);
        Assert.Contains("Not in a base", section, StringComparison.Ordinal);
        Assert.Contains("Organize in progress by <name>", section, StringComparison.Ordinal);
        Assert.Contains("Usage: dons-qol organize", section, StringComparison.Ordinal);
    }

    // GW-U-2327 (7.4): the CHANGELOG bullet is the last bullet of the first Unreleased Added list, and Changed stays last.
    [Fact]
    public void TheChangelogBulletIsTheLastAddedBulletAndChangedStaysLast()
    {
        string[] lines = RepoRoot.ReadText(ChangelogFile).Split('\n');
        // Edited by S-E009-001 (quality-audit QA4.7): the bullets now sit under the 0.1.0 release.
        int unreleased = Array.FindIndex(lines, line => line.StartsWith("## [0.1.0]", StringComparison.Ordinal));
        Assert.True(unreleased >= 0, "No ## [0.1.0] heading.");
        int nextVersion = Array.FindIndex(lines, unreleased + 1, line => line.StartsWith("## [", StringComparison.Ordinal));
        int end = nextVersion < 0 ? lines.Length : nextVersion;

        int crafting = Array.FindIndex(lines, unreleased, end - unreleased, line => line.StartsWith("- Crafting from base chests:", StringComparison.Ordinal));
        int bullet = Array.FindIndex(lines, unreleased, end - unreleased, line => line.StartsWith(ChangelogPrefix, StringComparison.Ordinal));
        Assert.True(crafting >= 0 && bullet == crafting + 1, "The console bullet must follow the Crafting from base chests bullet directly.");
        Assert.True(bullet + 1 < lines.Length && lines[bullet + 1].StartsWith("### ", StringComparison.Ordinal), "The console bullet must be the last bullet of the Added list.");
        Assert.Contains("`Not in a base`", lines[bullet], StringComparison.Ordinal);
        Assert.Contains("`Organize in progress by <name>`", lines[bullet], StringComparison.Ordinal);

        string[] headings = lines.Skip(unreleased).Take(end - unreleased).Where(line => line.StartsWith("### ", StringComparison.Ordinal)).ToArray();
        Assert.Equal("### Changed", headings.Last());
    }
}
