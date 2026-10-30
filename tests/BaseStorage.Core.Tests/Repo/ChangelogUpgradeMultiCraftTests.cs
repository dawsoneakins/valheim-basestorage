namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Linq;

/// <summary>GW-U-1730 (AC-9.3; contract upgrade-multicraft 9): the Unreleased Added list of CHANGELOG.md carries the upgrade and multi-craft bullet.</summary>
[Trait("Category", "Repo")]
public sealed class ChangelogUpgradeMultiCraftTests
{
    private const string ChangelogFile = "CHANGELOG.md";

    // GW-U-1730 (AC-9.3, contract 9): the last bullet of the 0.1.0 Added list, before the next subsection, names upgrading, several at once, CraftFromBase and the notice.
    [Fact]
    public void TheLastAddedBulletBeforeTheNextReleaseNamesUpgradeAndMultiCraftFromBase()
    {
        // Edited by S-E009-001 (quality-audit QA4.7): the bullets now sit under the 0.1.0 release.
        string text = RepoRoot.Normalise(RepoRoot.ReadText(ChangelogFile));
        int unreleased = text.IndexOf("## [0.1.0]", StringComparison.Ordinal);
        Assert.True(unreleased >= 0, "CHANGELOG.md has no 0.1.0 section.");

        int added = text.IndexOf("### Added", unreleased, StringComparison.Ordinal);
        Assert.True(added >= 0, "The 0.1.0 section has no Added list.");

        int nextRelease = text.IndexOf("\n### ", added, StringComparison.Ordinal);
        Assert.True(nextRelease > added, "The Added list is not followed by another subsection.");

        string addedList = text.Substring(added, nextRelease - added);
        // Edited by S-E010-002 (organize-console-command 7.4): the console bullet is now the last Added bullet, directly after this one, so the pin finds the bullet by its lead text.
        string lastBullet = addedList.Split('\n').Where(line => line.StartsWith("- Crafting from base chests:", StringComparison.Ordinal)).FirstOrDefault() ?? string.Empty;
        Assert.NotEqual(string.Empty, lastBullet);

        Assert.Contains("upgrading an item", lastBullet, StringComparison.Ordinal);
        Assert.Contains("several at once", lastBullet, StringComparison.Ordinal);
        Assert.Contains("`CraftFromBase`", lastBullet, StringComparison.Ordinal);
        Assert.Contains("Materials moved, try again", lastBullet, StringComparison.Ordinal);
    }
}
