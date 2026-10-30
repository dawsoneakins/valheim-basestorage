namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;

/// <summary>
/// GW-U-1789 (AC-5, spec AC-7.3 and AC-9.3, contract 8): docs/PLAYTEST.md holds the Station buttons section (PS1-PS7) between Organize and
/// Chest reservation, package/README.md holds the Station buttons section between Chest categories and Known limits, and CHANGELOG.md
/// holds the Added line. Red until ADR-GWIN-137 docs are written.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestStationButtonsTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string ReadmeFile = "package/README.md";
    private const string ChangelogFile = "CHANGELOG.md";
    private const string HeadingPattern = @"^## Station buttons \(AC-2\.4, AC-4\.7, AC-4\.10\)[ \t]*$";
    private const string ChangelogLine = "- Organize base button and base status line on every crafting station panel in a base.";

    // GW-U-1789 (PS1): exactly one heading, directly after Organize and directly before Chest reservation.
    // Edited by S-E004-015 (ADR-GWIN-143, contract 6): the Put away inventory section sits directly between Station buttons and Chest reservation.
    [Fact]
    public void TheSectionHeadingSitsBetweenOrganizeAndChestReservation()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        MatchCollection headings = Regex.Matches(text, HeadingPattern, RegexOptions.Multiline);
        Match organize = Regex.Match(text, @"^## Organize \(AC-2\.4, AC-4\.7, AC-4\.8, AC-5\.4\)[ \t]*$", RegexOptions.Multiline);
        Match putAway = Regex.Match(text, @"^## Put away inventory \(AC-2\.4, AC-4\.8, AC-4\.9, AC-4\.10, AC-5\.4\)[ \t]*$", RegexOptions.Multiline);
        Match reservation = Regex.Match(text, @"^## Chest reservation \(AC-5\.2, AC-5\.3, AC-6\.3\)[ \t]*$", RegexOptions.Multiline);

        Assert.Single(headings);
        Assert.True(organize.Success && putAway.Success && reservation.Success, "The neighbouring sections are missing.");
        Assert.True(headings[0].Index > organize.Index && headings[0].Index < putAway.Index, "The Station buttons section must sit between Organize and Put away inventory.");
        Assert.True(putAway.Index < reservation.Index, "The Put away inventory section must sit directly before Chest reservation.");
        Assert.DoesNotMatch(@"(?m)^## ", text.Substring(organize.Index + organize.Length, headings[0].Index - organize.Index - organize.Length));
        Assert.DoesNotMatch(@"(?m)^## ", text.Substring(headings[0].Index + headings[0].Length, putAway.Index - headings[0].Index - headings[0].Length));
        Assert.DoesNotMatch(@"(?m)^## ", text.Substring(putAway.Index + putAway.Length, reservation.Index - putAway.Index - putAway.Length));
    }

    // GW-U-1789 (PS2, PS7): the preamble names the behaviour, the settings in backticks, the Put away exclusion and the two Debug lines verbatim.
    [Fact]
    public void ThePreambleStatesTheBehaviourAndQuotesTheDebugLines()
    {
        string preamble = Section().Split(new[] { "### Solo" }, System.StringSplitOptions.None)[0];

        foreach (string literal in new[]
                 {
                     "Organize base",
                     "Base: N stations, M chests",
                     "`StationButtons`",
                     "`OrganizeSkipPreview`",
                     "`[General]`",
                     "`[Organize]`",
                     "Put away inventory",
                     "## Debug logging",
                     "`DebugLogging = true`",
                     "`<ValheimDir>\\BepInEx\\LogOutput.log`",
                     "`[station.actions.organize-button] Organize <Outcome>`",
                     "`[station.actions.row] hidden: no room next to the craft controls`",
                 })
        {
            Assert.Contains(literal, preamble, System.StringComparison.Ordinal);
        }
    }

    // GW-U-1789 (PS3, PS4, PS5, PS6): Solo then Host plus one friend, numbered from 1, each step followed by an Expected line, and each ends with the idle step.
    [Fact]
    public void TheSubsectionsAreNumberedFromOneWithAnExpectedLineAndAClosingIdleStep()
    {
        string section = Section();
        int solo = section.IndexOf("### Solo", System.StringComparison.Ordinal);
        int host = section.IndexOf("### Host plus one friend", System.StringComparison.Ordinal);
        Assert.True(solo > 0 && host > solo, "Solo must come before Host plus one friend.");

        string soloText = section.Substring(solo, host - solo);
        string hostText = section.Substring(host);
        AssertNumberedSteps(soloText, 12);
        AssertNumberedSteps(hostText, 5);

        foreach (string subsection in new[] { soloText, hostText })
        {
            Assert.Contains("failed and was skipped", subsection, System.StringComparison.Ordinal);
            Assert.Contains("30 seconds", subsection, System.StringComparison.Ordinal);
        }

        foreach (string literal in new[] { "Tab", "Base: 1 station, 0 chests", "No chests in this base", "Base: 1 station, 3 chests", "Organize cancelled", "Base: 2 stations, 3 chests", "[station.actions.organize-button] Organize Started", "`OrganizeSkipPreview = true`", "`StationButtons = false`", "[station.actions.row] hidden: no room next to the craft controls" })
        {
            Assert.Contains(literal, soloText, System.StringComparison.Ordinal);
        }

        foreach (string literal in new[] { "Organize in progress by <HostName>", "Organize in progress by <FriendName>", "`StationButtons = false`", "MovesPerFrame = 1" })
        {
            Assert.Contains(literal, hostText, System.StringComparison.Ordinal);
        }
    }

    // GW-U-1789 (PS7, contract 8.2): the closing note names the proofs, the section holds the contracted literals, no emoji, and the README and CHANGELOG carry their lines.
    [Fact]
    public void TheClosingNoteAndTheReadmeAndChangelogCarryTheContractedText()
    {
        string section = Section();
        Assert.Contains("StationPanelLayout", section, System.StringComparison.Ordinal);
        Assert.Contains("unit tests", section, System.StringComparison.Ordinal);
        Assert.DoesNotMatch(@"[\uD800-\uDFFF]", section);

        string readme = RepoRoot.ReadText(ReadmeFile);
        Match chest = Regex.Match(readme, @"^## Chest categories[ \t]*$", RegexOptions.Multiline);
        Match station = Regex.Match(readme, @"^## Station buttons[ \t]*$", RegexOptions.Multiline);
        Match known = Regex.Match(readme, @"^## Known limits[ \t]*$", RegexOptions.Multiline);
        Assert.True(chest.Success && station.Success && known.Success, "The README sections are missing.");
        Assert.True(chest.Index < station.Index && station.Index < known.Index, "The README Station buttons section must sit between Chest categories and Known limits.");
        foreach (string literal in new[] { "`StationButtons`", "`OrganizeSkipPreview`", "Organize base", "Put away inventory" })
        {
            Assert.Contains(literal, readme, System.StringComparison.Ordinal);
        }

        string changelog = RepoRoot.ReadText(ChangelogFile);
        // Edited by S-E009-001 (quality-audit QA4.7): the bullets now sit under the 0.1.0 release.
        int unreleased = changelog.IndexOf("## [0.1.0]", System.StringComparison.Ordinal);
        int added = changelog.IndexOf("### Added", unreleased, System.StringComparison.Ordinal);
        int line = changelog.IndexOf(ChangelogLine, System.StringComparison.Ordinal);
        Assert.True(unreleased >= 0 && added > unreleased && line > added, "The CHANGELOG Added line must sit under 0.1.0.");
    }

    private static string Section()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        Match heading = Regex.Match(text, HeadingPattern, RegexOptions.Multiline);
        Assert.True(heading.Success, "The Station buttons heading is missing.");

        int start = heading.Index;
        Match next = Regex.Match(text.Substring(start + heading.Length), @"^## ", RegexOptions.Multiline);
        int end = next.Success ? start + heading.Length + next.Index : text.Length;
        return text.Substring(start, end - start);
    }

    private static void AssertNumberedSteps(string subsection, int expectedSteps)
    {
        string[] lines = subsection.Split('\n');
        int steps = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            Match step = Regex.Match(lines[i], @"^(\d+)\. ");
            if (!step.Success)
            {
                continue;
            }

            steps++;
            Assert.Equal(steps, int.Parse(step.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));

            int next = i + 1;
            while (next < lines.Length && lines[next].Trim().Length == 0)
            {
                next++;
            }

            Assert.True(next < lines.Length && lines[next].Trim().StartsWith("Expected:", System.StringComparison.Ordinal), "Step " + steps + " has no Expected line.");
        }

        Assert.Equal(expectedSteps, steps);
    }
}
