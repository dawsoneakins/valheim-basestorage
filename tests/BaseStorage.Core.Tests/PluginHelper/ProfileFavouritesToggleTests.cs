namespace BaseStorage.Core.Tests.PluginHelper;

using System.Linq;
using BaseStorage.Core.Persistence;
using BaseStorage.Plugin.Favourites;

/// <summary>
/// AC-1 and AC-7 (contract 3.2 and 3.3): the toggle sequence, and each toggle writes the F1 form of the set under the favourites
/// key. Expected values are the contract literals, never FavouritesCodec output.
/// </summary>
public sealed class ProfileFavouritesToggleTests
{
    // GW-U-700
    [Fact]
    public void ToggleFlipsMembershipAlongTheContractSequence()
    {
        RecordingDictionary d = FavouriteFixtures.Seed(null);
        var f = new ProfileFavourites(d);

        Assert.Equal(FavouriteSlots.Empty, f.Current);

        Assert.Equal(FavouriteToggleOutcome.Added, f.Toggle(new InventorySlot(3, 2)));
        AssertSlots(f, (3, 2));

        Assert.Equal(FavouriteToggleOutcome.Added, f.Toggle(new InventorySlot(7, 0)));
        AssertSlots(f, (7, 0), (3, 2));
        Assert.True(f.IsFavourite(new InventorySlot(7, 0)));
        Assert.True(f.IsFavourite(new InventorySlot(3, 2)));
        Assert.False(f.IsFavourite(new InventorySlot(0, 0)));

        Assert.Equal(FavouriteToggleOutcome.Removed, f.Toggle(new InventorySlot(3, 2)));
        AssertSlots(f, (7, 0));
        Assert.False(f.IsFavourite(new InventorySlot(3, 2)));

        Assert.Equal(FavouriteToggleOutcome.Removed, f.Toggle(new InventorySlot(7, 0)));
        AssertSlots(f);
    }

    // GW-U-700
    [Fact]
    public void TheAdapterWritesOnlyToTheDictionaryItWasGiven()
    {
        RecordingDictionary d = FavouriteFixtures.Seed(null);
        var f = new ProfileFavourites(d);

        Assert.Same(d, f.CustomData);
        f.Toggle(new InventorySlot(3, 2));

        Assert.True(ReferenceEquals(f.CustomData, d));
        Assert.Equal(FavouriteFixtures.OtherValue, d[FavouriteFixtures.OtherKey]);
    }

    // GW-U-701
    [Fact]
    public void EachToggleStoresTheExpectedF1LiteralAndRemovesTheKeyWhenEmpty()
    {
        RecordingDictionary d = FavouriteFixtures.Seed(null);
        var f = new ProfileFavourites(d);

        f.Toggle(new InventorySlot(3, 2));
        Assert.Equal("F1;3,2", d[PersistenceKeys.Favourites]);

        f.Toggle(new InventorySlot(7, 0));
        Assert.Equal("F1;7,0;3,2", d[PersistenceKeys.Favourites]);

        f.Toggle(new InventorySlot(3, 2));
        Assert.Equal("F1;7,0", d[PersistenceKeys.Favourites]);

        f.Toggle(new InventorySlot(7, 0));
        Assert.False(d.ContainsKey(PersistenceKeys.Favourites));
        Assert.DoesNotContain("F1", d.Writes.Select(write => write.Value));
        Assert.Equal(FavouriteFixtures.OtherValue, d[FavouriteFixtures.OtherKey]);
    }

    // GW-U-701
    [Fact]
    public void TheValueJustWrittenDecodesToTheCurrentSetAndIsNotDecodedAgain()
    {
        RecordingDictionary d = FavouriteFixtures.Seed(null);
        var f = new ProfileFavourites(d);

        f.Toggle(new InventorySlot(3, 2));
        Assert.Equal(f.Current, FavouritesCodec.Decode(d[PersistenceKeys.Favourites]));
        Assert.True(ReferenceEquals(f.Current, f.Current));

        f.Toggle(new InventorySlot(7, 0));
        Assert.Equal(f.Current, FavouritesCodec.Decode(d[PersistenceKeys.Favourites]));
        Assert.True(ReferenceEquals(f.Current, f.Current));

        f.Toggle(new InventorySlot(3, 2));
        Assert.Equal(f.Current, FavouritesCodec.Decode(d[PersistenceKeys.Favourites]));
        Assert.True(ReferenceEquals(f.Current, f.Current));
    }

    // GW-U-701
    [Fact]
    public void AnEmptyF1ValueThenTwoTogglesOfTheSameSlotAddsThenRemovesIt()
    {
        RecordingDictionary d = FavouriteFixtures.Seed("F1");
        var f = new ProfileFavourites(d);

        Assert.Equal(FavouriteToggleOutcome.Added, f.Toggle(new InventorySlot(0, 0)));
        Assert.Equal("F1;0,0", d[PersistenceKeys.Favourites]);

        Assert.Equal(FavouriteToggleOutcome.Removed, f.Toggle(new InventorySlot(0, 0)));
        Assert.False(d.ContainsKey(PersistenceKeys.Favourites));
    }

    private static void AssertSlots(ProfileFavourites f, params (int X, int Y)[] expected)
    {
        InventorySlot[] literal = expected.Select(slot => new InventorySlot(slot.X, slot.Y)).ToArray();
        Assert.Equal(literal, f.Current.Slots);
    }
}
