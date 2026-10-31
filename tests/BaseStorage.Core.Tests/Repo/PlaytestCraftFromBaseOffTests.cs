namespace BaseStorage.Core.Tests.Repo;

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// GW-U-1821 to GW-U-1823 (AC-5; contract craft-from-base-toggle 9, rules K1-K5): the level-2 section
/// <c>## Craft from base off (AC-3.6)</c> of docs/PLAYTEST.md, its position and preamble, its numbered steps, and its style.
/// </summary>
public sealed class PlaytestCraftFromBaseOffTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string Heading = "## Craft from base off (AC-3.6)";
    private const string UpgradeHeading = "## Upgrade and multi-craft from base (AC-3.5)";
    private const string ConfigSyncHeading = "## Config sync (AC-6.2, AC-6.3)";
    // Edited by S-E008-001 (ADR-GWIN-153): the Feedback messages section sits between Craft from base off and Config sync.
    private const string FeedbackHeading = "## Feedback messages (AC-8.1, AC-7.3)";
    private const string Closing = "That every craft-from-base patch checks `CraftFromBase` before anything else is proven by an automated test, not by manual steps.";

    // GW-U-1821 (K1, K2): one heading, between Upgrade and multi-craft and Config sync, which stays the last level-2 section.
    // The preamble names the setting, vanilla, the absence of Reserve and Craft lines, the F1 live change, a started operation, and host sync.
    [Fact]
    public void TheSectionHasItsHeadingBetweenUpgradeAndConfigSyncAndItsPreamble()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        string[] headings = Regex.Matches(text, @"(?m)^## .*$").Select(match => match.Value.TrimEnd()).ToArray();
        Assert.Single(headings, line => line == Heading);

        int index = System.Array.IndexOf(headings, Heading);
        // Edited by S-E008-001 (ADR-GWIN-153): the section is followed by Feedback messages, and Config sync stays last.
        Assert.True(index > 0 && index < headings.Length - 2, "The section must sit between two level-2 sections, with one more before Config sync.");
        Assert.Equal(UpgradeHeading, headings[index - 1]);
        Assert.Equal(FeedbackHeading, headings[index + 1]);
        Assert.Equal(ConfigSyncHeading, headings[index + 2]);
        Assert.Equal(ConfigSyncHeading, headings[headings.Length - 1]);

        string section = Section(text);
        int firstStep = section.IndexOf("\n### ", System.StringComparison.Ordinal);
        Assert.True(firstStep > 0, "The section has no ### subsection.");
        string preamble = section.Substring(0, firstStep);

        Assert.Matches("`CraftFromBase`", preamble);
        Assert.Matches(@"`\[General\]`", preamble);
        Assert.Matches("default `true`", preamble);
        Assert.Matches("vanilla", preamble);
        Assert.Matches(@"`Reserve <id> \[craft\]`", preamble);
        Assert.Matches("`Craft`", preamble);
        Assert.Matches(@"\bF1\b", preamble);
        Assert.Matches("without a restart", preamble);
        Assert.Matches("started", preamble);
        Assert.Matches("host-synced", preamble);
        Assert.Matches("join", preamble);
    }

    // GW-U-1822 (K3, K4): Solo and Host plus one friend each have seven steps numbered from 1, with one Expected: per step, and the
    // content of each step appears in the order the contract gives.
    [Fact]
    public void SoloAndHostStepsAreNumberedFromOneWithOneExpectedEachInContractOrder()
    {
        string section = Section(RepoRoot.ReadText(PlaytestFile));
        string solo = Subsection(section, "### Solo", "### Host plus one friend");
        string host = Subsection(section, "### Host plus one friend", null);

        string[][] soloTokens =
        {
            new[] { "`DebugLogging = true`", "`CraftFromBase = true`", "Workbench", "20 `Wood`", "`20/<need>`" },
            new[] { "`CraftFromBase = false`", "F1", "`<need>`" },
            new[] { "inventory", "`Reserve <id> [craft]`" },
            new[] { "Hammer", "`<need>`", "20 `Wood`" },
            new[] { "upgrade", "Left Shift", "multi-craft" },
            new[] { "`CraftFromBase = true`", "without a restart", "`20/<need>`" },
            new[] { "30 seconds", "failed and was skipped" },
        };

        string[][] hostTokens =
        {
            new[] { "`CraftFromBase = false`", "Start server", "Config sync: CraftFromBase = false (host)" },
            new[] { "Workbench", "Reserve" },
            new[] { "Hammer", "tooltip" },
            new[] { "`CraftFromBase = true`", "F1", "vanilla" },
            new[] { "`CraftFromBase = true`", "Workbench", "`20/<need>`", "stays vanilla" },
            new[] { "quits to the main menu", "joins again", "Config sync: CraftFromBase = true (host)", "`20/<need>`" },
            new[] { "30 seconds", "failed and was skipped" },
        };

        AssertSteps(solo, soloTokens);
        AssertSteps(host, hostTokens);
    }

    // GW-U-1823 (K5): no emoji, settings and log text in backticks, and the closing sentence ends the section.
    [Fact]
    public void TheSectionHasNoEmojiKeepsSettingsInBackticksAndEndsWithTheClosingSentence()
    {
        string section = Section(RepoRoot.ReadText(PlaytestFile));
        string withoutCode = Regex.Replace(section, "`[^`]*`", string.Empty);

        Assert.DoesNotMatch(@"[☀-➿⬀-⯿]|[\uD83C-\uD83E][\uDC00-\uDFFF]", section);
        Assert.DoesNotMatch("CraftFromBase", withoutCode);
        Assert.DoesNotMatch("DebugLogging", withoutCode);

        string last = section.Split('\n').Select(line => line.TrimEnd()).Last(line => line.Length > 0);
        Assert.Equal(Closing, last);
    }

    private static string Section(string text)
    {
        Match heading = Regex.Match(text, "(?m)^" + Regex.Escape(Heading) + "$");
        Assert.True(heading.Success, "docs/PLAYTEST.md has no '" + Heading + "' section.");

        int after = heading.Index + heading.Length;
        Match next = Regex.Match(text.Substring(after), @"(?m)^## ");
        int end = next.Success ? after + next.Index : text.Length;
        return text.Substring(heading.Index, end - heading.Index);
    }

    private static string Subsection(string section, string start, string? end)
    {
        int from = section.IndexOf(start, System.StringComparison.Ordinal);
        Assert.True(from >= 0, "The section has no '" + start + "' subsection.");

        int to = end is null ? section.Length : section.IndexOf(end, from + start.Length, System.StringComparison.Ordinal);
        Assert.True(to > from, "The '" + start + "' subsection has no end.");
        return section.Substring(from, to - from);
    }

    private static void AssertSteps(string subsection, string[][] tokens)
    {
        List<string> steps = new();
        foreach (string line in subsection.Split('\n'))
        {
            if (Regex.IsMatch(line, @"^\d+\. "))
            {
                steps.Add(line);
            }
            else if (steps.Count > 0)
            {
                steps[steps.Count - 1] += "\n" + line;
            }
        }

        Assert.True(tokens.Length == steps.Count, "Expected " + tokens.Length + " numbered steps, found " + steps.Count + ".");
        for (int i = 0; i < steps.Count; i++)
        {
            Assert.StartsWith((i + 1) + ". ", steps[i]);
            Assert.Single(Regex.Matches(steps[i], "Expected:"));
            AssertInOrder(steps[i], tokens[i]);
        }
    }

    private static void AssertInOrder(string text, string[] tokens)
    {
        int position = 0;
        foreach (string token in tokens)
        {
            int at = text.IndexOf(token, position, System.StringComparison.Ordinal);
            Assert.True(at >= 0, "Expected '" + token + "' after character " + position + " of the step: " + text);
            position = at + token.Length;
        }
    }
}
