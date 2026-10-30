namespace BaseStorage.Core.Tests.Persistence;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-2 and AC-3 for FavouritesCodec (contract 4.3): exact encodings, round trips, and decode-to-no-value.</summary>
public sealed class FavouritesCodecTests
{
    private const string Codec = "FavouritesCodec";

    // GW-U-076
    [Fact]
    public void EncodeGivesTheExactContractStrings()
    {
        _ = PersistenceContract.Resolve(Codec);

        foreach (var row in PersistenceFixtures.FavouritesEncodings)
        {
            Assert.Equal(row.Encoded, PersistenceContract.Encode(Codec, PersistenceContract.FavouritesOf(row.Slots)));
        }
    }

    // GW-U-076
    [Fact]
    public void EveryPositiveRowDecodesToAnEqualSetAndReencodesExactly()
    {
        foreach (var row in PersistenceFixtures.FavouritesEncodings)
        {
            object value = PersistenceContract.FavouritesOf(row.Slots);
            object? decoded = PersistenceContract.Decode(Codec, row.Encoded);

            Assert.NotNull(decoded);
            ValueSemantics.AssertEqual(value, decoded!);
            Assert.Equal(row.Encoded, PersistenceContract.Encode(Codec, decoded));
        }
    }

    // GW-U-076
    [Fact]
    public void TagAloneDecodesToTheEmptySetNotToNoValue()
    {
        object? decoded = PersistenceContract.Decode(Codec, "F1");

        Assert.NotNull(decoded);
        ValueSemantics.AssertEqual(PersistenceContract.EmptyFavourites(), decoded!);
    }

    // GW-U-076
    [Fact]
    public void FullGridOf65536SlotsRoundTrips()
    {
        var grid = new List<(int X, int Y)>(256 * 256);
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                grid.Add((x, y));
            }
        }

        object value = PersistenceContract.FavouritesOf(grid.ToArray());
        Assert.Equal(65536, PersistenceContract.Int(value, "Count"));

        string encoded = PersistenceContract.Encode(Codec, value);
        object? decoded = PersistenceContract.Decode(Codec, encoded);

        Assert.NotNull(decoded);
        ValueSemantics.AssertEqual(value, decoded!);
    }

    // GW-U-076
    [Fact]
    public void EncodeOfNullIsAnArgumentNullException()
    {
        _ = PersistenceContract.Resolve(Codec);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => { PersistenceContract.Encode(Codec, null); });
        Assert.Equal("value", ex.ParamName);
    }

    // GW-U-082
    [Theory]
    [MemberData(nameof(PersistenceFixtures.FavouritesMalformedRows), MemberType = typeof(PersistenceFixtures))]
    public void MalformedDecodesToNoValue(string? encoded)
    {
        PersistenceContract.AssertNoValue(Codec, encoded);
    }
}
