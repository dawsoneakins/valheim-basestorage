namespace BaseStorage.Core.Tests.Feeding;

using System.Linq;
using BaseStorage.Core.Feeding;

/// <summary>The stored choice of what a station is kept fed with.</summary>
public sealed class FeedSelectionTests
{
    [Fact]
    public void ASelectionSurvivesBeingStored()
    {
        FeedSelection selection = FeedSelection.Empty.With("CopperOre", 50).With("Coal", 0);

        FeedSelection read = FeedSelection.Decode(selection.Encode());

        Assert.Equal("CopperOre=50|Coal=0", selection.Encode());
        Assert.Equal(new[] { "CopperOre", "Coal" }, read.Entries.Select(entry => entry.PrefabName));
        Assert.Equal(50, read.Find("CopperOre")!.Keep);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("|||")]
    [InlineData("=5")]
    [InlineData("CopperOre")]
    [InlineData("CopperOre=")]
    [InlineData("CopperOre=-1")]
    [InlineData("CopperOre=lots")]
    [InlineData("CopperOre=99999999999999")]
    public void DamagedTextReadsAsNothing(string? raw) =>
        Assert.Same(FeedSelection.Empty, FeedSelection.Decode(raw));

    [Fact]
    public void ADamagedEntryDoesNotLoseTheOthers()
    {
        FeedSelection read = FeedSelection.Decode("Coal=10|broken|TinOre=2");

        Assert.Equal(new[] { "Coal", "TinOre" }, read.Entries.Select(entry => entry.PrefabName));
    }

    [Fact]
    public void TheFirstEntryOfARepeatedItemWins() =>
        Assert.Equal(1, FeedSelection.Decode("Coal=1|Coal=2").Find("Coal")!.Keep);

    [Fact]
    public void ChangingTheKeepAmountKeepsTheOrder()
    {
        FeedSelection selection = FeedSelection.Empty.With("A", 1).With("B", 2).With("A", 9);

        Assert.Equal("A=9|B=2", selection.Encode());
    }

    [Fact]
    public void RemovingTheLastItemLeavesEmpty()
    {
        Assert.Same(FeedSelection.Empty, FeedSelection.Empty.With("A", 1).Without("A"));
        Assert.Equal(string.Empty, FeedSelection.Empty.Encode());
    }

    [Fact]
    public void ANegativeKeepAmountReadsAsZero() =>
        Assert.Equal(0, FeedSelection.Empty.With("A", -5).Find("A")!.Keep);

    [Fact]
    public void AnItemNotChosenIsNotFound() =>
        Assert.Null(FeedSelection.Empty.With("A", 0).Find("B"));
}
