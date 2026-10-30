namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Text.RegularExpressions;

/// <summary>AC-7 (spec S-E002-005, ADR-GWIN-032 section 5): the Unreleased section of CHANGELOG.md has an Added bullet about the hover lines.</summary>
[Trait("Category", "Repo")]
public sealed class ChangelogHoverTests
{
    private const string ChangelogFile = "CHANGELOG.md";

    // GW-U-560 (AC-7): the Unreleased section has an Added heading whose first bullet names the hover lines.
    [Fact]
    public void TheUnreleasedSectionHasAnAddedBulletAboutTheHoverLines()
    {
        // Edited by S-E009-001 (quality-audit QA4.7): the bullets now sit under the 0.1.0 release.
        string text = RepoRoot.ReadText(ChangelogFile);
        Assert.Contains("# Changelog", text, StringComparison.Ordinal);
        Assert.Contains("## [0.1.0]", text, StringComparison.Ordinal);

        int unreleased = text.IndexOf("## [0.1.0]", StringComparison.Ordinal);
        int nextHeading = text.IndexOf("\n## ", unreleased + 1, StringComparison.Ordinal);
        string section = nextHeading < 0 ? text.Substring(unreleased) : text.Substring(unreleased, nextHeading - unreleased);

        Match added = Regex.Match(section, @"(?m)^### Added[ \t]*$");
        Assert.True(added.Success, "The Unreleased section has no '### Added' heading.");

        string afterAdded = section.Substring(added.Index + added.Length);
        int nextSubheading = afterAdded.IndexOf("\n### ", StringComparison.Ordinal);
        string addedBody = nextSubheading < 0 ? afterAdded : afterAdded.Substring(0, nextSubheading);
        Match bullet = Regex.Match(addedBody, @"(?m)^- .*$");
        Assert.True(bullet.Success, "The Added section has no '- ' bullet.");

        string line = bullet.Value;
        Assert.Contains("hover", line, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("In base", line, StringComparison.Ordinal);
        Assert.Contains("Not in a base", line, StringComparison.Ordinal);
        Assert.Contains("Keep", line, StringComparison.Ordinal);
        Assert.Contains("station", line, StringComparison.Ordinal);
    }
}
