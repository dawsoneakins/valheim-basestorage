namespace BaseStorage.Core.Tests.Persistence;

using System;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-2 and AC-3 for CategoryCodec (contract 4.1): exact encodings, round trips, and decode-to-no-value.</summary>
public sealed class CategoryCodecTests
{
    private const string Codec = "CategoryCodec";

    // GW-U-074
    [Fact]
    public void EncodeGivesTheExactContractStrings()
    {
        _ = PersistenceContract.Resolve(Codec);

        foreach (var (name, encoded) in PersistenceFixtures.CategoryEncodings)
        {
            Assert.Equal(encoded, PersistenceContract.Encode(Codec, PersistenceContract.CategoryOrKeep(name)));
        }
    }

    // GW-U-074
    [Fact]
    public void DecodeOfEncodeReturnsAnEqualValue()
    {
        foreach (var (name, _) in PersistenceFixtures.CategoryEncodings)
        {
            object value = PersistenceContract.CategoryOrKeep(name);
            object? decoded = PersistenceContract.Decode(Codec, PersistenceContract.Encode(Codec, value));

            Assert.NotNull(decoded);
            ValueSemantics.AssertEqual(value, decoded!);
        }
    }

    // GW-U-074
    [Fact]
    public void PositiveDecodeRowsGiveTheExpectedValueAndReencodeExactly()
    {
        foreach (var (encoded, name) in PersistenceFixtures.CategoryDecodes)
        {
            object? decoded = PersistenceContract.Decode(Codec, encoded);

            Assert.NotNull(decoded);
            Assert.Equal(name is null, PersistenceContract.Flag(decoded!, "IsKeep"));
            Assert.Equal(name, PersistenceContract.Text(decoded!, "CategoryName"));
            ValueSemantics.AssertEqual(PersistenceContract.CategoryOrKeep(name), decoded!);
            Assert.Equal(encoded, PersistenceContract.Encode(Codec, decoded));
        }
    }

    // GW-U-074
    [Fact]
    public void SpacesLongNamesAndLineSeparatorRoundTrip()
    {
        string longName = new string('W', 1000);

        foreach (string name in new[] { " Wood ", longName, "A" + (char)0x2028 + "B" })
        {
            object value = PersistenceContract.CategoryOf(name);
            string encoded = PersistenceContract.Encode(Codec, value);

            Assert.Equal("c:" + name, encoded);
            object? decoded = PersistenceContract.Decode(Codec, encoded);
            Assert.NotNull(decoded);
            ValueSemantics.AssertEqual(value, decoded!);
        }
    }

    // GW-U-074
    [Fact]
    public void EncodeOfNullIsAnArgumentNullException()
    {
        _ = PersistenceContract.Resolve(Codec);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => { PersistenceContract.Encode(Codec, null); });
        Assert.Equal("value", ex.ParamName);
    }

    // GW-U-080
    [Theory]
    [MemberData(nameof(PersistenceFixtures.CategoryMalformedRows), MemberType = typeof(PersistenceFixtures))]
    public void MalformedDecodesToNoValue(string? encoded)
    {
        PersistenceContract.AssertNoValue(Codec, encoded);
    }
}
