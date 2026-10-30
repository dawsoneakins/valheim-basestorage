namespace BaseStorage.Core.Tests.Organize;

using System;
using System.Collections.Generic;

/// <summary>
/// A small, self-contained SplitMix64 generator for the S-E004-007 fuzzer. It does not depend on <see cref="System.Random"/>, so a
/// seed gives the same sequence on every runtime, and a logged seed reproduces a failing base exactly.
/// </summary>
internal sealed class FuzzRandom
{
    private ulong state;

    /// <summary>Creates a generator at the given seed.</summary>
    /// <param name="seed">The 64-bit seed.</param>
    internal FuzzRandom(ulong seed)
    {
        state = seed;
    }

    /// <summary>Returns the next 64-bit value.</summary>
    internal ulong NextUInt64()
    {
        state += 0x9E3779B97F4A7C15UL;
        ulong z = state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>Returns a value in <paramref name="minInclusive"/>..<paramref name="maxExclusive"/>-1.</summary>
    /// <param name="minInclusive">The smallest value.</param>
    /// <param name="maxExclusive">One past the largest value; greater than <paramref name="minInclusive"/>.</param>
    internal int Next(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Must be greater than the minimum.");
        }

        ulong range = (ulong)((long)maxExclusive - minInclusive);
        return (int)(minInclusive + (long)(NextUInt64() % range));
    }

    /// <summary>Returns true with the given probability in percent.</summary>
    /// <param name="percent">The probability, 0..100.</param>
    internal bool Chance(int percent) => Next(0, 100) < percent;

    /// <summary>Returns one element of a non-empty list, uniformly.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="items">The list; not empty.</param>
    internal T Pick<T>(IReadOnlyList<T> items) => items[Next(0, items.Count)];
}
