namespace BaseStorage.Core.Tests.Repo;

using System.Linq;

/// <summary>AC-17: the three shipped docs exist, open with the contract title, and carry the contract sections in order.</summary>
[Trait("Category", "Repo")]
public sealed class DocSkeletonTests
{
    // GW-U-023
    [Fact]
    public void ChangelogMatchesContractSkeleton()
    {
        AssertSkeleton("CHANGELOG.md", "# Changelog", "## [Unreleased]");
    }

    // GW-U-023
    [Fact]
    public void PackageReadmeMatchesContractSkeleton()
    {
        AssertSkeleton(
            "package/README.md",
            "# BaseStorage",
            "## What it does",
            "## Installation",
            "## Configuration",
            "## Categories file",
            "## Known limits",
            "## Uninstall");
    }

    // GW-U-023
    [Fact]
    public void PlaytestChecklistMatchesContractSkeleton()
    {
        AssertSkeleton(
            "docs/PLAYTEST.md",
            "# BaseStorage playtest checklist",
            "## Setup",
            "## Solo",
            "## Host plus one friend");
    }

    private static void AssertSkeleton(string relative, string title, params string[] sections)
    {
        string text = RepoRoot.ReadText(relative);
        Assert.False(string.IsNullOrWhiteSpace(text), $"{relative} is empty.");

        string firstLine = text.Split('\n').Select(l => l.TrimEnd()).First(l => l.Trim().Length > 0);
        Assert.Equal(title, firstLine);

        string[] headings = text.Split('\n').Select(l => l.TrimEnd()).Where(l => l.StartsWith("## ", System.StringComparison.Ordinal)).ToArray();
        int next = 0;
        foreach (string section in sections)
        {
            int index = System.Array.FindIndex(headings, next, h => h == section);
            Assert.True(index >= 0, $"Heading '{section}' is missing or out of order in {relative}.");
            next = index + 1;
        }
    }
}
