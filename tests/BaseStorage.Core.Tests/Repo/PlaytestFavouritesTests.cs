namespace BaseStorage.Core.Tests.Repo;

using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-14 (contract section 9): docs/PLAYTEST.md has a "Favourites" section with at least ten numbered steps, the required literals,
/// and the relog step before the uninstall step. The game is not launched by the pipeline, so the steps are checked as text.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PlaytestFavouritesTests
{
    private const string PlaytestFile = "docs/PLAYTEST.md";

    // GW-U-720
    [Fact]
    public void TheFavouritesSectionExistsOnceWithTenNumberedStepsAndTheExistingSectionsRemain()
    {
        string text = RepoRoot.ReadText(PlaytestFile);
        Assert.Single(Regex.Matches(text, @"^## Favourites[ \t]*$", RegexOptions.Multiline));

        string section = FavouritesSection(text);
        int steps = Regex.Matches(section, @"^\d+\. ", RegexOptions.Multiline).Count;
        Assert.True(steps >= 10, $"The Favourites section has {steps} numbered steps; at least 10 are required.");

        Assert.Contains("## Setup", text);
        Assert.Contains("## Load check", text);
        Assert.Contains("## Category file", text);
    }

    // GW-U-720
    [Fact]
    public void TheFavouritesSectionNamesTheContractLiterals()
    {
        string section = FavouritesSection(RepoRoot.ReadText(PlaytestFile));

        Assert.Contains("Favourite: ", section);
        Assert.Contains("No longer favourite: ", section);
        Assert.Contains("Alt", section);
        Assert.Contains("basestorage.fav", section);
        Assert.Contains("BaseStorage.inventory.slot.favourite-marker", section);
        Assert.Contains("1280x720", section);
        Assert.Contains("Dons-QOL.dll", section);
    }

    // GW-U-720
    [Fact]
    public void TheRelogStepComesBeforeTheUninstallStep()
    {
        string section = FavouritesSection(RepoRoot.ReadText(PlaytestFile));

        int relog = section.IndexOf("log out", System.StringComparison.Ordinal);
        int uninstall = section.IndexOf("Remove", System.StringComparison.Ordinal);
        Assert.True(relog >= 0, "The Favourites section has no 'log out' step.");
        Assert.True(uninstall > relog, "The uninstall step must follow the relog step.");
    }

    private static string FavouritesSection(string text)
    {
        Match heading = Regex.Match(text, @"^## Favourites[ \t]*$", RegexOptions.Multiline);
        Assert.True(heading.Success, "docs/PLAYTEST.md has no '## Favourites' section.");

        int start = heading.Index + heading.Length;
        Match next = Regex.Match(text.Substring(start), @"^## ", RegexOptions.Multiline);
        return next.Success ? text.Substring(start, next.Index) : text.Substring(start);
    }
}
