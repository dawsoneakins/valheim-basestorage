namespace BaseStorage.Core.Tests.Building;

using BaseStorage.Core.Availability;
using BaseStorage.Plugin.Crafting;

/// <summary>GW-U-1601 (AC-3; contract 3.3): a piece's requirement lines through the reused craft builder.</summary>
public sealed class BuildRequirementLinesTests
{
    private static RecipeLine Line(string? prefab, int amount, bool upgrader = false) => new RecipeLine(prefab, amount, upgrader);

    private static RecipeLine[] LinesFor(string piece) => piece switch
    {
        "wooden chest" => new[] { Line("Wood", 10) },
        "portal" => new[] { Line("FineWood", 20), Line("GreydwarfEye", 10), Line("SurtlingCore", 2) },
        "torch" => new[] { Line("Wood", 2), Line("Resin", 2) },
        "null and zero" => new[] { Line(null, 3), Line("Stone", 0), Line("Stone", 4) },
        _ => throw new ArgumentOutOfRangeException(nameof(piece)),
    };

    private static string[] ExpectedFor(string piece) => piece switch
    {
        "wooden chest" => new[] { "Wood 10" },
        "portal" => new[] { "FineWood 20", "GreydwarfEye 10", "SurtlingCore 2" },
        "torch" => new[] { "Wood 2", "Resin 2" },
        "null and zero" => new[] { "Stone 4" },
        _ => throw new ArgumentOutOfRangeException(nameof(piece)),
    };

    // GW-U-1601 (3.3 table rows 1-4): each piece's lines build to the contracted requirement list at multiplier 1.
    [Theory]
    [InlineData("wooden chest")]
    [InlineData("portal")]
    [InlineData("torch")]
    [InlineData("null and zero")]
    public void EachPieceBuildsToItsRequirementLines(string piece)
    {
        IReadOnlyList<CraftRequirement>? result = CraftRequirementLines.Build(LinesFor(piece), 1);

        Assert.NotNull(result);
        Assert.Equal(ExpectedFor(piece), result!.Select(requirement => requirement.PrefabName + " " + requirement.Amount).ToArray());
    }
}
