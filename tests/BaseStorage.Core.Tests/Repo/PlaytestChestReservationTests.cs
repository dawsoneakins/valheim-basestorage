namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;

/// <summary>
/// GW-U-790, GW-U-791 and GW-U-792 (AC-7, contract 7 P1-P8): docs/PLAYTEST.md holds the "Chest reservation" section. The file is
/// read CRLF-normalised. The game is never launched by the pipeline.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestChestReservationTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string HeadingPattern = @"^## Chest reservation \(AC-5\.2, AC-5\.3, AC-6\.3\)[ \t]*$";
    private const string BaseMembershipPattern = @"^## Base membership \(AC-2\.3, AC-2\.5, AC-5\.3\)[ \t]*$";

    // GW-U-790 (P1): exactly one heading, after Base membership, and the only level-2 section after it is the S-E006-002
    // "Version handshake" section (GW-U-1259), followed by the S-E003-004 "Craft from base" section.
    [Fact]
    public void TheSectionHeadingIsExactAndFollowsBaseMembership()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        MatchCollection headings = Regex.Matches(text, HeadingPattern, RegexOptions.Multiline);
        Match membership = Regex.Match(text, BaseMembershipPattern, RegexOptions.Multiline);

        Assert.Single(headings);
        Assert.True(membership.Success, "docs/PLAYTEST.md has no Base membership section.");
        Assert.True(headings[0].Index > membership.Index, "The Chest reservation section must come after Base membership.");
        string[] later = Regex.Matches(text.Substring(headings[0].Index + headings[0].Length), @"(?m)^## .*$")
            .Select(match => match.Value.TrimEnd())
            .ToArray();
        // Edited by S-E003-004 (ADR-GWIN-124): the "Craft from base" section is appended after the handshake section.
        // Edited by S-E003-006, ADR-GWIN-132 (GW-U-1666): the "Build from base" section follows Craft from base.
        // Edited by integration (S-E003-005 + S-E003-006): "Requirement display" (S-E003-005) follows Build from base.
        // Edited by S-E003-007, ADR-GWIN-128: "Upgrade and multi-craft from base" is appended after Requirement display.
        // Edited by integration (S-E006-004): "Config sync" is appended after the S-E003 sections (the last level-2 section).
        Assert.Equal(
            new[]
            {
                "## Version handshake (AC-6.1, AC-6.3)",
                "## Craft from base (AC-2.4, AC-3.1, AC-3.4)",
                "## Build from base (AC-2.4, AC-3.1, AC-3.4)",
                "## Requirement display (AC-3.2)",
                "## Upgrade and multi-craft from base (AC-3.5)",
                "## Craft from base off (AC-3.6)", // Edited by S-E003-008, craft-from-base-toggle 8.3: the K1 section sits between Upgrade and Config sync.
                "## Feedback messages (AC-8.1, AC-7.3)", // Edited by S-E008-001 (ADR-GWIN-153): the section sits immediately before Config sync.
                "## Config sync (AC-6.2, AC-6.3)",
            },
            later);
    }

    // GW-U-790 (P3): Solo then Host plus one friend, each a numbered list that restarts at 1 with an Expected line per step.
    [Fact]
    public void SubsectionsAreNumberedFromOneWithAnExpectedLinePerStep()
    {
        string section = Section();
        int solo = section.IndexOf("### Solo", StringComparison.Ordinal);
        int host = section.IndexOf("### Host plus one friend", StringComparison.Ordinal);
        Assert.True(solo > 0 && host > solo, "Solo must come before Host plus one friend.");

        foreach (string subsection in new[] { "### Solo", "### Host plus one friend" })
        {
            string body = Subsection(section, subsection);
            Assert.Matches(@"(?m)^1\. ", body);
            var missing = new List<string>();
            string? open = null;
            foreach (string line in body.Split('\n'))
            {
                string trimmed = line.Trim();
                if (Regex.IsMatch(trimmed, @"^\d+\. "))
                {
                    if (open is not null)
                    {
                        missing.Add(open);
                    }

                    open = trimmed;
                }
                else if (trimmed.StartsWith("Expected:", StringComparison.Ordinal))
                {
                    open = null;
                }
            }

            if (open is not null)
            {
                missing.Add(open);
            }

            Assert.Empty(missing);
        }
    }

    // GW-U-790 (P8): no emoji, settings in backticks, Windows paths with backslashes.
    [Fact]
    public void WordingFollowsTheExistingFile()
    {
        string section = Section();

        Assert.DoesNotMatch(@"[\uD800-\uDFFF]|\p{So}", section);
        string outsideCode = Regex.Replace(section, "`[^`\n]*`", string.Empty);
        Assert.DoesNotContain("ReserveTimeoutMs", outsideCode, StringComparison.Ordinal);
        Assert.DoesNotContain("DebugLogging", outsideCode, StringComparison.Ordinal);
        Assert.Contains(@"<ValheimDir>\BepInEx\LogOutput.log", section, StringComparison.Ordinal);
        Assert.DoesNotContain("BepInEx/", section, StringComparison.Ordinal);
    }

    // GW-U-791 (P2): the preamble.
    [Fact]
    public void ThePreambleExplainsDebugLoggingTheProbeAndTheTimeoutSetting()
    {
        string preamble = Preamble();

        Assert.Contains("## Debug logging", preamble, StringComparison.Ordinal);
        Assert.Contains("[Debug  :BaseStorage]", preamble, StringComparison.Ordinal);
        Assert.Contains(@"<ValheimDir>\BepInEx\LogOutput.log", preamble, StringComparison.Ordinal);
        Assert.Contains("`DebugLogging = true`", preamble, StringComparison.Ordinal);
        Assert.Contains("`DebugLogging = false`", preamble, StringComparison.Ordinal);
        Assert.Matches(@"(?i)left shift", preamble);
        Assert.Matches(@"(?i)half of `ReserveTimeoutMs`", preamble);
        Assert.Matches(@"(?i)writes nothing", preamble);
        Assert.Contains("`ReserveTimeoutMs = 20000`", preamble, StringComparison.Ordinal);
        Assert.Contains("`[Network]`", preamble, StringComparison.Ordinal);
        Assert.Contains("`1500`", preamble, StringComparison.Ordinal);
        Assert.Matches(@"(?i)read (once )?at (game )?start", preamble);
    }

    // GW-U-791 (P2): M1-M5 verbatim with <> placeholders.
    [Theory]
    [InlineData("Reserve <id> [<operation>]: route <Route>")]
    [InlineData("Reserve <id> [<operation>]: <From> -> <To>")]
    [InlineData("(<Failure>)")]
    [InlineData("Reserve <id> [<operation>]: in use by <holderLabel>")]
    [InlineData("Reserve <id> [<operation>]: open response <granted|refused> consumed, chest UI not shown")]
    [InlineData("Reserve <id> [late]: open response <granted|refused> consumed, chest UI not shown")]
    public void ThePreambleQuotesTheDebugLineFormats(string format)
    {
        Assert.Contains(format, Preamble(), StringComparison.Ordinal);
    }

    // GW-U-792 (P4): Solo in order.
    [Fact]
    public void SoloCoversTheOwnedProbeTheRealOpenAndTheDisabledProbeInOrder()
    {
        string solo = Subsection(Section(), "### Solo");

        AssertInOrder(
            solo,
            "route LocalOwner",
            "Requesting -> Granted",
            "Granted -> Writing",
            "Writing -> Releasing",
            "Releasing -> Completed",
            "never opens",
            "no `open response`",
            @"(?m)^2\. .*press(es)? E",
            @"opens (normally|as in vanilla)[^\n]*\n[^\n]*no (new )?`Reserve` line|no (new )?`Reserve` line",
            "`DebugLogging = false`",
            "Shift",
            "as in vanilla",
            "no `Reserve` line",
            "back to `true`|back to true");
    }

    // GW-U-792 (P5): Host plus one friend in order.
    [Fact]
    public void HostPlusOneFriendCoversTheContractStepsInOrder()
    {
        string host = Subsection(Section(), "### Host plus one friend");

        AssertInOrder(
            host,
            "route SkipInUse",
            "in use by <FriendName>",
            @"Requesting -> Failed \(InUseByOther\)",
            "stays open",
            "route SendRequest",
            "open response granted consumed, chest UI not shown",
            "Requesting -> Granted",
            "Releasing -> Completed",
            "no chest window",
            "(?i)in-use refusal",
            "opens",
            "Private",
            "route SkipAccessDenied",
            @"Requesting -> Failed \(Denied\)",
            "`ReserveTimeoutMs = 1`",
            "route SendRequest",
            @"Requesting -> Failed \(Timeout\)",
            @"Reserve <id> \[late\]: open response granted consumed, chest UI not shown",
            "no chest window",
            "20000",
            "route LocalOwner",
            "(?i)quits",
            "(?i)opens");
    }

    // GW-U-792 (P6): each subsection ends with the skip-and-idle step.
    [Theory]
    [InlineData("### Solo")]
    [InlineData("### Host plus one friend")]
    public void TheLastStepChecksForSkipsAndTheThirtySecondStillWindow(string subsection)
    {
        string body = Subsection(Section(), subsection);
        string[] lines = body.Split('\n');
        int last = Array.FindLastIndex(lines, line => Regex.IsMatch(line.Trim(), @"^\d+\. "));
        Assert.True(last >= 0, subsection + " has no numbered step.");
        string lastStep = string.Join("\n", lines, last, lines.Length - last);

        Assert.Contains("failed and was skipped", lastStep, StringComparison.Ordinal);
        Assert.Contains("30 seconds", lastStep, StringComparison.Ordinal);
        Assert.Contains("`Reserve`", lastStep, StringComparison.Ordinal);
    }

    // GW-U-792 (P7): the closing note.
    [Fact]
    public void TheClosingNoteNamesTheUnitTestedRaces()
    {
        string section = Section();
        int host = section.IndexOf("### Host plus one friend", StringComparison.Ordinal);
        string tail = section.Substring(host);
        string[] lines = tail.Split('\n');
        int last = Array.FindLastIndex(lines, line => line.Trim().StartsWith("Expected:", StringComparison.Ordinal));
        string note = string.Join("\n", lines, last + 1, lines.Length - last - 1);

        Assert.Matches(@"(?i)deny", note);
        Assert.Matches(@"(?i)owner[- ]lost[^\n]*writing|lost while writing", note);
        Assert.Matches(@"(?i)unit tests", note);
    }

    private static void AssertInOrder(string body, params string[] patterns)
    {
        int cursor = 0;
        foreach (string pattern in patterns)
        {
            Match found = Regex.Match(body.Substring(cursor), pattern);
            Assert.True(found.Success, $"The subsection does not contain '{pattern}' after the preceding required text.");
            cursor += found.Index + found.Length;
        }
    }

    private static string Section()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        Match heading = Regex.Match(text, HeadingPattern, RegexOptions.Multiline);
        Assert.True(heading.Success, "docs/PLAYTEST.md has no exact '## Chest reservation (AC-5.2, AC-5.3, AC-6.3)' heading.");

        int start = heading.Index + heading.Length;
        Match next = Regex.Match(text.Substring(start), @"^## ", RegexOptions.Multiline);
        return next.Success ? text.Substring(start, next.Index) : text.Substring(start);
    }

    private static string Preamble()
    {
        string section = Section();
        int solo = section.IndexOf("### Solo", StringComparison.Ordinal);
        Assert.True(solo > 0, "The Chest reservation section has no '### Solo' subsection.");
        return section.Substring(0, solo);
    }

    private static string Subsection(string section, string heading)
    {
        Match start = Regex.Match(section, $@"(?m)^{Regex.Escape(heading)}[ \t]*$");
        Assert.True(start.Success, $"The Chest reservation section has no '{heading}' subsection.");

        int from = start.Index + start.Length;
        Match next = Regex.Match(section.Substring(from), @"(?m)^#{2,3} ");
        return next.Success ? section.Substring(from, next.Index) : section.Substring(from);
    }
}
