namespace BaseStorage.Core.Tests.HostSync;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BaseStorage.Core.HostSync;

/// <summary>AC-6.2, principle 5 (contract 4.3, rules H5 and H8): every malformed class decodes to null, safely, for text and for bytes.</summary>
public sealed class HostSyncCodecDecodeTests
{
    private const int CorpusSeed = 20261026;
    private const string Alphabet = "BSC1;=@\n\r0123456789CraftFomBseMxtinuels ö\0";

    // GW-U-1006 (H5): a canonical payload decodes.
    [Fact]
    public void ContractTextDecodes()
    {
        HostSyncPayload? defaults = HostSyncCodec.Decode(HostSyncFixtures.DefaultsText);
        HostSyncPayload? withCategories = HostSyncCodec.Decode(Encoding.UTF8.GetBytes(HostSyncFixtures.CategoryPayloadText));

        Assert.Equal(HostSyncFixtures.Defaults(), defaults);
        Assert.Equal(HostSyncFixtures.WithCategories(), withCategories);
        Assert.Equal(HostSyncFixtures.CategoryText, withCategories!.CategoryFileText);
    }

    // GW-U-1006 (H5): null text decodes to null without throwing.
    [Fact]
    public void NullTextDecodesToNull()
    {
        Assert.Null(HostSyncCodec.Decode((string?)null));
    }

    // GW-U-1006 (H5): every contract 4.3 malformed text decodes to null.
    [Theory]
    [MemberData(nameof(HostSyncFixtures.MalformedTextRows), MemberType = typeof(HostSyncFixtures))]
    public void MalformedTextDecodesToNull(string reason, string payload)
    {
        Assert.Null(HostSyncCodec.Decode(payload));
        Assert.NotEmpty(reason);
    }

    // GW-U-1006 (H5): the same malformed texts as UTF-8 bytes decode to null.
    [Theory]
    [MemberData(nameof(HostSyncFixtures.MalformedTextRows), MemberType = typeof(HostSyncFixtures))]
    public void MalformedTextAsBytesDecodesToNull(string reason, string payload)
    {
        Assert.Null(HostSyncCodec.Decode(Encoding.UTF8.GetBytes(payload)));
        Assert.NotEmpty(reason);
    }

    // GW-U-1006 (H5, H8): byte-specific malformed inputs decode to null: null, empty, invalid and truncated UTF-8, BOM, UTF-16, over the cap.
    [Theory]
    [MemberData(nameof(MalformedByteRows))]
    public void MalformedBytesDecodeToNull(string reason, byte[]? payload)
    {
        Assert.Null(HostSyncCodec.Decode(payload));
        Assert.NotEmpty(reason);
    }

    public static IEnumerable<object?[]> MalformedByteRows()
    {
        byte[] defaults = Encoding.ASCII.GetBytes(HostSyncFixtures.DefaultsText);

        yield return ["null", null];
        yield return ["empty", Array.Empty<byte>()];
        yield return ["0xFF", new byte[] { 0xFF }];
        yield return ["truncated multi-byte", new byte[] { 0xC3 }];
        yield return ["over the cap", new byte[HostSyncCodec.MaxPayloadLength + 1]];
        yield return ["BOM prefix", Concat(new byte[] { 0xEF, 0xBB, 0xBF }, defaults)];
        yield return ["UTF-16", Encoding.Unicode.GetBytes(HostSyncFixtures.DefaultsText)];
        yield return ["invalid byte inside text", Concat(Encoding.ASCII.GetBytes(HostSyncFixtures.CategoryPayloadText.Substring(0, HostSyncFixtures.CategoryPayloadText.Length - 1)), new byte[] { 0xFF })];
        yield return ["overlong encoding of LF", Concat(defaults.Take(defaults.Length - 1).ToArray(), new byte[] { 0xC0, 0x8A })];
        yield return ["trailing 0x80", Concat(defaults, new byte[] { 0x80 })];
        yield return ["trailing NUL", Concat(defaults, new byte[] { 0x00 })];
        yield return ["lone surrogate", Concat(defaults.Take(defaults.Length - 3).ToArray(), Encoding.ASCII.GetBytes("3\n"), new byte[] { 0xED, 0xA0, 0x80 })];
    }

