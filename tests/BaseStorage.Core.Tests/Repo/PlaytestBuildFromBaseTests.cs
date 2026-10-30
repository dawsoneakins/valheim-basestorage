namespace BaseStorage.Core.Tests.Repo;

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// GW-U-1660 to GW-U-1664 (AC-10; build-from-base contract 9 PB1-PB8): the docs/PLAYTEST.md "Build from base" section. The
/// craft-side amendments PB9 (GW-U-1665) are pinned in <see cref="PlaytestCraftFromBaseTests"/>. Text is CRLF-normalised.
/// </summary>
public sealed class PlaytestBuildFromBaseTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string CraftHeading = "## Craft from base (AC-2.4, AC-3.1, AC-3.4)";
    private const string Heading = "## Build from base (AC-2.4, AC-3.1, AC-3.4)";

    // GW-U-1660 (PB1, AC-10): exactly one Build from base heading, directly after the Craft from base heading.
    // Edited by integration (S-E003-005 + S-E003-006): Requirement display (S-E003-005) follows Build from base, so it is no longer the last level-2 section.
    [Fact]
    public void TheHeadingIsOnceDirectlyAfterCraftFromBase()
    {
        string text = RepoRoot.Normalise(RepoRoot.ReadText(PlaytestFile));
        MatchCollection headings = Regex.Matches(text, @"^## Build from base \(AC-2\.4, AC-3\.1, AC-3\.4\)[ \t]*$", RegexOptions.Multiline);
        Assert.Single(headings);

        string[] levelTwo = Regex.Matches(text, @"(?m)^## .*$").Select(match => match.Value.TrimEnd()).ToArray();
        int craft = Array.IndexOf(levelTwo, CraftHeading);
        Assert.True(craft >= 0, "docs/PLAYTEST.md has no Craft from base section.");
        Assert.Equal(Heading, levelTwo[craft + 1]);
    }

    // GW-U-1661 (PB2, AC-10): the preamble says what building counts, which base, the setting, the notice and the log.
    [Fact]
    public void ThePreambleSaysWhatCountsWhichBaseTheSettingTheNoticeAndTheLog()
    {
        string preamble = Section().Split(new[] { "### Solo" }, StringSplitOptions.None)[0];

        Assert.Contains("Hammer", preamble, StringComparison.Ordinal);
        Assert.Contains("(or any build tool)", preamble, StringComparison.Ordinal);
        Assert.Contains("as well as the inventory", preamble, StringComparison.Ordinal);
        Assert.Contains("before the piece appears", preamble, StringComparison.Ordinal);
        Assert.Contains("station build range contains the player", preamble, StringComparison.Ordinal);
        Assert.Contains("`CraftFromBase`", preamble, StringComparison.Ordinal);
        Assert.Contains("[General]", preamble, StringComparison.Ordinal);
        Assert.Contains("Materials moved, try again", preamble, StringComparison.Ordinal);
        Assert.Contains("## Debug logging", preamble, StringComparison.Ordinal);
        Assert.Contains("[Debug  :BaseStorage]", preamble, StringComparison.Ordinal);
        Assert.Contains(@"<ValheimDir>\BepInEx\LogOutput.log", preamble, StringComparison.Ordinal);
        Assert.Contains("[craft]", preamble, StringComparison.Ordinal);
    }

    // GW-U-1661 (PB2): BK1 to BK6 are quoted verbatim with <> placeholders.
    [Fact]
    public void TheDebugLineFormatsAreQuotedWithPlaceholders()
    {
        string preamble = Section().Split(new[] { "### Solo" }, StringSplitOptions.None)[0];

        foreach (string line in new[]
                 {
                     "Build <token>: started, <takes> take(s) from <chests> chest(s)",
                     "Build <token>: <End>",
                     "Build <token>: Failed (<Outcome>)",
                     "Build <token>: Returned, <stacks> stack(s) put back",
                     "Build: vanilla (<Reason>)",
                     "Build: placement suppressed, inventory does not cover the piece",
                 })
        {
            Assert.Contains(line, preamble, StringComparison.Ordinal);
        }
    }

    // GW-U-1662 (PB3, PB4, PB6, AC-10): Solo is a numbered list from 1 with an Expected line per step.
    [Fact]
    public void TheSoloStepsAreNumberedFromOneWithAnExpectedLinePerStep()
    {
        string solo = Subsection("### Solo", "### Host plus one friend");

        int[] numbers = Regex.Matches(solo, @"(?m)^(\d+)\. ").Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)).ToArray();
        Assert.Equal(Enumerable.Range(1, numbers.Length), numbers);
        Assert.Equal(numbers.Length, Regex.Matches(solo, "Expected:").Count);
    }

    // GW-U-1662 (PB4, AC-10): Solo covers the nine steps in order, ending on the idle step.
    [Fact]
    public void TheSoloStepsCoverTheNineCasesInOrder()
    {
        string solo = Subsection("### Solo", "### Host plus one friend");

        AssertInOrder(
            solo,
            "route LocalOwner",
            "Build <token>: Placed",
            "one short",
            "second Workbench",
            "outside every base",
            "ExcludeVehicles",
            "KeepChestsCraftable",
            "PlacementRefused",
            "CraftFromBase",
            "30 seconds");
    }

    // GW-U-1663 (PB3, PB5, PB6, AC-10): Host plus one friend is a numbered list from 1 with an Expected line per step.
    [Fact]
    public void TheHostAndFriendStepsAreNumberedFromOneWithAnExpectedLinePerStep()
    {
        string host = Subsection("### Host plus one friend", null);

        int[] numbers = Regex.Matches(host, @"(?m)^(\d+)\. ").Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)).ToArray();
        Assert.Equal(Enumerable.Range(1, numbers.Length), numbers);
        Assert.Equal(numbers.Length, Regex.Matches(host, "Expected:").Count);
    }

    // GW-U-1663 (PB5, AC-10): the friend's grant, the open chest, the race with the 20000 ms timeout and the restore, in order, then the idle step.
    [Fact]
    public void TheHostAndFriendStepsCoverTheGrantTheOpenChestAndTheRaceInOrder()
    {
        string host = Subsection("### Host plus one friend", null);

        AssertInOrder(
            host,
            "route SendRequest",
            "open response granted consumed, chest UI not shown",
            "Build <token>: Placed",
            "ReserveTimeoutMs",
            "20000",
            "Materials moved, try again",
            "Build <token>: Failed (",
            "1500",
            "30 seconds");
    }

    // GW-U-1664 (PB7, AC-10): the closing note names the unit-tested paths: Returned, placement suppressed, pre-flight, abandonment, timeout and exceptions.
    [Fact]
    public void TheClosingNoteNamesTheUnitTestedPaths()
    {
        string section = Section();

        foreach (string path in new[] { "Returned", "placement suppressed", "pre-flight", "abandonment", "timeout", "exceptions" })
        {
            Assert.Contains(path, section, StringComparison.Ordinal);
        }

        Assert.Contains("unit tests", section, StringComparison.Ordinal);
    }

    // GW-U-1664 (PB8, AC-10): the wording follows the file: no emoji, setting names in backticks, paths with backslashes, and the required literals.
    [Fact]
    public void TheWordingFollowsTheFileAndCarriesTheRequiredLiterals()
    {
        string section = Section();

        Assert.DoesNotMatch(@"[☀-➿]|[\uD83C-\uDBFF]", section);
        Assert.Contains("`CraftFromBase`", section, StringComparison.Ordinal);
        Assert.Contains("`ReserveTimeoutMs`", section, StringComparison.Ordinal);
        Assert.DoesNotContain("BepInEx/LogOutput.log", section, StringComparison.Ordinal);
        foreach (string literal in new[]
                 {
                     "Materials moved, try again", "CraftFromBase", "Build <token>: Placed", "[craft]", "route SendRequest", "route LocalOwner",
                     "PlacementRefused", "ReserveTimeoutMs",
                 })
        {
            Assert.Contains(literal, section, StringComparison.Ordinal);
        }
    }

    private static string Section()
    {
        string text = RepoRoot.Normalise(RepoRoot.ReadText(PlaytestFile));
        int start = text.IndexOf(Heading, StringComparison.Ordinal);
        Assert.True(start >= 0, "docs/PLAYTEST.md has no Build from base section.");

        // Edited by integration (S-E003-005 + S-E003-006): the section ends at the next level-2 heading (Requirement display follows it).
        Match next = Regex.Match(text.Substring(start + Heading.Length), @"(?m)^## .*$");
        return next.Success ? text.Substring(start, Heading.Length + next.Index) : text.Substring(start);
    }

    private static string Subsection(string start, string? end)
    {
        string section = Section();
        int begin = section.IndexOf(start, StringComparison.Ordinal);
        Assert.True(begin >= 0, "The Build from base section has no " + start + " subsection.");
        int stop = end is null ? -1 : section.IndexOf(end, begin + start.Length, StringComparison.Ordinal);
        return stop < 0 ? section.Substring(begin) : section.Substring(begin, stop - begin);
    }

    private static void AssertInOrder(string text, params string[] literals)
    {
        int cursor = 0;
        foreach (string literal in literals)
        {
            int found = text.IndexOf(literal, cursor, StringComparison.Ordinal);
            Assert.True(found >= 0, "Missing, or out of order: " + literal);
            cursor = found + literal.Length;
        }
    }
}
