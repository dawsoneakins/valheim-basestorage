namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BaseStorage.Core.Bases;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.StationGraphFixtures;

/// <summary>AC-1: purity (no effect on the input) and result immutability (contract 7.2 and 7.3).</summary>
public sealed class StationGraphResultTests
{
    // GW-U-122 (AC-1)
    [Fact]
    public void InputIsUnchangedByTheCall()
    {
        StationSnapshot[] input = Rows["G2a"].Stations.ToArray();
        StationSnapshot[] before = input.ToArray();

        StationGraph.FindBase(input, Id(1), DefaultMax);

        Assert.Equal(before, input);
    }

    // GW-U-122 (AC-1): the result keeps no reference to the input array.
    [Fact]
    public void ResultDoesNotChangeWhenTheInputIsOverwritten()
    {
        StationSnapshot[] input = Rows["G2a"].Stations.ToArray();
        StationGraphResult result = StationGraph.FindBase(input, Id(1), DefaultMax);
        StationSnapshot[] expected = result.Stations.ToArray();

        for (int i = 0; i < input.Length; i++)
        {
            input[i] = S(900u + (uint)i, 500f + i);
        }

        Assert.Equal(expected, result.Stations);
    }

    // GW-U-122 (AC-1): Stations is a read-only, never-null collection, also on the empty result.
    [Fact]
    public void StationsIsAReadOnlyCollectionAndNeverNull()
    {
        StationGraphResult connected = Run("G2a");
        StationGraphResult empty = Run("G13c");

        Assert.NotNull(connected.Stations);
        Assert.NotNull(empty.Stations);
        Assert.IsType<ReadOnlyCollection<StationSnapshot>>(connected.Stations);
        Assert.IsType<ReadOnlyCollection<StationSnapshot>>(empty.Stations);
    }

    // GW-U-122 (AC-1): mutation through IList throws NotSupportedException.
    [Fact]
    public void MutatingStationsThroughIListThrowsNotSupportedException()
    {
        IList<StationSnapshot> list = (IList<StationSnapshot>)Run("G2a").Stations;

        Assert.Throws<NotSupportedException>(() => list.Add(S(900, 0f)));
        Assert.Throws<NotSupportedException>(() => list.Clear());
        Assert.Throws<NotSupportedException>(() => list[0] = S(900, 0f));
    }

    // GW-U-122 (AC-1): two calls give equal sequences but distinct result instances.
    [Fact]
    public void TwoCallsGiveEqualSequencesButDifferentResultInstances()
    {
        StationGraphResult first = Run("G2a");
        StationGraphResult second = Run("G2a");

        Assert.Equal(first.Stations, second.Stations);
        Assert.NotSame(first, second);
    }
}
