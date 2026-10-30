namespace BaseStorage.Core.Tests.PluginHelper;

using System.Collections.Generic;
using BaseStorage.Core.Persistence;
using BaseStorage.Plugin.Favourites;

/// <summary>
/// AC-8 (contract 3.2 Current cache and the 3.3 relog table): a relog reads the stored value again, and the set survives a
/// save and load round trip of the custom data dictionary.
/// </summary>
public sealed class ProfileFavouritesCacheTests
{
    // GW-U-703
    [Fact]
    public void SeededValueDecodesOnceAndTheSameInstanceIsReturnedWhileTheRawValueIsUnchanged()
    {
        var g = new ProfileFavourites(new Dictionary<string, string>
        {
            [FavouriteFixtures.OtherKey] = FavouriteFixtures.OtherValue,
            [PersistenceKeys.Favourites] = "F1;7,0;0,1;2,1",
        });

        Assert.Equal(
            new[] { new InventorySlot(7, 0), new InventorySlot(0, 1), new InventorySlot(2, 1) },
            g.Current.Slots);
        Assert.True(ReferenceEquals(g.Current, g.Current));
    }

    // GW-U-703
    [Fact]
    public void ANewStoredValueAfterARelogIsDecodedAgain()
    {
        var g = new ProfileFavourites(new Dictionary<string, string>
        {
            [FavouriteFixtures.OtherKey] = FavouriteFixtures.OtherValue,
            [PersistenceKeys.Favourites] = "F1;7,0;0,1;2,1",
        });
        Assert.Equal(3, g.Current.Count);

        g.CustomData[PersistenceKeys.Favourites] = "F1;5,3";

        Assert.Equal(new[] { new InventorySlot(5, 3) }, g.Current.Slots);
        Assert.True(g.IsFavourite(new InventorySlot(5, 3)));
        Assert.False(g.IsFavourite(new InventorySlot(7, 0)));
    }

    // GW-U-703
    [Fact]
    public void TheSetSurvivesACopyOfTheCustomDataDictionary()
    {
        var customA = new Dictionary<string, string> { [FavouriteFixtures.OtherKey] = FavouriteFixtures.OtherValue };
        var a = new ProfileFavourites(customA);
        a.Toggle(new InventorySlot(3, 2));
        a.Toggle(new InventorySlot(7, 0));

        // Player.Save and Player.Load copy every m_customData entry into the profile and back (contract V2).
        var copy = new Dictionary<string, string>(customA);
        var b = new ProfileFavourites(copy);

        Assert.NotSame(customA, copy);
        Assert.Equal(a.Current, b.Current);
        Assert.True(b.IsFavourite(new InventorySlot(3, 2)));
        Assert.True(b.IsFavourite(new InventorySlot(7, 0)));
    }
}
