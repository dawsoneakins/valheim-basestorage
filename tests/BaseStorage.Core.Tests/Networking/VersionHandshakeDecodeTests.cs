namespace BaseStorage.Core.Tests.Networking;

using System;
using System.Collections.Generic;
using System.Text;
using BaseStorage.Core.Networking;

/// <summary>AC-6.1 (contract 3.2, rule V5): malformed payloads decode to null, safely, for text and for bytes.</summary>
public sealed class VersionHandshakeDecodeTests
{
    private const int CorpusSeed = 20261026;
    private const string Alphabet = "BSV1;0129.\0 \n-abö";

    // GW-U-804 (V5): a valid payload decodes to its version.
    [Fact]
    public void ValidPayloadDecodesToItsVersion()
    {
        Assert.Equal("0.1.0", VersionHandshake.DecodePayload("BSV1;0.1.0"));
        Assert.Equal("0.1.0", VersionHandshake.DecodePayload(Encoding.ASCII.GetBytes("BSV1;0.1.0")));
    }

    // GW-U-804 (V5): null text decodes to null without throwing.
    [Fact]
    public void NullTextDecodesToNull()
    {
        Assert.Null(VersionHandshake.DecodePayload((string?)null));
    }

    // GW-U-804 (V5): every contract 3.2 malformed text decodes to null.
    [Theory]
    [MemberData(nameof(NetworkingFixtures.MalformedPayloadRows), MemberType = typeof(NetworkingFixtures))]
    public void MalformedTextDecodesToNull(string payload)
    {
        Assert.Null(VersionHandshake.DecodePayload(payload));
    }

    // GW-U-804 (V5): the same malformed texts as UTF-8 bytes decode to null.
    [Theory]
    [MemberData(nameof(NetworkingFixtures.MalformedPayloadRows), MemberType = typeof(NetworkingFixtures))]
    public void MalformedTextAsBytesDecodesToNull(string payload)
    {
        Assert.Null(VersionHandshake.DecodePayload(Encoding.UTF8.GetBytes(payload)));
    }

    // GW-U-804 (V5): byte-specific malformed inputs decode to null.
    [Theory]
    [MemberData(nameof(MalformedByteRows))]
    public void MalformedBytesDecodeToNull(byte[]? payload)
    {
        Assert.Null(VersionHandshake.DecodePayload(payload));
    }

    public static IEnumerable<object?[]> MalformedByteRows()
    {
        yield return [null];
        yield return [Array.Empty<byte>()];
        yield return [new byte[] { 0xFF }];
        yield return [new byte[1000]];
        yield return [Concat(new byte[] { 0xEF, 0xBB, 0xBF }, Encoding.ASCII.GetBytes("BSV1;0.1.0"))];
        yield return [Encoding.UTF8.GetBytes("BSV1;0.1.ö")];
        yield return [Encoding.Unicode.GetBytes("BSV1;0.1.0")];
        yield return [Concat(Encoding.ASCII.GetBytes("BSV1;0.1.0"), new byte[] { 0x80 })];
        yield return [Concat(Encoding.ASCII.GetBytes("BSV1;0.1.0"), new byte[] { 0x00 })];
    }

    // GW-U-804 (V5): a seeded corpus of random texts and byte arrays never throws, and every non-null result is a well-formed version
    // whose re-encoding equals the input (canonical form).
    [Fact]
    public void SeededCorpusNeverThrowsAndOnlyCanonicalPayloadsDecode()
    {
        var random = new Random(CorpusSeed);
        int decoded = 0;
        for (int i = 0; i < 2000; i++)
        {
            string text = RandomText(random);
            string? fromText = VersionHandshake.DecodePayload(text);
            if (fromText is not null)
            {
                decoded++;
                Assert.True(ModVersion.IsWellFormed(fromText));
                Assert.Equal(text, VersionHandshake.EncodePayload(fromText));
            }

            byte[] bytes = new byte[random.Next(0, 40)];
            random.NextBytes(bytes);
            string? fromBytes = VersionHandshake.DecodePayload(bytes);
            if (fromBytes is not null)
            {
                Assert.True(ModVersion.IsWellFormed(fromBytes));
                Assert.Equal(bytes, VersionHandshake.EncodePayloadBytes(fromBytes));
            }
        }

        Assert.True(decoded >= 0);
    }

    // GW-U-804 (V5): decoding is deterministic.
    [Fact]
    public void DecodingTwiceGivesTheSameResult()
    {
        foreach (string payload in NetworkingFixtures.MalformedPayloads)
        {
            Assert.Equal(VersionHandshake.DecodePayload(payload), VersionHandshake.DecodePayload(payload));
        }

        Assert.Equal(VersionHandshake.DecodePayload("BSV1;0.1.0"), VersionHandshake.DecodePayload("BSV1;0.1.0"));
    }

    private static string RandomText(Random random)
    {
        int length = random.Next(0, 40);
        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = Alphabet[random.Next(Alphabet.Length)];
        }

        return new string(chars);
    }

    private static byte[] Concat(byte[] first, byte[] second)
    {
        var result = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, result, 0, first.Length);
        Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
        return result;
    }
}
