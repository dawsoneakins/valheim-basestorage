namespace BaseStorage.Core.Tests.Repo;

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// GW-U-1940 (AC-5, spec AC-7.3 and AC-9.3; deposit-button contract 8 PD1-PD7 and 8.2): docs/PLAYTEST.md holds the Put away inventory
/// section (PD1-PD7) directly between Station buttons and Chest reservation. package/README.md replaces the "comes in a later version"
/// sentence in its Station buttons section, and CHANGELOG.md holds the Put away line directly after the Organize base line. Red until
/// the implement phase writes the docs.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestPutAwayTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string ReadmeFile = "package/README.md";
    private const string ChangelogFile = "CHANGELOG.md";
    private const string StationHeading = @"^## Station buttons \(AC-2\.4, AC-4\.7, AC-4\.10\)[ \t]*$";
    private const string PutAwayHeading = @"^## Put away inventory \(AC-2\.4, AC-4\.8, AC-4\.9, AC-4\.10, AC-5\.4\)[ \t]*$";
    private const string ReservationHeading = @"^## Chest reservation \(AC-5\.2, AC-5\.3, AC-6\.3\)[ \t]*$";
    private const string OrganizeLine = "- Organize base button and base status line on every crafting station panel in a base.";
    private const string PutAwayLinePrefix = "- Put away inventory button next to Organize base on every crafting station panel in a base";

    // GW-U-1940 (PD1): one level-2 heading, directly after Station buttons and directly before Chest reservation.
    [Fact]
    public void ThePutAwaySectionSitsBetweenStationButtonsAndChestReservation()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        Match station = Regex.Match(text, StationHeading, RegexOptions.Multiline);
        MatchCollection headings = Regex.Matches(text, PutAwayHeading, RegexOptions.Multiline);
        Match reservation = Regex.Match(text, ReservationHeading, RegexOptions.Multiline);

        Assert.True(station.Success && reservation.Success, "The neighbouring sections are missing.");
        Assert.Single(headings);
        int at = headings[0].Index;
        Assert.True(station.Index < at && at < reservation.Index, "The Put away inventory section must sit between Station buttons and Chest reservation.");
        Assert.DoesNotMatch(@"(?m)^## ", text.Substring(station.Index + station.Length, at - station.Index - station.Length));
        Assert.DoesNotMatch(@"(?m)^## ", text.Substring(at + headings[0].Length, reservation.Index - at - headings[0].Length));
    }

    // GW-U-1940 (PD2): the preamble states the behaviour, the settings, the exclusions, the summary line and the Debug line, with the contracted literals.
    [Fact]
    public void ThePreambleStatesThePutAwayBehaviourAndTheDebugLine()
    {
        string preamble = PutAwaySection().Split(new[] { "### Solo" }, System.StringSplitOptions.None)[0];

        foreach (string literal in new[]
                 {
                     "Put away inventory",
                     "Organize base",
                     "`DepositKey`",
                     "`[Keys]`",
                     "`DepositPreview`",
                     "`[Organize]`",
                     "`DepositNeverItems`",
                     "`@mead, @ammo, Hammer, Hoe, Cultivator, FishingRod, @bait`",
                     "(top row)",
                     "Alt+click",
                     "Put away: 4 stacks moved, 2 no room",
                     "top-left",
                     "## Debug logging",
                     "`DebugLogging = true`",
                     "`[station.actions.deposit-button] Deposit <Outcome>`",
                     "`<ValheimDir>\\BepInEx\\LogOutput.log`",
                 })
        {
            Assert.Contains(literal, preamble, System.StringComparison.Ordinal);
        }
    }

    // GW-U-1940 (PD3, PD6): Solo with 11 numbered steps, then Host plus one friend with 5, each step followed by an Expected line, and each ending with the idle step.
    [Fact]
    public void TheSubsectionsAreNumberedFromOneWithExpectedLinesAndAClosingIdleStep()
    {
        string section = PutAwaySection();
        int solo = section.IndexOf("### Solo", System.StringComparison.Ordinal);
        int host = section.IndexOf("### Host plus one friend", System.StringComparison.Ordinal);
        Assert.True(solo > 0 && host > solo, "Solo must come before Host plus one friend.");

        string soloText = section.Substring(solo, host - solo);
        string hostText = section.Substring(host);
        AssertNumberedSteps(soloText, 11);
        AssertNumberedSteps(hostText, 5);

        foreach (string subsection in new[] { soloText, hostText })
        {
            Assert.Contains("failed and was skipped", subsection, System.StringComparison.Ordinal);
            Assert.Contains("30 seconds", subsection, System.StringComparison.Ordinal);
        }
    }

    // GW-U-1940 (PD4): the Solo steps carry the contracted actions and the expected texts.
    [Fact]
    public void TheSoloStepsCarryTheContractedActionsAndTexts()
    {
        string solo = PutAwaySection();
        solo = solo.Substring(solo.IndexOf("### Solo", System.StringComparison.Ordinal));
        solo = solo.Substring(0, solo.IndexOf("### Host plus one friend", System.StringComparison.Ordinal));

        foreach (string literal in new[]
                 {
                     "Wood 30",
                     "Cooked meat",
                     "Resin",
                     "Stone",
                     "Flint",
                     "Put away: 2 stacks moved, 0 no room",
                     "[station.actions.deposit-button] Deposit Started",
                     "`DepositPreview = true`",
                     "Put away cancelled",
                     "No chests in this base",
                     "`DepositKey = G`",
                     "Not in a base",
                     "Tab",
                     "`MovesPerFrame = 1`",
                     "`StationButtons = false`",
                 })
        {
            Assert.Contains(literal, solo, System.StringComparison.Ordinal);
        }
    }

    // GW-U-1940 (PD5): the Host plus one friend steps name the host's run and the refusal of the friend.
    [Fact]
    public void TheHostAndFriendStepsNameTheHostRunAndTheRefusal()
    {
        string section = PutAwaySection();
        string host = section.Substring(section.IndexOf("### Host plus one friend", System.StringComparison.Ordinal));

        foreach (string literal in new[] { "Organize in progress by <HostName>", "`MovesPerFrame = 1`", "in the centre", "host's chests" })
        {
            Assert.Contains(literal, host, System.StringComparison.Ordinal);
        }
    }

    // GW-U-1940 (PD7): the closing note names what the unit tests prove, the section holds the contracted literals, and it has no emoji.
    [Fact]
    public void TheClosingNoteNamesTheUnitTestsAndTheSectionHoldsTheLiterals()
    {
        string section = PutAwaySection();

        foreach (string literal in new[]
                 {
                     "Put away inventory",
                     "Put away: 2 stacks moved, 0 no room",
                     "Put away cancelled",
                     "Not in a base",
                     "No chests in this base",
                     "Organize in progress by <HostName>",
                     "`DepositKey`",
                     "`DepositPreview`",
                     "`DepositNeverItems`",
                     "`StationButtons`",
                     "`MovesPerFrame = 1`",
                     "StationPanelLayout",
                     "coordinator",
                     "planner",
                 })
        {
            Assert.Contains(literal, section, System.StringComparison.Ordinal);
        }

        Assert.DoesNotMatch(@"[\uD800-\uDFFF]", section);
    }

    // GW-U-1940 (8.2): the README Station buttons section names the feature, and the old "later version" sentence is gone.
    [Fact]
    public void TheReadmeStationButtonsSectionDescribesPutAwayInventory()
    {
        string readme = RepoRoot.ReadText(ReadmeFile);
        Match station = Regex.Match(readme, @"^## Station buttons[ \t]*$", RegexOptions.Multiline);
        Assert.True(station.Success, "The README Station buttons section is missing.");

        Match next = Regex.Match(readme.Substring(station.Index + station.Length), @"^## ", RegexOptions.Multiline);
        int end = next.Success ? station.Index + station.Length + next.Index : readme.Length;
        string section = readme.Substring(station.Index, end - station.Index);

        foreach (string literal in new[] { "Put away inventory", "DepositPreview", "DepositKey", "DepositNeverItems" })
        {
            Assert.Contains(literal, section, System.StringComparison.Ordinal);
        }

        Assert.DoesNotContain("comes in a later version", readme, System.StringComparison.Ordinal);
    }

    // GW-U-1940 (8.2): the CHANGELOG Put away line sits under Unreleased and Added, directly after the Organize base line.
    [Fact]
    public void TheChangelogPutAwayLineFollowsTheOrganizeBaseLine()
    {
        string changelog = RepoRoot.ReadText(ChangelogFile);
        // Edited by S-E009-001 (quality-audit QA4.7): the bullets now sit under the 0.1.0 release.
        int unreleased = changelog.IndexOf("## [0.1.0]", System.StringComparison.Ordinal);
        int added = changelog.IndexOf("### Added", unreleased, System.StringComparison.Ordinal);
        int organize = changelog.IndexOf(OrganizeLine, System.StringComparison.Ordinal);
        int putAway = changelog.IndexOf(PutAwayLinePrefix, System.StringComparison.Ordinal);

        Assert.True(unreleased >= 0 && added > unreleased, "The Added section must sit under 0.1.0.");
        Assert.True(organize > added && putAway > organize, "The Put away line must sit under Added, after the Organize base line.");
        Assert.Equal(organize + OrganizeLine.Length + 1, putAway);
        int lineEnd = changelog.IndexOf('\n', putAway);
        string line = changelog.Substring(putAway, lineEnd - putAway);
        foreach (string literal in new[] { "DepositKey", "DepositPreview", "DepositNeverItems", "Put away: 4 stacks moved, 2 no room" })
        {
            Assert.Contains(literal, line, System.StringComparison.Ordinal);
        }
    }

    private static string PutAwaySection()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        Match heading = Regex.Match(text, PutAwayHeading, RegexOptions.Multiline);
        Assert.True(heading.Success, "The Put away inventory heading is missing.");

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
            Assert.Equal(steps, int.Parse(step.Groups[1].Value, CultureInfo.InvariantCulture));

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
