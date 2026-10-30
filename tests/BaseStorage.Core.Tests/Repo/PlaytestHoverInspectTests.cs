namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// AC-6 (spec S-E002-005, ADR-GWIN-032 section 5): docs/PLAYTEST.md holds the "Hover inspect" section, after the Base membership section, with the
/// two subsections, the literal lines, the ordered Solo steps, the deferred Ores and Keep steps, and the final 30-second step. The section is read as text.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestHoverInspectTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string HoverHeadingPattern = @"^## Hover inspect \(AC-2\.6, AC-8\.3\)[ \t]*$";
    private const string MembershipHeadingPattern = @"^## Base membership \(AC-2\.3, AC-2\.5, AC-5\.3\)[ \t]*$";
    private const string Solo = "### Solo";
    private const string Host = "### Host plus one friend";

    private static readonly string[] SoloOrder =
    {
        "[Not in a base]", "[In base: 1 station]", "[In base: 2 stations]", "[Base: 1 chest]", "[Base: 2 chests]",
        "cart", "[Base: 0 chests]", "[Ores]", "[Keep]",
    };

    // GW-U-559 (AC-6): the exact heading, after the Base membership heading.
    [Fact]
    public void TheHeadingIsExactAndComesAfterBaseMembership()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        Match hover = Regex.Match(text, HoverHeadingPattern, RegexOptions.Multiline);
        Match membership = Regex.Match(text, MembershipHeadingPattern, RegexOptions.Multiline);

        Assert.True(hover.Success, "docs/PLAYTEST.md has no exact '## Hover inspect (AC-2.6, AC-8.3)' heading.");
        Assert.True(membership.Success, "docs/PLAYTEST.md has no '## Base membership (AC-2.3, AC-2.5, AC-5.3)' heading.");
        Assert.True(hover.Index > membership.Index, "The Hover inspect section must come after Base membership.");
    }

    // GW-U-559 (AC-6): the preamble names every line form and says that the line is one line.
    [Fact]
    public void ThePreambleNamesEveryLineFormAndSaysOneLine()
    {
        string preamble = Preamble();

        foreach (string form in new[]
        {
            "[In base: 1 station]", "[In base: <N> stations]", "[Not in a base]", "[Keep]", "[Base: 1 chest]", "[Base: <N> chests]",
        })
        {
            Assert.Contains(form, preamble, StringComparison.Ordinal);
        }

        Assert.Contains("one line", preamble, StringComparison.Ordinal);
    }

    // GW-U-559 (AC-6): each subsection has a numbered list that starts at 1, and every numbered step is followed by an Expected line.
    [Fact]
    public void EachSubsectionIsANumberedListWithAnExpectedLineAfterEveryStep()
    {
        string section = Section();

        foreach (string subsection in new[] { Solo, Host })
        {
            List<string> steps = Steps(Subsection(section, subsection));
            Assert.NotEmpty(steps);
            for (int i = 0; i < steps.Count; i++)
            {
                Assert.StartsWith((i + 1) + ". ", steps[i], StringComparison.Ordinal);
                string[] lines = steps[i].Split('\n');
                Assert.True(
                    lines.Length > 1 && lines[1].StartsWith("Expected:", StringComparison.Ordinal),
                    $"Step {i + 1} of '{subsection}' has no Expected line.");
            }
        }
    }

    // GW-U-559 (AC-6): the Solo steps show the lines in this order.
    [Fact]
    public void TheSoloStepsShowTheLinesInOrder()
    {
        string solo = Subsection(Section(), Solo);

        int cursor = 0;
        foreach (string item in SoloOrder)
        {
            int at = solo.IndexOf(item, cursor, StringComparison.Ordinal);
            Assert.True(at >= 0, $"'{item}' is missing from Solo after position {cursor}.");
            cursor = at + item.Length;
        }
    }

    // GW-U-559 (AC-6): the Ores and Keep steps are deferred to S-E004-004.
    [Fact]
    public void TheOresAndKeepStepsMentionTheDeferredStory()
    {
        List<string> steps = Steps(Subsection(Section(), Solo));

        foreach (string marker in new[] { "[Ores]", "[Keep]" })
        {
            string? step = steps.FirstOrDefault(s => s.Contains(marker, StringComparison.Ordinal));
            Assert.NotNull(step);
            Assert.Contains("S-E004-004", step!, StringComparison.Ordinal);
        }
    }

    // GW-U-559 (AC-6): the last step of each subsection covers the failure text, the 30-second hover, and exactly one line.
    [Fact]
    public void TheLastStepOfEachSubsectionCoversTheHoverCheck()
    {
        string section = Section();

        foreach (string subsection in new[] { Solo, Host })
        {
            List<string> steps = Steps(Subsection(section, subsection));
            Assert.NotEmpty(steps);
            string last = steps[steps.Count - 1];
            Assert.Contains("failed and was skipped", last, StringComparison.Ordinal);
            Assert.Contains("30 seconds", last, StringComparison.Ordinal);
            Assert.Contains("exactly one", last, StringComparison.Ordinal);
        }
    }

    private static string Text() => RepoRoot.ReadText(PlaytestFile);

    private static string Section()
    {
        string text = Text();
        Match heading = Regex.Match(text, HoverHeadingPattern, RegexOptions.Multiline);
        Assert.True(heading.Success, "docs/PLAYTEST.md has no Hover inspect section.");

        int start = heading.Index;
        int bodyStart = heading.Index + heading.Length;
        Match next = Regex.Match(text.Substring(bodyStart), @"(?m)^## ");
        int end = next.Success ? bodyStart + next.Index : text.Length;
        return text.Substring(start, end - start);
    }

    private static string Preamble()
    {
        string section = Section();
        int firstSubsection = section.IndexOf("\n### ", StringComparison.Ordinal);
        return firstSubsection < 0 ? section : section.Substring(0, firstSubsection);
    }

    private static string Subsection(string section, string heading)
    {
        int start = section.IndexOf("\n" + heading, StringComparison.Ordinal);
        Assert.True(start >= 0, $"The Hover inspect section has no '{heading}' subsection.");
        start += 1;

        int next = section.IndexOf("\n### ", start + heading.Length, StringComparison.Ordinal);
        return next < 0 ? section.Substring(start) : section.Substring(start, next - start);
    }

    /// <summary>The numbered steps of a subsection. Each step is its numbered line and the non-blank lines that follow it, trimmed and joined with a line break.</summary>
    private static List<string> Steps(string subsection)
    {
        var steps = new List<string>();
        StringBuilder? current = null;
        foreach (string line in subsection.Split('\n'))
        {
            string trimmed = line.Trim();
            if (Regex.IsMatch(trimmed, @"^\d+\. "))
            {
                if (current is not null)
                {
                    steps.Add(current.ToString());
                }

                current = new StringBuilder(trimmed);
            }
            else if (current is not null && trimmed.Length > 0 && !trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                current.Append('\n').Append(trimmed);
            }
        }

        if (current is not null)
        {
            steps.Add(current.ToString());
        }

        return steps;
    }
}
