namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;

/// <summary>
/// GW-U-1259 (AC-7.3, contract version-rpc-contracts.md 7): docs/PLAYTEST.md ends with the "Version handshake" section, which holds
/// the single-player no-handshake step and the matched, mismatched and missing host-plus-friend steps. The game is never launched.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestVersionHandshakeTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string HeadingPattern = @"^## Version handshake \(AC-6\.1, AC-6\.3\)[ \t]*$";

    // GW-U-1259 (P1): exactly one heading, after Chest reservation, and it is the last level-2 section.
    [Fact]
    public void TheSectionIsTheLastLevelTwoSection()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        MatchCollection headings = Regex.Matches(text, HeadingPattern, RegexOptions.Multiline);
        Match reservation = Regex.Match(text, @"^## Chest reservation \(AC-5\.2, AC-5\.3, AC-6\.3\)[ \t]*$", RegexOptions.Multiline);

        Assert.Single(headings);
        Assert.True(reservation.Success, "docs/PLAYTEST.md has no Chest reservation section.");
        Assert.True(headings[0].Index > reservation.Index, "The Version handshake section must come after Chest reservation.");
        // Edited by S-E003-004 (GW-U-1161): Craft from base is the last level-2 section, so Version handshake need not be last.
        // Edited by integration (S-E003-004..007 + S-E006-004): the S-E003 sections follow Version handshake, then Config sync, the last one.
        string[] later = Regex.Matches(text.Substring(headings[0].Index + headings[0].Length), @"(?m)^## .*$")
            .Select(match => match.Value.TrimEnd())
            .ToArray();
        Assert.Equal(
            new[]
            {
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

    // GW-U-1259 (P2): the preamble names the RPC, the 5 s deadline, the 1 s grace, the setting, and the log line prefix.
    [Fact]
    public void ThePreambleExplainsTheHandshake()
    {
        string preamble = Preamble();

        Assert.Contains("`BaseStorage_Version`", preamble, StringComparison.Ordinal);
        Assert.Contains("5 seconds", preamble, StringComparison.Ordinal);
        Assert.Contains("1 second", preamble, StringComparison.Ordinal);
        Assert.Contains("`RequireSameVersion`", preamble, StringComparison.Ordinal);
        Assert.Contains("`[General]`", preamble, StringComparison.Ordinal);
        Assert.Contains("`true`", preamble, StringComparison.Ordinal);
        Assert.Contains("`Version handshake: peer <id>`", preamble, StringComparison.Ordinal);
        Assert.Contains("`[Info   :BaseStorage]`", preamble, StringComparison.Ordinal);
        Assert.Contains(@"<ValheimDir>\BepInEx\LogOutput.log", preamble, StringComparison.Ordinal);
        Assert.Matches(@"(?i)vanilla connection-error path", preamble);
    }

    // GW-U-1259 (P3): Solo then Host plus one friend, each numbered from 1 with an Expected line per step.
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
            string? open = null;
            var missing = new List<string>();
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

    // GW-U-1259 (P4, AC-6.3): Solo proves that single-player sends nothing.
    [Fact]
    public void SoloCoversTheNoHandshakeCase()
    {
        string solo = Subsection(Section(), "### Solo");

        AssertInOrder(solo, "single-player", "no line contains `Version handshake`", "(?i)nothing is sent", "30 seconds", "failed and was skipped", "no `Version handshake` line");
    }

    // GW-U-1259 (P5, AC-6.1): the host-plus-friend steps cover matched, mismatched, missing and the lenient host, in order, with both sides' texts.
    [Fact]
    public void HostPlusOneFriendCoversMatchedMismatchedMissingAndLenient()
    {
        string host = Subsection(Section(), "### Host plus one friend");

        AssertInOrder(
            host,
            @"(?m)^1\. Matched",
            "accepted, host <version>, client <version>",
            @"(?m)^2\. Mismatched",
            "Incompatible version",
            "`Don's QOL version mismatch: host <hostVersion>, you <friendVersion>`",
            "centre notice",
            "refused, versions differ, host <hostVersion>, client <friendVersion>",
            "disconnected after refusal",
            "is the host and versions differ, leaving",
            @"(?m)^3\. Missing",
            "Incompatible version",
            "5 seconds",
            "`Don's QOL version mismatch: host <hostVersion>, joining player has no Don's QOL`",
            "refused, no Dons-QOL payload, host <hostVersion>",
            "disconnected after refusal",
            @"(?m)^4\. Lenient host",
            "`RequireSameVersion = false`",
            "accepted, host <hostVersion>, client none",
            "back to `true`",
            "30 seconds",
            "failed and was skipped",
            "no new `Version handshake` line");
    }

    // GW-U-1259 (P8): no emoji, settings in backticks, Windows paths with backslashes.
    [Fact]
    public void WordingFollowsTheExistingFile()
    {
        string section = Section();

        Assert.DoesNotMatch(@"[\uD800-\uDFFF]|\p{So}", section);
        string outsideCode = Regex.Replace(section, "`[^`\n]*`", string.Empty);
        Assert.DoesNotContain("RequireSameVersion", outsideCode, StringComparison.Ordinal);
        Assert.DoesNotContain("BepInEx/", section, StringComparison.Ordinal);
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
        Assert.True(heading.Success, "docs/PLAYTEST.md has no exact '## Version handshake (AC-6.1, AC-6.3)' heading.");
        int start = heading.Index + heading.Length;
        Match next = Regex.Match(text.Substring(start), @"^## ", RegexOptions.Multiline);
        return next.Success ? text.Substring(start, next.Index) : text.Substring(start);
    }

    private static string Preamble()
    {
        string section = Section();
        int solo = section.IndexOf("### Solo", StringComparison.Ordinal);
        Assert.True(solo > 0, "The Version handshake section has no '### Solo' subsection.");
        return section.Substring(0, solo);
    }

    private static string Subsection(string section, string heading)
    {
        Match start = Regex.Match(section, $@"(?m)^{Regex.Escape(heading)}[ \t]*$");
        Assert.True(start.Success, $"The Version handshake section has no '{heading}' subsection.");
        int from = start.Index + start.Length;
        Match next = Regex.Match(section.Substring(from), @"(?m)^#{2,3} ");
        return next.Success ? section.Substring(from, next.Index) : section.Substring(from);
    }
}
