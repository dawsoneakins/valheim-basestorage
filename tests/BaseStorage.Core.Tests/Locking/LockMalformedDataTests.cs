namespace BaseStorage.Core.Tests.Locking;

using System;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Persistence;

/// <summary>AC-1 and the NFR "malformed = no lock" (contract 5.4 malformed table, LK5): bad or foreign values count as no lock and are never cleared.</summary>
public sealed class LockMalformedDataTests
{
    // GW-U-264 (LK5): each malformed or empty value on s1 is "no lock", so Acquire grants and overwrites it.
    [Theory]
    [InlineData("L1;0;36300000000;Astrid")]
    [InlineData("c:Wood")]
    [InlineData("L2;-6017393921047218534;36300000000;Astrid")]
    [InlineData("")]
    public void MalformedValueOnTheLockStationCountsAsNoLock(string raw)
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        store.Seed(LockFixtures.S1, raw);

        OrganizeLockResult result = locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");

        Assert.Equal(OrganizeLockStatus.Granted, result.Status);
        Assert.Equal(new OrganizeLockRecord(LockFixtures.ErikId, "Erik", TimeSpan.FromSeconds(3630)), result.Record);
        Assert.Equal(new[] { (LockFixtures.S1, LockFixtures.Erik3630) }, store.Contents());
    }

    // GW-U-264 (LK5): Release over a malformed value returns false, keeps the value, and makes no Clear.
    [Fact]
    public void ReleaseOverAForeignCodecValueReturnsFalseAndKeepsIt()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        store.Seed(LockFixtures.S1, "c:Wood");

        Assert.False(locks.Release(LockFixtures.Base12(), LockFixtures.AstridId));

        Assert.Equal(new[] { (LockFixtures.S1, "c:Wood") }, store.Contents());
        Assert.Equal(0, store.ClearCount);
    }

    // GW-U-264 (LK5): a malformed value on a stale station (s2, not the lock station) is never cleared by a grant.
    [Fact]
    public void MalformedValueOnAStaleStationIsNeverCleared()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        store.Seed(LockFixtures.S2, "c:Wood");

        OrganizeLockResult result = locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");

        Assert.Equal(OrganizeLockStatus.Granted, result.Status);
        Assert.Equal(new[] { (LockFixtures.S1, LockFixtures.Erik3630), (LockFixtures.S2, "c:Wood") }, store.Contents());
        LockFixtures.AssertSingleWriteTo(store, LockFixtures.S1);
    }

    // GW-U-264 (LK5): Renew over malformed data only is NotHeld, with no store write.
    [Fact]
    public void RenewOverMalformedDataOnlyIsNotHeld()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        store.Seed(LockFixtures.S1, "c:Wood");

        OrganizeLockResult result = locks.Renew(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");

        Assert.Equal(OrganizeLockStatus.NotHeld, result.Status);
        LockFixtures.AssertNoWrite(store);
        Assert.Equal(new[] { (LockFixtures.S1, "c:Wood") }, store.Contents());
    }
}
