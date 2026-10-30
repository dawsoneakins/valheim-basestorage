namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using BaseStorage.Core.Categories.AutoAssign;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-3 (contract 2.1, 4.3 step 4, 5.2 row 103): a mixed chest with no majority stays a source only.</summary>
public sealed class AutoAssignSourceOnlyTests
{
    private static readonly string[] AllCategoryNames =
    {
        "Metals & Ores", "Wood", "Stone", "Cooked Food", "Trophies", "Gear", "Misc", "Ammo",
    };

    // GW-U-508
    [Fact]
    public void MixedChest103IsSourceOnlyAndNeverPooledOrCategorised()
    {
        AutoAssignResult r = ResolveB();

        AssertChest("chest 103", AutoAssignOutcome.SourceOnly, null, At(r, 103));
        Assert.DoesNotContain(103u, ContainerIds(r.OverflowChests));
        foreach (string name in AllCategoryNames)
        {
            Assert.DoesNotContain(103u, ContainerIds(r.GetCategoryChests(name)));
        }
    }

    // GW-U-508
    [Fact]
    public void TieAtTheMaximumThatDoesNotReachTheThresholdIsSourceOnly()
    {
        AutoAssignResult r = ResolveCodes(60, "St,W");

        AssertChest("threshold 60, St,W", AutoAssignOutcome.SourceOnly, null, At(r, 201));
    }

    // GW-U-508
    [Fact]
    public void FiveWayTieAtThresholdFiftyIsSourceOnly()
    {
        AutoAssignResult r = ResolveCodes(50, "M,W,St,F,T");

        AssertChest("threshold 50, M,W,St,F,T", AutoAssignOutcome.SourceOnly, null, At(r, 201));
    }

    // GW-U-509
    [Fact]
    public void SourceOnlyChestsAreNotInTheOverflowPoolAt70()
    {
        AutoAssignResult r = ResolveB(70);

        AssertChest("103 at 70", AutoAssignOutcome.SourceOnly, null, At(r, 103));
        AssertChest("104 at 70", AutoAssignOutcome.SourceOnly, null, At(r, 104));
        AssertChest("110 at 70", AutoAssignOutcome.SourceOnly, null, At(r, 110));
        Assert.Equal(new uint[] { 105, 108, 109 }, ContainerIds(r.OverflowChests));
    }

    // GW-U-509
    [Fact]
    public void PoolAt70DrainedWithWoodReturnsOnlyTheEmptyChestsThenNull()
    {
        AutoAssignResult r = ResolveB(70);

        Assert.Equal(new uint?[] { 105, 108, 109, null }, ClaimIds(r.CreateOverflowPool(), "Wood", "Wood", "Wood", "Wood"));
    }

    // GW-U-509
    [Fact]
    public void PoolAt70DrainedWithGearReturnsSnapshotOrderBecauseChest110IsNotAGearChest()
    {
        AutoAssignResult r = ResolveB(70);

        Assert.Equal(new uint?[] { 105, 108, 109, null }, ClaimIds(r.CreateOverflowPool(), "Gear", "Gear", "Gear", "Gear"));
    }
}
