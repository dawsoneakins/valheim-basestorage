namespace BaseStorage.Core.Tests.Persistence;

using BaseStorage.Core.Persistence;

/// <summary>The favourite item types stored with a character.</summary>
public sealed class FavouriteItemsTests
{
    [Fact]
    public void ASetSurvivesBeingStored()
    {
        FavouriteItems read = FavouriteItems.Decode(FavouriteItems.Empty.With("$item_wood").With("$item_coal").Encode());

        Assert.Equal(2, read.Count);
        Assert.True(read.Contains("$item_wood"));
        Assert.True(read.Contains("$item_coal"));
    }

    [Fact]
    public void TheSameSetAlwaysGivesTheSameText() =>
        Assert.Equal(FavouriteItems.Empty.With("b").With("a").Encode(), FavouriteItems.Empty.With("a").With("b").Encode());

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("|||")]
    public void NothingStoredReadsAsEmpty(string? raw) =>
        Assert.Same(FavouriteItems.Empty, FavouriteItems.Decode(raw));

    [Fact]
    public void EmptyPartsAreSkipped() =>
        Assert.Equal(2, FavouriteItems.Decode("a||b|").Count);

    [Fact]
    public void AddingTwiceChangesNothing()
    {
        FavouriteItems once = FavouriteItems.Empty.With("a");

        Assert.Same(once, once.With("a"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a|b")]
    public void ANameThatCouldNotBeReadBackIsNotAdded(string name) =>
        Assert.Same(FavouriteItems.Empty, FavouriteItems.Empty.With(name));

    [Fact]
    public void RemovingTheLastItemLeavesEmpty() =>
        Assert.Same(FavouriteItems.Empty, FavouriteItems.Empty.With("a").Without("a"));

    [Fact]
    public void RemovingAnItemNotInTheSetChangesNothing()
    {
        FavouriteItems set = FavouriteItems.Empty.With("a");

        Assert.Same(set, set.Without("b"));
    }

    [Fact]
    public void AddingLeavesTheOriginalAlone()
    {
        FavouriteItems set = FavouriteItems.Empty.With("a");

        set.With("b");

        Assert.False(set.Contains("b"));
    }
}
