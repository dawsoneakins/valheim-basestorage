namespace BaseStorage.Core.Tests.Locking;

using System;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Snapshots;

/// <summary>AC-1 (contract 5.2, 4.2): the lock station is the lowest StationId, and duplicate ids count as one station.</summary>
public sealed class LockStationTests
{
    // GW-U-261 (AC-1): base12 returns s1's id.
    [Fact]
    public void FindLockStationOfBase12IsS1()
    {
        Assert.Equal(LockFixtures.S1, OrganizeLock.FindLockStation(LockFixtures.Base12()));
    }

    // GW-U-261 (AC-1): every permutation of {s0, s1, s2} returns s0's id. One case per permutation, named by its index.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void FindLockStationPicksS0FromEveryPermutation(int order)
    {
        StationSnapshot[][] permutations =
        {
            new[] { LockFixtures.Station0, LockFixtures.Station1, LockFixtures.Station2 },
            new[] { LockFixtures.Station0, LockFixtures.Station2, LockFixtures.Station1 },
            new[] { LockFixtures.Station1, LockFixtures.Station0, LockFixtures.Station2 },
            new[] { LockFixtures.Station1, LockFixtures.Station2, LockFixtures.Station0 },
            new[] { LockFixtures.Station2, LockFixtures.Station0, LockFixtures.Station1 },
            new[] { LockFixtures.Station2, LockFixtures.Station1, LockFixtures.Station0 },
        };

        Assert.Equal(LockFixtures.S0, OrganizeLock.FindLockStation(permutations[order]));
    }

    // GW-U-261 (AC-1): a list with a duplicate of s1 returns s1's id (duplicates are allowed).
    [Fact]
    public void FindLockStationOfADuplicatedStationIsThatStation()
    {
        Assert.Equal(LockFixtures.S1, OrganizeLock.FindLockStation(new[] { LockFixtures.Station1, LockFixtures.Station1 }));
    }

    // GW-U-263 (AC-1): Acquire over [s1, s2, s1] reads each distinct id once and writes once to s1.
    [Fact]
    public void AcquireWithDuplicateStationsReadsEachDistinctIdOnce()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        var stations = new[] { LockFixtures.Station1, LockFixtures.Station2, LockFixtures.Station1 };

        OrganizeLockResult result = locks.Acquire(stations, LockFixtures.ErikId, "Erik");

        Assert.Equal(OrganizeLockStatus.Granted, result.Status);
        string[] reads = Array.FindAll(store.Calls.ToArray(), call => call.StartsWith("Read:", StringComparison.Ordinal));
        Assert.Equal(2, reads.Length);
        Assert.Single(Array.FindAll(reads, call => call == FakeOrganizeLockStore.ReadCall(LockFixtures.S1)));
        Assert.Single(Array.FindAll(reads, call => call == FakeOrganizeLockStore.ReadCall(LockFixtures.S2)));
        LockFixtures.AssertSingleWriteTo(store, LockFixtures.S1);
    }

    // GW-U-263 (AC-1): Release over [s1, s1] makes exactly one Clear and returns true.
    [Fact]
    public void ReleaseWithDuplicateStationsClearsOnce()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        store.Seed(LockFixtures.S1, LockFixtures.Erik3650);
        clock.Elapsed = TimeSpan.FromSeconds(3620);

        bool released = locks.Release(new[] { LockFixtures.Station1, LockFixtures.Station1 }, LockFixtures.ErikId);

        Assert.True(released);
        Assert.Equal(1, store.ClearCount);
        Assert.Empty(store.Contents());
    }
}
