namespace BaseStorage.Core.Tests.Locking;

using System;
using System.Linq;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Snapshots;

/// <summary>AC-6 (contract 4.3, 5.6, LC1-LC6): the lock follows the lowest-id station as the base gains or loses stations.</summary>
public sealed class LockStationChangeTests
{
    // GW-U-277 (AC-6, LC1-LC3): Erik's lock on s1 survives a lower station s0 joining the base, and moves to s0 on his renew.
    [Fact]
    public void LowerIdStationAddedGivesNoDuplicateAndNoSecondLock()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        store.ResetLog();

        // Step 1: at 3605 the stale live record on s1 still refuses Astrid.
        clock.Elapsed = TimeSpan.FromSeconds(3605);
        OrganizeLockResult refusedAtStep1 = locks.Acquire(LockFixtures.Base012(), LockFixtures.AstridId, "Astrid");
        Assert.Equal(OrganizeLockStatus.Refused, refusedAtStep1.Status);
        Assert.Equal("Erik", refusedAtStep1.HolderName);
        LockFixtures.AssertNoWrite(store);
        Assert.Equal(new[] { (LockFixtures.S1, LockFixtures.Erik3630) }, store.Contents());

        // Step 2: Erik renews on s0 and his stale record on s1 is cleared; there is no duplicate.
        clock.Elapsed = TimeSpan.FromSeconds(3610);
        OrganizeLockResult renewedAtStep2 = locks.Renew(LockFixtures.Base012(), LockFixtures.ErikId, "Erik");
        Assert.Equal(OrganizeLockStatus.Granted, renewedAtStep2.Status);
        Assert.Equal(new[] { (LockFixtures.S0, LockFixtures.Erik3640) }, store.Contents());

        // Step 3: a refusal again, and nothing changes.
        clock.Elapsed = TimeSpan.FromSeconds(3615);
        OrganizeLockResult refusedAtStep3 = locks.Acquire(LockFixtures.Base012(), LockFixtures.AstridId, "Astrid");
        Assert.Equal(OrganizeLockStatus.Refused, refusedAtStep3.Status);
        Assert.Equal("Erik", refusedAtStep3.HolderName);
        Assert.Equal(new[] { (LockFixtures.S0, LockFixtures.Erik3640) }, store.Contents());

        // Step 4: Erik releases the lock on s0.
        Assert.True(locks.Release(LockFixtures.Base012(), LockFixtures.ErikId), "Step 4: the release must return true.");
        Assert.Empty(store.Contents());

