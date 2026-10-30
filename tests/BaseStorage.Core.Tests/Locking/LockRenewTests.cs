namespace BaseStorage.Core.Tests.Locking;

using System;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Persistence;

/// <summary>AC-1 (contract 5.3 rows 4 and 10): Renew moves the holder's expiry forward, and Renew without a lock is NotHeld.</summary>
public sealed class LockRenewTests
{
    // GW-U-259 (AC-1): the renewal of Erik at 3620 moves his expiry from 3630 to 3650.
    [Fact]
    public void RenewMovesTheExpiryForwardOnTheLowestStation()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        store.ResetLog();
        clock.Elapsed = TimeSpan.FromSeconds(3620);

        OrganizeLockResult result = locks.Renew(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");

        Assert.Equal(OrganizeLockStatus.Granted, result.Status);
        Assert.Equal(new OrganizeLockRecord(LockFixtures.ErikId, "Erik", TimeSpan.FromSeconds(3650)), result.Record);
        Assert.True(result.Record!.ExpiresAt > TimeSpan.FromSeconds(3630), "The renewed expiry must be later than the old one.");
        Assert.Equal(new[] { (LockFixtures.S1, LockFixtures.Erik3650) }, store.Contents());
        LockFixtures.AssertSingleWriteTo(store, LockFixtures.S1);
    }

    // GW-U-259 (AC-1): Renew on a fresh store is NotHeld, and nothing is written.
    [Fact]
    public void RenewWithoutALockIsNotHeldAndWritesNothing()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();

        OrganizeLockResult result = locks.Renew(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");

        Assert.Equal(OrganizeLockResult.NotHeld, result);
        Assert.Equal(OrganizeLockStatus.NotHeld, result.Status);
        LockFixtures.AssertNoWrite(store);
        Assert.Empty(store.Contents());
    }
}
