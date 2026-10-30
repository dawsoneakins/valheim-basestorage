namespace BaseStorage.Core.Tests.Locking;

using System;
using BaseStorage.Core.Locking;

/// <summary>AC-4 (contract 5.3 row 3, 4.1 own): a holder's re-acquire renews the lock; identity is the holder id alone.</summary>
public sealed class LockReacquireTests
{
    // GW-U-274 (AC-4): Erik re-acquires at 3610 and renews to 3640 on the same station, with one write to it.
    [Fact]
    public void HolderReacquireRenewsInsteadOfRefusing()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        store.ResetLog();
        clock.Elapsed = TimeSpan.FromSeconds(3610);

        OrganizeLockResult result = locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");

        Assert.Equal(OrganizeLockStatus.Granted, result.Status);
        Assert.Equal(new[] { (LockFixtures.S1, LockFixtures.Erik3640) }, store.Contents());
        LockFixtures.AssertSingleWriteTo(store, LockFixtures.S1);
    }

    // GW-U-274 (AC-4): the same id with a new name is still the holder, and the stored name becomes the new one.
    [Fact]
    public void ReacquireWithANewNameStoresTheNewName()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        clock.Elapsed = TimeSpan.FromSeconds(3610);

        OrganizeLockResult result = locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik the Red");

        Assert.Equal(OrganizeLockStatus.Granted, result.Status);
        Assert.Equal(new[] { (LockFixtures.S1, "L1;1923487561234;36400000000;Erik the Red") }, store.Contents());
    }

    // GW-U-274 (AC-4): the same name with another id is a different holder, so it is refused.
    [Fact]
    public void SameNameWithAnotherIdIsRefused()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        store.ResetLog();
        clock.Elapsed = TimeSpan.FromSeconds(3610);

        OrganizeLockResult result = locks.Acquire(LockFixtures.Base12(), 555L, "Erik");

        Assert.Equal(OrganizeLockStatus.Refused, result.Status);
        Assert.Equal("Erik", result.HolderName);
        LockFixtures.AssertNoWrite(store);
    }
}
