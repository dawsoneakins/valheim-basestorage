namespace BaseStorage.Core.Tests.Persistence;

using System.Collections.Generic;
using System.Linq;

/// <summary>AC-3 and contract 4.4: no encoded form of one codec decodes as a value of another codec.</summary>
public sealed class CrossCodecTests
{
    private static readonly string[] Codecs = ["CategoryCodec", "OrganizeLockCodec", "FavouritesCodec"];

    // GW-U-083
    [Fact]
    public void EachCodecsExamplesDecodeToNoValueInTheOtherTwo()
    {
        foreach (string codec in Codecs)
        {
            _ = PersistenceContract.Resolve(codec);
        }

        var examples = new Dictionary<string, string[]>
        {
            ["CategoryCodec"] = PersistenceFixtures.CategoryEncodings.Select(e => e.Encoded).ToArray(),
            ["OrganizeLockCodec"] = PersistenceFixtures.LockEncodings.Select(e => e.Encoded).ToArray(),
            ["FavouritesCodec"] = PersistenceFixtures.FavouritesEncodings.Select(e => e.Encoded).ToArray(),
        };

        var failures = new List<string>();
        foreach (KeyValuePair<string, string[]> pair in examples)
        {
            foreach (string other in Codecs.Where(c => c != pair.Key))
            {
                foreach (string encoded in pair.Value)
                {
                    object? result = PersistenceContract.Decode(other, encoded);
                    if (result is not null)
                    {
                        failures.Add($"{other}.Decode(\"{encoded}\") returned a value; it belongs to {pair.Key}.");
                    }
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }
}
