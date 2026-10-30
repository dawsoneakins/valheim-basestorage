namespace BaseStorage.Core.Tests.PluginHelper;

using System.Collections.Generic;
using BaseStorage.Core.Persistence;
using BaseStorage.Plugin.Favourites;

/// <summary>
/// AC-9 and AC-10 (contract 3.3 malformed table, FV4): a missing, empty, malformed or foreign value reads as no favourites,
/// reading never writes, and the next toggle replaces the value. A null row means the key is absent.
/// </summary>
public sealed class ProfileFavouritesMalformedTests
{
    /// <summary>The stored values of the contract table. Null means the key is absent.</summary>
    public static TheoryData<string?> MalformedValues => new()
    {
        null,
        string.Empty,
        "F1;",
        "F1;0,1;7,0",
        "F1;3,2;3,2",
        "F1;256,0",
        "F2;3,2",
        " F1",
        "c:Wood",
        "L1;1923487561234;36300000000;Erik",
        "\uD800",
    };

    // GW-U-704
    [Theory]
    [MemberData(nameof(MalformedValues))]
    public void AMalformedOrForeignValueReadsAsNoFavouritesAndIsNotWrittenOnRead(string? value)
    {
        RecordingDictionary d = FavouriteFixtures.Seed(value);
        Dictionary<string, string> before = new(FavouriteFixtures.Sorted(d));
        var f = new ProfileFavourites(d);

        FavouriteSlots current = f.Current;

        Assert.Equal(FavouriteSlots.Empty, current);
        Assert.Equal(0, d.WriteCount);
        Assert.Equal(FavouriteFixtures.Sorted(before), FavouriteFixtures.Sorted(d));
    }

    // GW-U-704
    [Theory]
    [MemberData(nameof(MalformedValues))]
    public void TheFirstToggleReplacesAMalformedValueAndKeepsTheOtherKey(string? value)
    {
        RecordingDictionary d = FavouriteFixtures.Seed(value);
        var f = new ProfileFavourites(d);

        Assert.Equal(FavouriteToggleOutcome.Added, f.Toggle(new InventorySlot(0, 1)));
        Assert.Equal("F1;0,1", d[PersistenceKeys.Favourites]);
        Assert.Equal(FavouriteFixtures.OtherValue, d[FavouriteFixtures.OtherKey]);
    }
}
