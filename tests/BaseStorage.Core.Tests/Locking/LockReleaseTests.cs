namespace BaseStorage.Core.Tests.Locking;

using System;
using BaseStorage.Core.Locking;

/// <summary>AC-1 and AC-7 (contract 5.3 rows 6-11, 4.2 Release): a release leaves no live lock, and a wrong-holder release changes nothing.</summary>
public sealed class LockReleaseTests
{
    // GW-U-260 (AC-1): the step table from Erik@3650 at 3620, through release, idempotence, NotHeld, and a new Acquire.
    [Fact]
    public void ReleaseLeavesNoLiveLockAndTheBaseCanBeTakenAgain()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        clock.Elapsed = TimeSpan.FromSeconds(3620);
        locks.Renew(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");

        // Step 1: the own live record is released.
        Assert.True(locks.Release(LockFixtures.Base12(), LockFixtures.ErikId), "Step 1: the own lock must be released.");
        Assert.Empty(store.Contents());

        // Step 2: a second release finds nothing, and clears nothing.
        store.ResetLog();
        Assert.False(locks.Release(LockFixtures.Base12(), LockFixtures.ErikId), "Step 2: a second release must return false.");
        LockFixtures.AssertNoWrite(store);

        // Step 3: Renew after the release is NotHeld.
        Assert.Equal(OrganizeLockStatus.NotHeld, locks.Renew(LockFixtures.Base12(), LockFixtures.ErikId, "Erik").Status);

        // Step 4: Astrid can take the free base; the granted record is on s1.
        OrganizeLockResult granted = locks.Acquire(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid");
        Assert.Equal(OrganizeLockStatus.Granted, granted.Status);
        Assert.Equal(new[] { (LockFixtures.S1, LockFixtures.Astrid3650) }, store.Contents());
    }

    // GW-U-260 (AC-1): an own expired record (Erik until 3630, read at 3700) is cleared by Release, which returns true.
    [Fact]
    public void ReleaseClearsAnOwnExpiredRecord()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        store.Seed(LockFixtures.S1, LockFixtures.Erik3630);
        clock.Elapsed = TimeSpan.FromSeconds(3700);

        Assert.True(locks.Release(LockFixtures.Base12(), LockFixtures.ErikId));

        Assert.Empty(store.Contents());
        Assert.Equal(1, store.ClearCount);
    }

    // GW-U-282 (AC-7, contract 5.3 rows 6-7): Astrid cannot release Erik's live lock, and nothing changes.
    [Fact]
    public void WrongHolderReleaseChangesNothing()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        clock.Elapsed = TimeSpan.FromSeconds(3620);
        locks.Renew(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        store.ResetLog();

        Assert.False(locks.Release(LockFixtures.Base12(), LockFixtures.AstridId), "A wrong-holder release must return false.");

        Assert.Equal(new[] { (LockFixtures.S1, LockFixtures.Erik3650) }, store.Contents());
        Assert.Equal(0, store.ClearCount);
        Assert.DoesNotContain(store.Calls, call => call.StartsWith("Clear:", StringComparison.Ordinal));

        OrganizeLockResult refused = locks.Acquire(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid");
        Assert.Equal(OrganizeLockStatus.Refused, refused.Status);
        Assert.Equal("Erik", refused.HolderName);

        OrganizeLockResult renewed = locks.Renew(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        Assert.Equal(OrganizeLockStatus.Granted, renewed.Status);
    }

    // GW-U-282 (AC-7): a foreign expired record (Erik until 3630, read at 3700) is not released by Astrid and stays stored.
    [Fact]
    public void WrongHolderReleaseLeavesAForeignExpiredRecordStored()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        store.Seed(LockFixtures.S1, LockFixtures.Erik3630);
        clock.Elapsed = TimeSpan.FromSeconds(3700);

        Assert.False(locks.Release(LockFixtures.Base12(), LockFixtures.AstridId));

        Assert.Equal(new[] { (LockFixtures.S1, LockFixtures.Erik3630) }, store.Contents());
        Assert.Equal(0, store.ClearCount);
    }

    // GW-U-282 (AC-7): Release never reads the clock or the settings, in either outcome.
    [Fact]
    public void ReleaseReadsNeitherTheClockNorTheSettings()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        store.Seed(LockFixtures.S1, LockFixtures.Erik3650);

        locks.Release(LockFixtures.Base12(), LockFixtures.AstridId);
        locks.Release(LockFixtures.Base12(), LockFixtures.ErikId);

        Assert.Equal(0, clock.Reads);
        Assert.Equal(0, settings.IntCalls);
    }
}
