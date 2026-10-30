namespace BaseStorage.Core.Tests.Persistence;

using System;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-2 and AC-3 for OrganizeLockCodec (contract 4.2): exact encodings, round trips, and decode-to-no-value.</summary>
public sealed class OrganizeLockCodecTests
{
    private const string Codec = "OrganizeLockCodec";

    // GW-U-075
    [Fact]
    public void EncodeGivesTheExactContractStrings()
    {
        _ = PersistenceContract.Resolve(Codec);

        foreach (var row in PersistenceFixtures.LockEncodings)
        {
            object value = PersistenceContract.LockOf(row.HolderId, row.HolderName, row.Ticks);
            Assert.Equal(row.Encoded, PersistenceContract.Encode(Codec, value));
        }
    }

    // GW-U-075
    [Fact]
    public void EveryPositiveRowDecodesToAnEqualRecordAndReencodesExactly()
    {
        foreach (var row in PersistenceFixtures.LockEncodings)
        {
            object value = PersistenceContract.LockOf(row.HolderId, row.HolderName, row.Ticks);
            object? decoded = PersistenceContract.Decode(Codec, row.Encoded);

            Assert.NotNull(decoded);
            ValueSemantics.AssertEqual(value, decoded!);
            Assert.Equal(row.Encoded, PersistenceContract.Encode(Codec, decoded));
        }
    }

    // GW-U-075
    [Fact]
    public void MaximumIdWithMinimumExpiryRoundTrips()
    {
        object value = PersistenceContract.LockOf(long.MaxValue, "Max", long.MinValue);
        string encoded = PersistenceContract.Encode(Codec, value);

        Assert.Equal("L1;9223372036854775807;-9223372036854775808;Max", encoded);
        object? decoded = PersistenceContract.Decode(Codec, encoded);
        Assert.NotNull(decoded);
        ValueSemantics.AssertEqual(value, decoded!);
    }

    // GW-U-081
    [Fact]
    public void MinimumIdWithMaximumExpiryDecodesAndReencodes()
    {
        const string encoded = "L1;-9223372036854775808;9223372036854775807;Min";
        object value = PersistenceContract.LockOf(long.MinValue, "Min", long.MaxValue);

        object? decoded = PersistenceContract.Decode(Codec, encoded);
        Assert.NotNull(decoded);
        ValueSemantics.AssertEqual(value, decoded!);
        Assert.Equal(encoded, PersistenceContract.Encode(Codec, decoded));
    }

    // GW-U-081
    [Fact]
    public void ExpiryBelowInt64MinimumDecodesToNoValue()
    {
        PersistenceContract.AssertNoValue(Codec, "L1;1;-9223372036854775809;Bo");
    }

    // GW-U-075
    [Fact]
    public void EncodeOfNullIsAnArgumentNullException()
    {
        _ = PersistenceContract.Resolve(Codec);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => { PersistenceContract.Encode(Codec, null); });
        Assert.Equal("value", ex.ParamName);
    }

    // GW-U-081
    [Theory]
    [MemberData(nameof(PersistenceFixtures.LockMalformedRows), MemberType = typeof(PersistenceFixtures))]
    public void MalformedDecodesToNoValue(string? encoded)
    {
        PersistenceContract.AssertNoValue(Codec, encoded);
    }
}
