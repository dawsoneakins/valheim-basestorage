namespace BaseStorage.Core.Tests.Locking;

using System;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Persistence;

/// <summary>AC-2 (contract 4.1, 5.4): a lock expires exactly at its ExpiresAt and an abandoned lock never sticks.</summary>
public sealed class LockExpiryTests
{
    // GW-U-266 (AC-2): one tick before ExpiresAt Erik is still live; at ExpiresAt he is expired and Astrid is granted.
    [Fact]
    public void ExpiryIsExactAtExpiresAt()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        store.ResetLog();

        // Row 1: one tick before 3630 s.
        clock.Elapsed = TimeSpan.FromTicks(36299999999L);
        OrganizeLockResult oneTickBefore = locks.Acquire(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid");
        Assert.Equal(OrganizeLockStatus.Refused, oneTickBefore.Status);
        Assert.Equal("Erik", oneTickBefore.HolderName);
        LockFixtures.AssertNoWrite(store);
        Assert.Equal(new[] { (LockFixtures.S1, LockFixtures.Erik3630) }, store.Contents());

        // Row 2: exactly 3630 s, which is ExpiresAt.
        clock.Elapsed = TimeSpan.FromSeconds(3630);
        OrganizeLockResult atExpiry = locks.Acquire(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid");
        Assert.Equal(OrganizeLockStatus.Granted, atExpiry.Status);
        Assert.Equal(new OrganizeLockRecord(LockFixtures.AstridId, "Astrid", TimeSpan.FromSeconds(3660)), atExpiry.Record);
        Assert.Equal(new[] { (LockFixtures.S1, LockFixtures.Astrid3660) }, store.Contents());

        // Row 3: Erik has lost the lock to Astrid.
        OrganizeLockResult renewByErik = locks.Renew(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        Assert.Equal(OrganizeLockStatus.Refused, renewByErik.Status);
        Assert.Equal("Astrid", renewByErik.HolderName);
    }

    // GW-U-269 (AC-2): an abandoned Erik lock (acquired at 3600, never renewed) frees the base at 3630, 3631 and one day later.
    [Theory]
    [InlineData(3630)]
    [InlineData(3631)]
    [InlineData(90000)]
    public void AnAbandonedLockFreesItselfAfterItsDuration(int readAt)
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        clock.Elapsed = TimeSpan.FromSeconds(readAt);

        OrganizeLockResult result = locks.Acquire(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid");

        Assert.Equal(OrganizeLockStatus.Granted, result.Status);
        Assert.Equal(new OrganizeLockRecord(LockFixtures.AstridId, "Astrid", TimeSpan.FromSeconds(readAt + 30)), result.Record);
    }

    // GW-U-269 (AC-2): an own expired lock (Erik at 3640 after acquiring at 3600): Renew is NotHeld with no write, and Acquire is granted until 3670.
    [Fact]
    public void AnOwnExpiredLockIsNotHeldOnRenewAndGrantedOnAcquire()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        clock.Elapsed = TimeSpan.FromSeconds(3640);
        store.ResetLog();

        OrganizeLockResult renew = locks.Renew(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        Assert.Equal(OrganizeLockStatus.NotHeld, renew.Status);
        LockFixtures.AssertNoWrite(store);

        OrganizeLockResult acquire = locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        Assert.Equal(OrganizeLockStatus.Granted, acquire.Status);
        Assert.Equal(new[] { (LockFixtures.S1, LockFixtures.Erik3670) }, store.Contents());
    }
}
