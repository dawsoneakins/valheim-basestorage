namespace BaseStorage.Core.Tests.Repo;

using System;

/// <summary>
/// Visibility of Debug logging for players (log-policy-contract.md v1, section 5 visibility note; ADR-GWIN-022 Documentation).
/// The playtest checklist and the package README say how to turn DebugLogging on without a restart.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestDebugLoggingTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string ReadmeFile = "package/README.md";

    // GW-U-221
    [Fact]
    public void PlaytestHasADebugLoggingSectionAfterHostPlusOneFriend()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        int host = text.IndexOf("\n## Host plus one friend\n", StringComparison.Ordinal);
        Assert.True(host >= 0, "PLAYTEST.md has no '## Host plus one friend' section.");

        int debugHeading = text.IndexOf("\n## Debug logging\n", StringComparison.Ordinal);
        Assert.True(debugHeading > host, "PLAYTEST.md has no '## Debug logging' section after '## Host plus one friend'.");

        string section = SectionAt(text, debugHeading);
        Assert.Contains("DebugLogging", section, StringComparison.Ordinal);
        Assert.Contains("[Debug]", section, StringComparison.Ordinal);
        Assert.Contains("LogLevels", section, StringComparison.Ordinal);
        Assert.Contains("BepInEx.cfg", section, StringComparison.Ordinal);
        Assert.Contains("ConfigurationManager", section, StringComparison.Ordinal);
        Assert.Contains("without restarting", section, StringComparison.Ordinal);
    }

    // GW-U-221
    [Fact]
    public void PackageReadmeMentionsDebugLoggingAndLogLevelsInsideTheConfigurationSection()
    {
        string text = RepoRoot.ReadText(ReadmeFile);
        int configuration = text.IndexOf("\n## Configuration\n", StringComparison.Ordinal);
        Assert.True(configuration >= 0, "package/README.md has no '## Configuration' section.");

        string section = SectionAt(text, configuration);
        Assert.Contains("DebugLogging", section, StringComparison.Ordinal);
        Assert.Contains("LogLevels", section, StringComparison.Ordinal);
    }

    /// <summary>The text from a heading (at index of its preceding newline) up to the next level-2 heading or the end.</summary>
    private static string SectionAt(string text, int newlineBeforeHeading)
    {
        int start = newlineBeforeHeading + 1;
        int next = text.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
        return next < 0 ? text.Substring(start) : text.Substring(start, next - start);
    }
}
