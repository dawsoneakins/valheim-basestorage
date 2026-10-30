namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;

/// <summary>GW-U-1158 to GW-U-1161 (AC-5; contract 9 P1-P8): the docs/PLAYTEST.md "Craft from base" section.</summary>
public sealed class PlaytestCraftFromBaseTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";
    private const string Heading = "## Craft from base (AC-2.4, AC-3.1, AC-3.4)";
    private const string VersionHandshake = "## Version handshake (AC-6.1, AC-6.3)";
    private const string BuildHeading = "## Build from base (AC-2.4, AC-3.1, AC-3.4)";

    // GW-U-1158 (P1): exactly one heading, after Version handshake, and it is followed first by Build from base.
    // Edited by S-E003-006, ADR-GWIN-132: the last level-2 section is now Build from base (build-from-base PB1).
    [Fact]
    public void TheHeadingIsOnceAfterVersionHandshakeAndIsFollowedOnlyByBuildFromBase()
    {
        string text = RepoRoot.Normalise(RepoRoot.ReadText(PlaytestFile));
        MatchCollection headings = Regex.Matches(text, @"^## Craft from base \(AC-2\.4, AC-3\.1, AC-3\.4\)[ \t]*$", RegexOptions.Multiline);
        int handshake = text.IndexOf(VersionHandshake, StringComparison.Ordinal);

        Assert.Single(headings);
        Assert.True(handshake >= 0 && handshake < headings[0].Index, "The Craft from base section must come after Version handshake.");
        // Edited by integration (S-E003-005 + S-E003-006): Build from base directly follows Craft from base; Requirement display and later sections follow it.
        string afterCraft = text.Substring(headings[0].Index + headings[0].Length);
        Assert.Equal(BuildHeading, Regex.Matches(afterCraft, @"(?m)^## .*$").Select(match => match.Value.TrimEnd()).First());
    }

    // GW-U-1158 (P2): the preamble names the setting, quotes the notice, points to the debug log and quotes K1-K5 with placeholders.
    [Fact]
    public void ThePreambleNamesTheSettingTheNoticeTheLogAndTheLines()
    {
        string preamble = Section().Split(new[] { "### Solo" }, StringSplitOptions.None)[0];

        Assert.Contains("`CraftFromBase`", preamble, StringComparison.Ordinal);
        Assert.Contains("[General]", preamble, StringComparison.Ordinal);
        Assert.Contains("Materials moved, try again", preamble, StringComparison.Ordinal);
        Assert.Contains("## Debug logging", preamble, StringComparison.Ordinal);
        Assert.Contains(@"<ValheimDir>\BepInEx\LogOutput.log", preamble, StringComparison.Ordinal);
        Assert.Contains("Craft <token>: started, <takes> take(s) from <chests> chest(s)", preamble, StringComparison.Ordinal);
        Assert.Contains("Craft <token>: <End>", preamble, StringComparison.Ordinal);
        Assert.Contains("Craft <token>: Failed (<Outcome>)", preamble, StringComparison.Ordinal);
        Assert.Contains("Craft <token>: Returned, <stacks> stack(s) put back", preamble, StringComparison.Ordinal);
        Assert.Contains("Craft: vanilla (<Reason>)", preamble, StringComparison.Ordinal);
        Assert.Contains("Hammer", preamble, StringComparison.Ordinal);
    }

    // GW-U-1159 (P3, P4, P6): Solo is a numbered list from 1 with an Expected line per step, covering the steps in order.
    [Fact]
    public void TheSoloStepsAreNumberedWithExpectedLinesAndCoverTheStepsInOrder()
    {
        string solo = Subsection("### Solo", "### Host plus one friend");

        int[] numbers = Regex.Matches(solo, @"(?m)^(\d+)\. ").Select(match => int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        Assert.Equal(Enumerable.Range(1, numbers.Length), numbers);
        Assert.Equal(numbers.Length, Regex.Matches(solo, "Expected:").Count);
        AssertInOrder(solo, "route LocalOwner", "Craft <token>: Created", "outside", "CraftFromBase", "Hammer", "30 seconds");
    }

    // GW-U-1160 (P3, P5, P6, P7, P8): Host plus one friend covers the friend's craft, the race, the swapped roles and the idle step, in order.
    [Fact]
    public void TheHostAndFriendStepsCoverTheRaceInOrder()
    {
        string host = Subsection("### Host plus one friend", null);

        int[] numbers = Regex.Matches(host, @"(?m)^(\d+)\. ").Select(match => int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        Assert.Equal(Enumerable.Range(1, numbers.Length), numbers);
        AssertInOrder(host, "route SendRequest", "open response granted consumed, chest UI not shown", "Craft <token>: Created", "ReserveTimeoutMs", "20000", "Materials moved, try again", "Craft <token>: Failed (", "1500", "30 seconds");
        Assert.DoesNotMatch(@"[☀-➿]|[\uD83C-\uDBFF]", Section());
    }

    // GW-U-1160 (P8): the literals the section must carry appear, and the section names setting keys in backticks.
    [Fact]
    public void TheRequiredLiteralsAppearInTheSection()
    {
        string section = Section();

        foreach (string literal in new[] { "Materials moved, try again", "CraftFromBase", "Craft <token>: Created", "[craft]", "route SendRequest", "route LocalOwner", "ReserveTimeoutMs" })
        {
            Assert.Contains(literal, section, StringComparison.Ordinal);
        }

        Assert.Contains("`ReserveTimeoutMs`", section, StringComparison.Ordinal);
    }

    // GW-U-1161 (P1, Craft from base is the last section): a level-2 heading follows Version handshake only when it is Craft from base.
    [Fact]
    public void CraftFromBaseIsTheLastSectionAfterVersionHandshake()
    {
        string text = RepoRoot.Normalise(RepoRoot.ReadText(PlaytestFile));
        int handshake = text.IndexOf(VersionHandshake, StringComparison.Ordinal);
        Assert.True(handshake >= 0, "docs/PLAYTEST.md has no Version handshake section.");

        string afterHandshake = text.Substring(handshake + VersionHandshake.Length);
        // Edited by S-E003-006, ADR-GWIN-132 (GW-U-1161): after Version handshake the level-2 sections are Craft from base, then Build from base.
        // Edited by integration (S-E003-005 + S-E003-006): only the first two are pinned; Requirement display and later sections follow Build from base.
        string[] later = Regex.Matches(afterHandshake, @"(?m)^## .*$").Select(match => match.Value.TrimEnd()).Take(2).ToArray();
        Assert.Equal(new[] { Heading, BuildHeading }, later);
    }

    // GW-U-1665 (PB9, Craft from base amendments): the craft preamble's last sentence points to the Build from base section.
    [Fact]
    public void TheCraftPreambleEndsWithTheBuildPiecesSentence()
    {
        string preamble = Section().Split(new[] { "### Solo" }, StringSplitOptions.None)[0].TrimEnd();

        Assert.EndsWith("Build pieces (the Hammer) are covered by the section Build from base below.", preamble, StringComparison.Ordinal);
    }

    // GW-U-1665 (PB9, Craft from base amendments): Solo step 5 selects a piece whose Wood is only in a base chest, and its Expected line points to Build from base.
    [Fact]
    public void SoloStepFiveSelectsAPieceWhoseWoodIsOnlyInABaseChest()
    {
        string solo = Subsection("### Solo", "### Host plus one friend");

        Assert.Contains(
            "5. Equip the Hammer and select a piece whose Wood is only in a base chest.\nExpected: the piece is buildable; placing it is covered by the section Build from base below.",
            solo,
            StringComparison.Ordinal);
    }

    // GW-U-1666 (section order): Craft from base is followed first by Build from base.
    // Edited by integration (S-E003-005 + S-E003-006): Requirement display (S-E003-005) follows Build from base, so Build from base is no longer the last section.
    [Fact]
    public void BuildFromBaseIsTheFirstSectionAfterCraftFromBase()
    {
        string text = RepoRoot.Normalise(RepoRoot.ReadText(PlaytestFile));
        int craft = text.IndexOf(Heading, StringComparison.Ordinal);
        Assert.True(craft >= 0, "docs/PLAYTEST.md has no Craft from base section.");

        string[] afterCraft = Regex.Matches(text.Substring(craft + Heading.Length), @"(?m)^## .*$").Select(match => match.Value.TrimEnd()).ToArray();
        Assert.Equal(BuildHeading, afterCraft.First());
    }

    // GW-U-1666 (section order): the Build from base heading comes directly after the Craft from base heading.
    [Fact]
    public void BuildFromBaseFollowsCraftFromBaseWithNoSectionBetween()
    {
        string text = RepoRoot.Normalise(RepoRoot.ReadText(PlaytestFile));

        Assert.Contains(Heading + "\n", text, StringComparison.Ordinal);
        string[] levelTwo = Regex.Matches(text, @"(?m)^## .*$").Select(match => match.Value.TrimEnd()).ToArray();
        Assert.Equal(BuildHeading, levelTwo[Array.IndexOf(levelTwo, Heading) + 1]);
        Assert.Single(Regex.Matches(text, @"(?m)^## Build from base"));
    }

    private static string Section()
    {
        string text = RepoRoot.Normalise(RepoRoot.ReadText(PlaytestFile));
        int start = text.IndexOf(Heading, StringComparison.Ordinal);
        Assert.True(start >= 0, "docs/PLAYTEST.md has no Craft from base section.");

        // Edited by S-E003-005, ADR-GWIN-127: the section ends at the next level-2 heading, so the Requirement display steps are not counted here.
        string after = text.Substring(start);
        Match next = Regex.Match(after.Substring(Heading.Length), @"(?m)^## .*$");
        return next.Success ? after.Substring(0, Heading.Length + next.Index) : after;
    }

    private static string Subsection(string start, string? end)
    {
        string section = Section();
        int begin = section.IndexOf(start, StringComparison.Ordinal);
        Assert.True(begin >= 0, "The Craft from base section has no " + start + " subsection.");
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
