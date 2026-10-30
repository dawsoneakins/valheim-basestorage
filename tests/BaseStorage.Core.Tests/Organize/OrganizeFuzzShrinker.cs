namespace BaseStorage.Core.Tests.Organize;

using System;
using System.Globalization;
using Xunit.Sdk;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>
/// The shrinking aid of the S-E004-007 fuzzer. When a base fails, it regenerates the same base from the same seed with 1, 2, ...
/// chests (the generator's prefix property keeps those chests identical) and reports the smallest prefix that still fails, with
/// its description, so the failure can be turned into a literal fixture.
/// </summary>
internal static class OrganizeFuzzShrinker
{
    /// <summary>The smallest failing prefix of a base.</summary>
    /// <param name="Base">The shrunken base.</param>
    /// <param name="Failure">The assertion it fails.</param>
    internal sealed record Shrunk(RandomBaseGenerator.GeneratedBase Base, XunitException Failure);

    /// <summary>Finds the smallest chest prefix of base <paramref name="index"/> that fails <paramref name="check"/>.</summary>
    /// <param name="seed">The corpus seed.</param>
    /// <param name="index">The base index.</param>
    /// <param name="chestCount">The chest count of the full base.</param>
    /// <param name="check">The check that failed on the full base; it throws an xunit assertion exception on failure.</param>
    /// <returns>The smallest failing prefix, or null when only the full base fails.</returns>
    internal static Shrunk? Shrink(int seed, int index, int chestCount, Action<PlanInput> check)
    {
        for (int n = 1; n < chestCount; n++)
        {
            RandomBaseGenerator.GeneratedBase prefix = RandomBaseGenerator.Generate(seed, index, n);
            try
            {
                check(prefix.Input);
            }
            catch (XunitException failure)
            {
                return new Shrunk(prefix, failure);
            }
        }

        return null;
    }

    /// <summary>Formats the failure of one base, with the seed, the index and the smallest failing prefix, for the assertion message.</summary>
    /// <param name="seed">The corpus seed.</param>
    /// <param name="full">The failing base.</param>
    /// <param name="failure">The assertion the full base failed.</param>
    /// <param name="shrunk">The smallest failing prefix, or null.</param>
    internal static string Report(int seed, RandomBaseGenerator.GeneratedBase full, XunitException failure, Shrunk? shrunk)
    {
        string header = string.Format(
            CultureInfo.InvariantCulture,
            "Fuzz seed {0}, base {1} ({2} chests) failed: {3}\n",
            seed,
            full.Index,
            full.Input.Containers.Length,
            failure.Message);
        if (shrunk is null)
        {
            return header + "No smaller chest prefix fails. Full base:\n" + RandomBaseGenerator.Describe(full);
        }

        return header
            + string.Format(
                CultureInfo.InvariantCulture,
                "Smallest failing prefix: {0} chests, failing with: {1}\n",
                shrunk.Base.Input.Containers.Length,
                shrunk.Failure.Message)
            + RandomBaseGenerator.Describe(shrunk.Base);
    }
}
