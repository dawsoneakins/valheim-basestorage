namespace BaseStorage.Core.Tests.Locking;

using System;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>AC-1 (contract 5.3 row 1, LC1): an Acquire on a free base writes only to the lowest-id station.</summary>
public sealed class LockAcquireTests
{
    // GW-U-258 (AC-1): the three layouts; the lock station is the lowest id of the list, whatever the order of the list.
    [Theory]
    [InlineData("base12", "s1")]
    [InlineData("ascending", "s1")]
    [InlineData("base012", "s0")]
    public void AcquireOnAFreeBaseWritesOnlyToTheLowestIdStation(string layout, string lockStation)
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        StationId station = LockFixtures.Named(lockStation);

        OrganizeLockResult result = locks.Acquire(LockFixtures.Layout(layout), LockFixtures.ErikId, "Erik");

        Assert.Equal(OrganizeLockStatus.Granted, result.Status);
        Assert.Equal(new OrganizeLockRecord(LockFixtures.ErikId, "Erik", TimeSpan.FromSeconds(3630)), result.Record);
        Assert.Equal(new[] { (station, LockFixtures.Erik3630) }, store.Contents());
        LockFixtures.AssertSingleWriteTo(store, station);
    }
}
