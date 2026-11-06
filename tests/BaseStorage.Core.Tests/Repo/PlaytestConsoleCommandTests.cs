namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// GW-U-2324 and GW-U-2325 (AC-5; organize-console-command contract 7.1, rules PT1-PT5): docs/PLAYTEST.md holds the Organize console
/// command section once, between Category assignment and Organize, in the numbered Solo and Host plus one friend form with Expected lines,
/// and its preamble and steps carry the required literals in order. Red until the implement phase writes the section. Not a test class.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestConsoleCommandTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string HeadingPattern = @"^## Organize console command \(AC-2\.4, AC-5\.4\)[ \t]*$";
    private const string CategoryHeadingPattern = @"^## Category assignment \(AC-4\.3, AC-8\.2\)[ \t]*$";
    private const string OrganizeHeadingPattern = @"^## Organize \(AC-2\.4, AC-4\.7, AC-4\.8, AC-5\.4\)[ \t]*$";

    // GW-U-2324 (PT1, PT3): the heading appears once, between Category assignment and Organize, and the steps are numbered with Expected lines.
    [Fact]
    public void TheSectionSitsBetweenCategoryAssignmentAndOrganizeInTheNumberedForm()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        MatchCollection headings = Regex.Matches(text, HeadingPattern, RegexOptions.Multiline);
        Match category = Regex.Match(text, CategoryHeadingPattern, RegexOptions.Multiline);
        Match organize = Regex.Match(text, OrganizeHeadingPattern, RegexOptions.Multiline);

        Assert.Single(headings);
        Assert.True(category.Success && organize.Success, "The Category assignment or Organize section is missing.");
        int heading = headings[0].Index;
        Assert.True(category.Index < heading && heading < organize.Index, "The console section must sit between Category assignment and Organize.");
        // Integration 2026-10-29: the S-E008-002 Gamepad cursor section shares this slot, so it is the one other heading allowed here.
        Assert.DoesNotMatch(@"(?m)^## (?!Gamepad cursor \(AC-8\.2, AC-7\.3\)[ \t]*$)", text.Substring(category.Index + category.Length, heading - category.Index - category.Length));
        Assert.DoesNotMatch(@"(?m)^## (?!Gamepad cursor \(AC-8\.2, AC-7\.3\)[ \t]*$)", text.Substring(heading + headings[0].Length, organize.Index - heading - headings[0].Length));

        string section = Section(text);
        int solo = section.IndexOf("### Solo", StringComparison.Ordinal);
        int host = section.IndexOf("### Host plus one friend", StringComparison.Ordinal);
        Assert.True(solo >= 0 && host > solo, "The section needs ### Solo then ### Host plus one friend.");

        AssertNumberedWithExpected(section.Substring(solo, host - solo));
        AssertNumberedWithExpected(section.Substring(host));
    }

    // GW-U-2325 (PT2, PT4, PT5): the preamble names -console and F5, and the required literals appear in order in each subsection.
    [Fact]
    public void ThePreambleAndTheStepsCarryTheRequiredLiterals()
    {
        string section = Section(RepoRoot.ReadText(PlaytestFile));
        int solo = section.IndexOf("### Solo", StringComparison.Ordinal);
        int host = section.IndexOf("### Host plus one friend", StringComparison.Ordinal);
        string preamble = section.Substring(0, solo);

        Assert.Contains("-console", preamble, StringComparison.Ordinal);
        Assert.Contains("F5", preamble, StringComparison.Ordinal);
        Assert.Contains("dons-qol organize", preamble, StringComparison.Ordinal);

        AssertInOrder(section.Substring(solo, host - solo),
            "organize - start an Organize for the base you are in",
            "Usage: dons-qol organize",
            "Not in a base",
            "Organize preview open, close the console to confirm",
            "Organize started",
            "No chests in this base",
            "Organize in progress by <YourName>");

        AssertInOrder(section.Substring(host),
            "Organize in progress by <HostName>",
            "Organize preview open, close the console to confirm",
            "Not in a base",
            "Organize in progress by <FriendName>");
    }

    private static string Section(string text)
    {
        Match match = Regex.Match(text, HeadingPattern, RegexOptions.Multiline);
        Assert.True(match.Success, "The Organize console command section is missing.");

        int start = match.Index + match.Length;
        Match next = Regex.Match(text.Substring(start), @"^## ", RegexOptions.Multiline);
        int end = next.Success ? start + next.Index : text.Length;
        return text.Substring(start, end - start);
    }

    private static void AssertNumberedWithExpected(string subsection)
    {
        MatchCollection steps = Regex.Matches(subsection, @"(?m)^(\d+)\. ");
        int[] numbers = steps.Select(m => int.Parse(m.Groups[1].Value)).ToArray();
        Assert.Equal(Enumerable.Range(1, numbers.Length), numbers);
        Assert.NotEmpty(numbers);

        for (int i = 0; i < steps.Count; i++)
        {
            int end = i + 1 < steps.Count ? steps[i + 1].Index : subsection.Length;
            Assert.Contains("Expected:", subsection.Substring(steps[i].Index, end - steps[i].Index), StringComparison.Ordinal);
        }
    }

    private static void AssertInOrder(string text, params string[] literals)
    {
        int position = 0;
        foreach (string literal in literals)
        {
            int found = text.IndexOf(literal, position, StringComparison.Ordinal);
            Assert.True(found >= 0, "Missing or out of order: " + literal);
            position = found + literal.Length;
        }
    }
}