        // Step 5: Astrid is granted on s0.
        OrganizeLockResult grantedAtStep5 = locks.Acquire(LockFixtures.Base012(), LockFixtures.AstridId, "Astrid");
        Assert.Equal(OrganizeLockStatus.Granted, grantedAtStep5.Status);
        Assert.Equal(new[] { (LockFixtures.S0, LockFixtures.Astrid3645) }, store.Contents());
    }

    // GW-U-278 (AC-6, LC5): a holder who crashes after a station was added gets an expired leftover on s1 cleared by the next grant at 3630.
    [Fact]
    public void LowerIdStationAddedThenTheHolderCrashesAndTheLeftoverIsCleared()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");

        clock.Elapsed = TimeSpan.FromSeconds(3629);
        OrganizeLockResult refused = locks.Acquire(LockFixtures.Base012(), LockFixtures.AstridId, "Astrid");
        Assert.Equal(OrganizeLockStatus.Refused, refused.Status);
        Assert.Equal("Erik", refused.HolderName);
        Assert.Equal(new[] { (LockFixtures.S1, LockFixtures.Erik3630) }, store.Contents());

        clock.Elapsed = TimeSpan.FromSeconds(3630);
        OrganizeLockResult granted = locks.Acquire(LockFixtures.Base012(), LockFixtures.AstridId, "Astrid");
        Assert.Equal(OrganizeLockStatus.Granted, granted.Status);
        Assert.Equal(new[] { (LockFixtures.S0, LockFixtures.Astrid3660) }, store.Contents());
    }

    // GW-U-278 (AC-6, LC5): the holder releases on the old station s1 at 3605; then Astrid is granted on s0 with no stuck lock.
    [Fact]
    public void LowerIdStationAddedThenTheHolderReleasesOnTheOldStation()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");

        clock.Elapsed = TimeSpan.FromSeconds(3605);
        Assert.True(locks.Release(LockFixtures.Base012(), LockFixtures.ErikId), "The release on the old station must return true.");
        Assert.Empty(store.Contents());

        OrganizeLockResult granted = locks.Acquire(LockFixtures.Base012(), LockFixtures.AstridId, "Astrid");
        Assert.Equal(OrganizeLockStatus.Granted, granted.Status);
        Assert.Equal(new[] { (LockFixtures.S0, LockFixtures.Astrid3635) }, store.Contents());
    }

    // GW-U-279 (AC-6, LC4): Erik holds s0; s0 is destroyed. Renew is NotHeld, and Erik's new Acquire on s1 is granted at 3605.
    [Fact]
    public void LowestIdStationRemovedMainCase()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base012(), LockFixtures.ErikId, "Erik");
        store.Remove(LockFixtures.S0);

        clock.Elapsed = TimeSpan.FromSeconds(3605);
        Assert.Equal(OrganizeLockStatus.NotHeld, locks.Renew(LockFixtures.Base12(), LockFixtures.ErikId, "Erik").Status);
        Assert.Empty(store.Contents());

        OrganizeLockResult erik = locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        Assert.Equal(OrganizeLockStatus.Granted, erik.Status);
        Assert.Equal(new[] { (LockFixtures.S1, LockFixtures.Erik3635) }, store.Contents());

        clock.Elapsed = TimeSpan.FromSeconds(3606);
        OrganizeLockResult astrid = locks.Acquire(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid");
        Assert.Equal(OrganizeLockStatus.Refused, astrid.Status);
        Assert.Equal("Erik", astrid.HolderName);
    }

    // GW-U-279 (AC-6, LC4): after s0 is destroyed, another requester is granted first, and Erik's next Renew is refused naming Astrid.
    [Fact]
    public void LowestIdStationRemovedVariantAnotherRequesterComesFirst()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base012(), LockFixtures.ErikId, "Erik");
        store.Remove(LockFixtures.S0);

        clock.Elapsed = TimeSpan.FromSeconds(3605);
        OrganizeLockResult astrid = locks.Acquire(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid");
        Assert.Equal(OrganizeLockStatus.Granted, astrid.Status);
        Assert.Equal(new[] { (LockFixtures.S1, LockFixtures.Astrid3635) }, store.Contents());

        clock.Elapsed = TimeSpan.FromSeconds(3606);
        OrganizeLockResult erik = locks.Renew(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        Assert.Equal(OrganizeLockStatus.Refused, erik.Status);
        Assert.Equal("Astrid", erik.HolderName);
    }

    // GW-U-279 (AC-6, split): s0 keeps Erik's value but is no longer in the base; Astrid is granted on s1 and s0 is never read or written.
    [Fact]
    public void SplitLeavesTheOldStationValueUntouchedAndUnread()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base012(), LockFixtures.ErikId, "Erik");
        clock.Elapsed = TimeSpan.FromSeconds(3605);
        store.ResetLog();

        OrganizeLockResult astrid = locks.Acquire(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid");

        Assert.Equal(OrganizeLockStatus.Granted, astrid.Status);
        Assert.Equal(new[] { (LockFixtures.S0, LockFixtures.Erik3630), (LockFixtures.S1, LockFixtures.Astrid3635) }, store.Contents());
        Assert.DoesNotContain(store.Calls, call => call.Contains(FakeOrganizeLockStore.KeyOf(LockFixtures.S0), StringComparison.Ordinal));
    }

    // GW-U-280 (AC-6, LC1, LC2): after any grant, Erik's record is on the lock station only. Four seeded states over base012 at 3600.
    [Theory]
    [InlineData("own-live-on-s1-and-s2")]
    [InlineData("foreign-expired-on-s2")]
    [InlineData("foreign-live-on-s2")]
    [InlineData("malformed-on-s2")]
    public void AfterAnyGrantTheRequesterRecordIsOnTheLockStationAndNowhereElse(string seeding)
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        switch (seeding)
        {
            case "own-live-on-s1-and-s2":
                store.Seed(LockFixtures.S1, LockFixtures.Erik3630);
                store.Seed(LockFixtures.S2, LockFixtures.Erik3630);
                break;
            case "foreign-expired-on-s2":
                store.Seed(LockFixtures.S2, LockFixtures.Astrid3590);
                break;
            case "foreign-live-on-s2":
                store.Seed(LockFixtures.S2, LockFixtures.Astrid3620);
                break;
            default:
                store.Seed(LockFixtures.S2, "c:Wood");
                break;
        }

        store.ResetLog();
        OrganizeLockResult result = locks.Acquire(LockFixtures.Base012(), LockFixtures.ErikId, "Erik");

        switch (seeding)
        {
            case "own-live-on-s1-and-s2":
                Assert.Equal(OrganizeLockStatus.Granted, result.Status);
                Assert.Equal(new[] { (LockFixtures.S0, LockFixtures.Erik3630) }, store.Contents());
                break;
            case "foreign-expired-on-s2":
                Assert.Equal(OrganizeLockStatus.Granted, result.Status);
                Assert.Equal(new[] { (LockFixtures.S0, LockFixtures.Erik3630) }, store.Contents());
                break;
            case "foreign-live-on-s2":
                Assert.Equal(OrganizeLockStatus.Refused, result.Status);
                Assert.Equal("Astrid", result.HolderName);
                LockFixtures.AssertNoWrite(store);
                Assert.Equal(new[] { (LockFixtures.S2, LockFixtures.Astrid3620) }, store.Contents());
                return;
            default:
                Assert.Equal(OrganizeLockStatus.Granted, result.Status);
                Assert.Equal(new[] { (LockFixtures.S0, LockFixtures.Erik3630), (LockFixtures.S2, "c:Wood") }, store.Contents());
                break;
        }

        // Every grant: exactly one write, to the lock station, and Erik's value decodes on exactly one station.
        Assert.Equal(1, store.WriteCount);
        Assert.Contains(FakeOrganizeLockStore.WriteCall(LockFixtures.S0), store.Calls);
        Assert.Single(store.Contents(), entry => entry.Value.StartsWith("L1;1923487561234;", StringComparison.Ordinal));
        Assert.Equal(LockFixtures.S0, OrganizeLock.FindLockStation(LockFixtures.Base012()));
    }

    // GW-U-281 (AC-6, LC6): two live locks from two bases merged into base012. Both holders are refused both ways and nothing is written.
    [Fact]
    public void BaseMergeWithTwoLiveLocksRefusesBothHoldersAndWritesNothing()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        store.Seed(LockFixtures.S0, LockFixtures.Erik3630);
        store.Seed(LockFixtures.S1, LockFixtures.Astrid3620);
        store.ResetLog();

        OrganizeLockResult erikAcquire = locks.Acquire(LockFixtures.Base012(), LockFixtures.ErikId, "Erik");
        OrganizeLockResult erikRenew = locks.Renew(LockFixtures.Base012(), LockFixtures.ErikId, "Erik");
        OrganizeLockResult astridAcquire = locks.Acquire(LockFixtures.Base012(), LockFixtures.AstridId, "Astrid");
        OrganizeLockResult astridRenew = locks.Renew(LockFixtures.Base012(), LockFixtures.AstridId, "Astrid");

        Assert.Equal(OrganizeLockStatus.Refused, erikAcquire.Status);
        Assert.Equal("Astrid", erikAcquire.HolderName);
        Assert.Equal(OrganizeLockStatus.Refused, erikRenew.Status);
        Assert.Equal("Astrid", erikRenew.HolderName);
        Assert.Equal(OrganizeLockStatus.Refused, astridAcquire.Status);
        Assert.Equal("Erik", astridAcquire.HolderName);
        Assert.Equal(OrganizeLockStatus.Refused, astridRenew.Status);
        Assert.Equal("Erik", astridRenew.HolderName);
        LockFixtures.AssertNoWrite(store);
        Assert.Equal(new[] { (LockFixtures.S0, LockFixtures.Erik3630), (LockFixtures.S1, LockFixtures.Astrid3620) }, store.Contents());
    }

    // GW-U-281 (AC-6, LC6): at 3630 both merged locks have expired. Bjorn is granted on s0, and the expired leftover on s1 is cleared.
    [Fact]
    public void BaseMergeAfterBothLocksExpireGrantsOnLowestAndClearsTheLeftover()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        store.Seed(LockFixtures.S0, LockFixtures.Erik3630);
        store.Seed(LockFixtures.S1, LockFixtures.Astrid3620);
        clock.Elapsed = TimeSpan.FromSeconds(3630);

        OrganizeLockResult bjorn = locks.Acquire(LockFixtures.Base012(), LockFixtures.BjornId, "Bjorn");

        Assert.Equal(OrganizeLockStatus.Granted, bjorn.Status);
        Assert.Equal(new[] { (LockFixtures.S0, LockFixtures.Bjorn3660) }, store.Contents());
    }
}