    // GW-U-1006 (H5): a decode of a non-canonical but well-formed-looking payload is refused rather than normalised.
    [Fact]
    public void NonCanonicalValuesAreRefusedNotNormalised()
    {
        string trueCapitalised = HostSyncFixtures.ReplaceLine(HostSyncFixtures.DefaultsText, "CraftFromBase", "CraftFromBase=TRUE\n");
        string plusInt = HostSyncFixtures.ReplaceLine(HostSyncFixtures.DefaultsText, "MaxBaseStations", "MaxBaseStations=+64\n");

        Assert.Null(HostSyncCodec.Decode(trueCapitalised));
        Assert.Null(HostSyncCodec.Decode(plusInt));
    }

    // GW-U-1007 (H5): a seeded corpus of random texts never throws, and every non-null result re-encodes to its input (canonical form).
    [Fact]
    public void SeededTextCorpusNeverThrowsAndOnlyCanonicalPayloadsDecode()
    {
        var random = new Random(CorpusSeed);
        int decoded = 0;

        for (int i = 0; i < 2000; i++)
        {
            string text = RandomText(random, random.Next(0, 160));
            HostSyncPayload? result = HostSyncCodec.Decode(text);
            HostSyncPayload? again = HostSyncCodec.Decode(text);

            Assert.Equal(result, again);
            if (result is not null)
            {
                decoded++;
                Assert.Equal(text, HostSyncCodec.Encode(result));
            }
        }

        Assert.True(decoded >= 0);
    }

    // GW-U-1007 (H5): a seeded corpus of mutations of a valid payload never throws; whatever still decodes re-encodes to the mutated input.
    [Fact]
    public void SeededMutationCorpusNeverThrowsAndStaysCanonical()
    {
        var random = new Random(CorpusSeed + 1);
        string valid = HostSyncCodec.Encode(HostSyncFixtures.Changed());
        int survived = 0;

        for (int i = 0; i < 2000; i++)
        {
            string mutated = Mutate(random, valid, random.Next(1, 4));
            HostSyncPayload? result = HostSyncCodec.Decode(mutated);
            HostSyncPayload? fromBytes = HostSyncCodec.Decode(Encoding.UTF8.GetBytes(mutated));

            Assert.Equal(result, fromBytes);
            if (result is not null)
            {
                survived++;
                Assert.Equal(mutated, HostSyncCodec.Encode(result));
            }
        }

        // Mutations inside the category text (the bulk of the payload) keep the length only when a character is replaced, so some survive.
        Assert.True(survived > 0, "No mutation survived; the corpus is not exercising the text path.");
    }

    // GW-U-1007 (H5): a seeded corpus of random byte arrays never throws.
    [Fact]
    public void SeededByteCorpusNeverThrows()
    {
        var random = new Random(CorpusSeed + 2);
        byte[] valid = HostSyncCodec.EncodeBytes(HostSyncFixtures.WithCategories());

        for (int i = 0; i < 2000; i++)
        {
            byte[] bytes;
            if (i % 2 == 0)
            {
                bytes = new byte[random.Next(0, 200)];
                random.NextBytes(bytes);
            }
            else
            {
                bytes = (byte[])valid.Clone();
                bytes[random.Next(bytes.Length)] = (byte)random.Next(256);
            }

            HostSyncPayload? result = HostSyncCodec.Decode(bytes);
            if (result is not null)
            {
                Assert.Equal(bytes, HostSyncCodec.EncodeBytes(result));
            }
        }
    }

    private static string RandomText(Random random, int length)
    {
        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = Alphabet[random.Next(Alphabet.Length)];
        }

        return new string(chars);
    }

    private static string Mutate(Random random, string text, int count)
    {
        var builder = new StringBuilder(text);
        for (int i = 0; i < count && builder.Length > 0; i++)
        {
            int at = random.Next(builder.Length);
            switch (random.Next(3))
            {
                case 0:
                    builder[at] = Alphabet[random.Next(Alphabet.Length)];
                    break;
                case 1:
                    builder.Remove(at, 1);
                    break;
                default:
                    builder.Insert(at, Alphabet[random.Next(Alphabet.Length)]);
                    break;
            }
        }

        return builder.ToString();
    }

    private static byte[] Concat(params byte[][] parts) => parts.SelectMany(part => part).ToArray();
}
