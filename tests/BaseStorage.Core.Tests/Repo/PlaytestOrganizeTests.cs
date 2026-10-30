namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;

/// <summary>
/// GW-U-1385 (AC-5, spec AC-7.3): docs/PLAYTEST.md holds the Organize section between Hover inspect and Chest reservation, with a
/// Solo and a Host plus one friend subsection, numbered steps that each carry an Expected line, and the contracted texts.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestOrganizeTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string HeadingPattern = @"^## Organize \(AC-2\.4, AC-4\.7, AC-4\.8, AC-5\.4\)[ \t]*$";

    // GW-U-1385: exactly one heading, after Hover inspect and before Chest reservation.
    [Fact]
    public void TheSectionHeadingIsExactAndSitsBetweenHoverInspectAndChestReservation()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        MatchCollection headings = Regex.Matches(text, HeadingPattern, RegexOptions.Multiline);
        Match hover = Regex.Match(text, @"^## Hover inspect \(AC-2\.6, AC-8\.3\)[ \t]*$", RegexOptions.Multiline);
        Match reservation = Regex.Match(text, @"^## Chest reservation \(AC-5\.2, AC-5\.3, AC-6\.3\)[ \t]*$", RegexOptions.Multiline);

        Assert.Single(headings);
        Assert.True(hover.Success && reservation.Success, "The neighbouring sections are missing.");
        Assert.True(headings[0].Index > hover.Index, "The Organize section must come after Hover inspect.");
        Assert.True(headings[0].Index < reservation.Index, "The Organize section must come before Chest reservation.");
    }

    // GW-U-1385: Solo then Host plus one friend, numbered from 1 with an Expected line per step.
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

    // GW-U-1385: the texts the player sees, the settings in backticks, and the log path.
    [Theory]
    [InlineData("`OrganizeKey`")]
    [InlineData("`OrganizeSkipPreview`")]
    [InlineData("`MovesPerFrame`")]
    [InlineData("`OrganizeLockSeconds`")]
    [InlineData("Not in a base")]
    [InlineData("Organize in progress by <FriendName>")]
    [InlineData("Organize base")]
    [InlineData("Organized ")]
    [InlineData("stopped early")]
    [InlineData("Organize cancelled")]
    [InlineData("No chests in this base")]
    [InlineData("[Info   :BaseStorage]")]
    [InlineData(@"<ValheimDir>\BepInEx\LogOutput.log")]
    [InlineData("`[Keys]`")]
    public void TheSectionNamesTheContractedTexts(string needle)
    {
        Assert.Contains(needle, Section(), StringComparison.Ordinal);
    }

    // GW-U-1385: wording follows the file: no emoji, settings only in backticks, Windows paths.
    [Fact]
    public void WordingFollowsTheExistingFile()
    {
        string section = Section();

        Assert.DoesNotMatch(@"[\uD800-\uDFFF]|\p{So}", section);
        string outsideCode = Regex.Replace(section, "`[^`\n]*`", string.Empty);
        Assert.DoesNotContain("OrganizeKey", outsideCode, StringComparison.Ordinal);
        Assert.DoesNotContain("OrganizeSkipPreview", outsideCode, StringComparison.Ordinal);
        Assert.DoesNotContain("BepInEx/", section, StringComparison.Ordinal);
    }

    private static string Section()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        Match heading = Regex.Match(text, HeadingPattern, RegexOptions.Multiline);
        Assert.True(heading.Success, "docs/PLAYTEST.md has no exact Organize heading.");
        int start = heading.Index + heading.Length;
        Match next = Regex.Match(text.Substring(start), @"^## ", RegexOptions.Multiline);
        return next.Success ? text.Substring(start, next.Index) : text.Substring(start);
    }

    private static string Subsection(string section, string heading)
    {
        Match start = Regex.Match(section, $@"(?m)^{Regex.Escape(heading)}[ \t]*$");
        Assert.True(start.Success, $"The Organize section has no '{heading}' subsection.");
        int from = start.Index + start.Length;
        Match next = Regex.Match(section.Substring(from), @"(?m)^#{2,3} ");
        return next.Success ? section.Substring(from, next.Index) : section.Substring(from);
    }
}
