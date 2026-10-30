namespace BaseStorage.Core.Tests.Persistence;

using System;
using System.Linq;
using System.Text;

/// <summary>
/// AC-3 with P4 and P7 (contract 0.2, ADR-GWIN-013): no decoder throws, and any non-null result re-encodes to the input.
/// The corpus is deterministic: a fixed seed, a fixed alphabet, and fixed one-million-character shapes.
/// </summary>
public sealed class DecodeRobustnessTests
{
    private const int CorpusSeed = 20261025;
    private const int RandomCount = 2000;
    private const int LongLength = 1_000_000;

    private const string Alphabet =
        "kcL1F:;,-+0123456789 .\t\r\n\u0000\u007F\u0085١٢٣𐀀\uDFFFéxO";

    private static readonly string[] Codecs = ["CategoryCodec", "OrganizeLockCodec", "FavouritesCodec"];

    // GW-U-084
    [Fact]
    public void PrefixesDeletionsAndDuplicationsNeverThrow()
    {
        foreach (string valid in PersistenceFixtures.AllEncodedExamples())
        {
            for (int length = 0; length <= valid.Length; length++)
            {
                AssertRobust(valid[..length]);
            }

            for (int i = 0; i < valid.Length; i++)
            {
                AssertRobust(valid.Remove(i, 1));
                AssertRobust(valid.Insert(i, valid[i].ToString()));
            }
        }
    }

    // GW-U-084
    [Fact]
    public void SeededRandomCorpusNeverThrows()
    {
        var random = new Random(CorpusSeed);
        for (int n = 0; n < RandomCount; n++)
        {
            int length = random.Next(0, 33);
            var text = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                text.Append(Alphabet[random.Next(Alphabet.Length)]);
            }

            AssertRobust(text.ToString());
        }
    }

    // GW-U-084
    [Fact]
    public void OneMillionCharacterInputsNeverThrow()
    {
        AssertRobust(Pad("c:", 'W'));
        AssertRobust(Pad("L1;", '1'));
        AssertRobust(Pad("F1;", '0'));
    }

    private static string Pad(string prefix, char fill) => prefix + new string(fill, LongLength - prefix.Length);

    /// <summary>
    /// Every codec must return without throwing. A non-null result must re-encode, with its own codec, to the input.
    /// Messages carry only the length, so a failure on a million-character input stays readable.
    /// </summary>
    private static void AssertRobust(string input)
    {
        foreach (string codec in Codecs)
        {
            (Exception? thrown, object? result) = PersistenceContract.DecodeCapture(codec, input);
            if (thrown is not null)
            {
                Assert.Fail($"{codec}.Decode threw {thrown.GetType().Name} for an input of length {input.Length}.");
            }

            if (result is not null)
            {
                string reencoded = PersistenceContract.Encode(codec, result);
                Assert.True(
                    string.Equals(reencoded, input, StringComparison.Ordinal),
                    $"{codec}: Encode(Decode(input)) differs from the input (length {input.Length}).");
            }
        }
    }
}
