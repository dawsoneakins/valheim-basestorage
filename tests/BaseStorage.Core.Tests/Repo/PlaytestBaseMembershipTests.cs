namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// AC-10 (contract section 9, ADR-GWIN-031): docs/PLAYTEST.md holds the "Base membership" section with requirements R1 to R8.
/// The section is read as text. The game is never launched by the pipeline (spec non-goal).
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestBaseMembershipTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string HeadingPattern = @"^## Base membership \(AC-2\.3, AC-2\.5, AC-5\.3\)[ \t]*$";

    // GW-U-532 (R1)
    [Fact]
    public void TheSectionHeadingIsExactlyAndFollowsCategoryFile()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        Match heading = Regex.Match(text, HeadingPattern, RegexOptions.Multiline);

        Assert.True(heading.Success, "docs/PLAYTEST.md has no exact '## Base membership (AC-2.3, AC-2.5, AC-5.3)' heading.");
        Match category = Regex.Match(text, @"^## Category file[ \t]*$", RegexOptions.Multiline);
        Assert.True(category.Success, "docs/PLAYTEST.md has no '## Category file' section.");
        Assert.True(heading.Index > category.Index, "The Base membership section must come after '## Category file'.");
    }

    // GW-U-532 (R2)
    [Fact]
    public void ThePreambleNamesTheDebugLogAndTheD1AndD2FormatsAndSaysWhereCountsComeFrom()
    {
        string preamble = Preamble();

        Assert.Contains("## Debug logging", preamble, StringComparison.Ordinal);
        Assert.Contains("[Debug  :BaseStorage]", preamble, StringComparison.Ordinal);
        Assert.Contains("LogOutput.log", preamble, StringComparison.Ordinal);
        Assert.Matches(@"Registry: <[^>]+>[^\n]*\(<n> loaded\)", preamble);
        Assert.Contains("Base at station", preamble, StringComparison.Ordinal);
        Assert.Matches(@"(?i)counts?[^\n]*\bplac\w*[^\n]*\bstation\b", preamble);
    }

    // GW-U-532 (R3)
    [Fact]
    public void SoloAndHostSubsectionsEachHaveANumberedListThatStartsAtOne()
    {
        string section = Section();

        foreach (string subsection in new[] { "### Solo", "### Host plus one friend" })
        {
            Assert.Matches($@"(?m)^{Regex.Escape(subsection)}[ \t]*$", section);
            Assert.Matches(@"(?m)^1\. ", Subsection(section, subsection));
        }
    }

    // GW-U-532 (R8)
    [Fact]
    public void TheSectionHasNoEmoji()
    {
        Assert.DoesNotMatch(@"[\uD800-\uDFFF]|\p{So}", Section());
    }

    // GW-U-533 (R4)
    [Fact]
    public void EveryNumberedStepIsFollowedByAnExpectedLine()
    {
        string section = Section();
        var missing = new List<string>();
        string? open = null;
        foreach (string line in section.Split('\n'))
        {
            string trimmed = line.Trim();
            if (Regex.IsMatch(trimmed, @"^\d+\. ") || trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                if (open is not null)
                {
                    missing.Add(open);
                }

                open = Regex.IsMatch(trimmed, @"^\d+\. ") ? trimmed : null;
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

        Assert.Matches(@"(?m)^\d+\. ", section);
        Assert.Empty(missing);
    }

    // GW-U-533 (R5, Solo in order)
    [Fact]
    public void SoloCoversTheStationContainerCartShipTombstoneAndCampfireCasesInOrder()
    {
        string solo = Subsection(Section(), "### Solo");
        string[] steps =
        {
            "Registry: station added",
            @"0 container\(s\)",
            "Registry: container added",
            "2 eligible",
            "Registry: container removed",
            "cart",
            "1 vehicle",
            "ExcludeVehicles",
            "0 vehicle",
            "ship|raft|karve",
            "1 tombstone",
            "unload|far",
            @"\(<n> loaded\)",
            "campfire",
        };

        int cursor = 0;
        foreach (string step in steps)
        {
            Match found = Regex.Match(solo.Substring(cursor), step, RegexOptions.IgnoreCase);
            Assert.True(found.Success, $"The Solo subsection does not contain '{step}' after the preceding required line.");
            cursor += found.Index + found.Length;
        }
    }

    // GW-U-533 (R6)
    [Theory]
    [InlineData("Private")]
    [InlineData("1 access denied")]
    [InlineData("Public")]
    [InlineData("ward")]
    [InlineData("access denied")]
    [InlineData("1 in use by another")]
    public void HostPlusOneFriendNamesEachRequiredOutcome(string token)
    {
        Assert.Contains(token, Subsection(Section(), "### Host plus one friend"), StringComparison.Ordinal);
    }

    // GW-U-533 (R7)
    [Theory]
    [InlineData("### Solo")]
    [InlineData("### Host plus one friend")]
    public void TheLastStepOfEachSubsectionChecksForSkipsAndTheThirtySecondStillWindow(string subsection)
    {
        string body = Subsection(Section(), subsection);
        string[] lines = body.Split('\n');
        int last = Array.FindLastIndex(lines, line => Regex.IsMatch(line.Trim(), @"^\d+\. "));
        Assert.True(last >= 0, $"{subsection} has no numbered step.");
        string lastStep = string.Join("\n", lines, last, lines.Length - last);

        Assert.Contains("failed and was skipped", lastStep, StringComparison.Ordinal);
        Assert.Contains("30 seconds", lastStep, StringComparison.Ordinal);
    }

    private static string Section()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        Match heading = Regex.Match(text, HeadingPattern, RegexOptions.Multiline);
        Assert.True(heading.Success, "docs/PLAYTEST.md has no exact '## Base membership (AC-2.3, AC-2.5, AC-5.3)' heading.");

        int start = heading.Index + heading.Length;
        Match next = Regex.Match(text.Substring(start), @"^## ", RegexOptions.Multiline);
        return next.Success ? text.Substring(start, next.Index) : text.Substring(start);
    }

    private static string Preamble()
    {
        string section = Section();
        int solo = section.IndexOf("### Solo", StringComparison.Ordinal);
        Assert.True(solo > 0, "The Base membership section has no '### Solo' subsection.");
        return section.Substring(0, solo);
    }

    private static string Subsection(string section, string heading)
    {
        Match start = Regex.Match(section, $@"(?m)^{Regex.Escape(heading)}[ \t]*$");
        Assert.True(start.Success, $"The Base membership section has no '{heading}' subsection.");

        int from = start.Index + start.Length;
        Match next = Regex.Match(section.Substring(from), @"(?m)^#{2,3} ");
        return next.Success ? section.Substring(from, next.Index) : section.Substring(from);
    }
}
