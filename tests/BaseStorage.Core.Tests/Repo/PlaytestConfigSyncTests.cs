namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// GW-U-1422 (AC-7, contract 8 C1-C6; ADR-GWIN-121): docs/PLAYTEST.md ends with the "Config sync" section, which has the preamble, the two
/// numbered subsections with their Expected lines, and the closing sentence. The game is never launched by the pipeline.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestConfigSyncTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string HeadingLine = "## Config sync (AC-6.2, AC-6.3)";
    private const string VersionHandshakeHeading = "## Version handshake (AC-6.1, AC-6.3)";
    private const string FriendTitle = "Host plus one friend";
    private const string SoloTitle = "Solo and hosting";

    private static readonly string[] SettingKeys =
    {
        "CraftFromBase",
        "ExcludeVehicles",
        "KeepChestsCraftable",
        "MaxBaseStations",
        "RequireSameVersion",
        "AutoAssignThresholdPct",
        "OrganizeLockSeconds",
        "ReserveTimeoutMs",
    };

    // GW-U-1422 (C1): exactly one "## Config sync (AC-6.2, AC-6.3)" heading, the last level-2 heading, after the Version handshake section.
    [Fact]
    public void TheConfigSyncHeadingIsExactAndLast()
    {
        string[] headings = Regex.Matches(RepoRoot.ReadText(PlaytestFile), @"(?m)^## .*$")
            .Select(match => match.Value.TrimEnd())
            .ToArray();

        Assert.Single(headings, heading => heading == HeadingLine);
        Assert.Equal(HeadingLine, headings.Last());

        // Edited by integration (S-E003-004..007 + S-E006-004): the S-E003 sections sit between Version handshake and Config sync.
        Assert.True(Array.IndexOf(headings, VersionHandshakeHeading) >= 0, "The Version handshake section is missing.");
        Assert.True(Array.IndexOf(headings, VersionHandshakeHeading) < Array.IndexOf(headings, HeadingLine), "Config sync must come after Version handshake.");
    }

    // GW-U-1422 (C2): the preamble names the RPC, the eight keys, the two files with <ValheimDir> and backslashes, the Info line, the Debug lines, DebugLogging and the F1 display.
    [Fact]
    public void ThePreambleNamesTheRpcKeysPathsLinesAndF1()
    {
        string preamble = Preamble(Section(RepoRoot.ReadText(PlaytestFile)));

        Assert.Contains("BaseStorage_ConfigSync", preamble);
        foreach (string key in SettingKeys)
        {
            Assert.Contains(key, preamble);
        }

        Assert.Contains(@"<ValheimDir>\BepInEx\config\BaseStorage.categories.txt", preamble);
        Assert.Contains(@"<ValheimDir>\BepInEx\config\dawso.valheim.basestorage.cfg", preamble);
        Assert.Contains("never written", preamble);
        Assert.Contains("`Config sync: applied host settings from peer <id>, 9 settings, <n> categories`", preamble);
        Assert.Contains("`Config sync: <Key> = <value> (host)`", preamble);
        Assert.Contains("DebugLogging = true", preamble);
        Assert.Contains("ConfigurationManager", preamble);
        Assert.Contains("F1", preamble);
    }

    // GW-U-1422 (C3, C4, C5): the two subsections come in order, each numbered from 1 to 5, with an Expected line before each next step.
    [Fact]
    public void BothSubsectionsAreNumberedFromOneWithAnExpectedLinePerStep()
    {
        string section = Section(RepoRoot.ReadText(PlaytestFile));
        int friend = section.IndexOf("### " + FriendTitle, StringComparison.Ordinal);
        int solo = section.IndexOf("### " + SoloTitle, StringComparison.Ordinal);
        Assert.True(friend >= 0 && solo > friend, "The subsections must be Host plus one friend, then Solo and hosting.");

        string[] friendSteps = StepChunks(Subsection(section, FriendTitle));
        string[] soloSteps = StepChunks(Subsection(section, SoloTitle));
        Assert.Equal(new[] { "1", "2", "3", "4", "5" }, friendSteps.Select(StepNumber));
        Assert.Equal(new[] { "1", "2", "3", "4", "5" }, soloSteps.Select(StepNumber));
        Assert.All(friendSteps, step => Assert.Contains("Expected:", step));
        Assert.All(soloSteps, step => Assert.Contains("Expected:", step));
    }

    // GW-U-1422 (C4(1), C4(4), C5(4)): the hash is taken at setup, after the session and after hosting, with Get-FileHash in the named steps.
    [Fact]
    public void GetFileHashIsInTheNamedSteps()
    {
        string section = Section(RepoRoot.ReadText(PlaytestFile));
        string[] friendSteps = StepChunks(Subsection(section, FriendTitle));
        string[] soloSteps = StepChunks(Subsection(section, SoloTitle));

        Assert.Contains("Get-FileHash", friendSteps[0]);
        Assert.Contains("Get-FileHash", friendSteps[3]);
        Assert.Contains("Get-FileHash", soloSteps[3]);
    }

    // GW-U-1422 (C6): no emoji, and no forward-slash BepInEx path in the section.
    [Fact]
    public void TheSectionHasNoEmojiAndNoForwardSlashBepInExPath()
    {
        string section = Section(RepoRoot.ReadText(PlaytestFile));

        Assert.False(Regex.IsMatch(section, @"[\uD800-\uDFFF☀-➿]"), "The section holds an emoji or a symbol.");
        Assert.DoesNotContain("BepInEx/", section);
    }

    // GW-U-1422 (C6): the closing sentence names the malformed, oversize and out-of-order payloads as covered by unit tests.
    [Fact]
    public void TheSectionClosesByNamingTheUnitTestCoverage()
    {
        string section = Section(RepoRoot.ReadText(PlaytestFile));

        Assert.Matches(@"(?is)malformed.*oversize.*out-of-order.*unit tests", section);
    }

    /// <summary>The text after the heading, to the end of the file (the section is the last level-2 section).</summary>
    private static string Section(string text)
    {
        Match heading = Regex.Match(text, @"(?m)^## Config sync \(AC-6\.2, AC-6\.3\)[ \t]*$");
        Assert.True(heading.Success, "docs/PLAYTEST.md has no Config sync section.");
        return text.Substring(heading.Index + heading.Length);
    }

    /// <summary>The text before the first level-3 heading of the section.</summary>
    private static string Preamble(string section)
    {
        int next = section.IndexOf("\n### ", StringComparison.Ordinal);
        return next < 0 ? section : section.Substring(0, next);
    }

    /// <summary>The text of one level-3 subsection, to the next level-3 heading or the end.</summary>
    private static string Subsection(string section, string title)
    {
        int start = section.IndexOf("### " + title, StringComparison.Ordinal);
        Assert.True(start >= 0, "No subsection " + title + ".");
        int end = section.IndexOf("\n### ", start + 1, StringComparison.Ordinal);
        return end < 0 ? section.Substring(start) : section.Substring(start, end - start);
    }

    /// <summary>Splits a subsection into its numbered steps; each step runs to the next numbered step.</summary>
    private static string[] StepChunks(string subsection) =>
        Regex.Split(subsection, @"(?m)^(?=\d+\. )").Where(chunk => Regex.IsMatch(chunk, @"^\d+\. ")).ToArray();

    /// <summary>The number at the start of a step chunk.</summary>
    private static string StepNumber(string chunk) => Regex.Match(chunk, @"^(\d+)\. ").Groups[1].Value;
}
