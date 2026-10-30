namespace BaseStorage.Core.Tests.PluginHelper;

using System;
using System.Collections.Generic;
using System.Globalization;
using BaseStorage.Core.Persistence;
using BaseStorage.Plugin.Favourites;

/// <summary>
/// AC-7 and the spec NFR (contract 3.3 extreme row): a set of all 65,536 slots, and culture independence of the stored literals.
/// </summary>
public sealed class ProfileFavouritesScaleTests
{
    /// <summary>The cultures of the replay. The value "invariant" selects the invariant culture.</summary>
    public static TheoryData<string> Cultures => new()
    {
        "invariant",
        "en-US",
        "de-DE",
        "tr-TR",
        "ar-SA",
    };

    // GW-U-702
    [Fact]
    public void AllSlotsSeedDecodesAndRemovingOneLeavesAllTheRest()
    {
        var all = new List<InventorySlot>(65536);
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                all.Add(new InventorySlot(x, y));
            }
        }

        string seeded = FavouritesCodec.Encode(new FavouriteSlots(all));
        RecordingDictionary d = FavouriteFixtures.Seed(seeded);
        var f = new ProfileFavourites(d);

        Assert.Equal(65536, f.Current.Count);
        Assert.Equal(FavouriteToggleOutcome.Removed, f.Toggle(new InventorySlot(128, 128)));

        FavouriteSlots? stored = FavouritesCodec.Decode(d[PersistenceKeys.Favourites]);
        Assert.NotNull(stored);
        Assert.Equal(65535, stored!.Count);
        Assert.False(stored.Contains(new InventorySlot(128, 128)));
    }

    // GW-U-702
    [Theory]
    [MemberData(nameof(Cultures))]
    public void TheSequenceStoresTheSameLiteralsUnderEveryCulture(string cultureName)
    {
        CultureInfo savedCulture = CultureInfo.CurrentCulture;
        CultureInfo savedUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo culture = cultureName == "invariant"
                ? CultureInfo.InvariantCulture
                : CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            RecordingDictionary d = FavouriteFixtures.Seed(null);
            var f = new ProfileFavourites(d);

            f.Toggle(new InventorySlot(3, 2));
            Assert.Equal("F1;3,2", d[PersistenceKeys.Favourites]);

            f.Toggle(new InventorySlot(7, 0));
            Assert.Equal("F1;7,0;3,2", d[PersistenceKeys.Favourites]);

            f.Toggle(new InventorySlot(3, 2));
            Assert.Equal("F1;7,0", d[PersistenceKeys.Favourites]);

            Assert.Equal(FavouriteToggleOutcome.Removed, f.Toggle(new InventorySlot(7, 0)));
            Assert.False(d.ContainsKey(PersistenceKeys.Favourites));
        }
        finally
        {
            CultureInfo.CurrentCulture = savedCulture;
            CultureInfo.CurrentUICulture = savedUiCulture;
        }
    }
}
