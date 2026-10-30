namespace BaseStorage.Core.Tests.Locking;

using System;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Persistence;

/// <summary>AC-2 (contract 4.1, 5.4, LK2): the clock is read once per call, a clock decrease fails closed, and the limit saturates.</summary>
public sealed class LockClockTests
{
    // GW-U-268 (LK2): each Acquire or Renew outcome reads the clock exactly once; Release reads it not at all.
    [Theory]
    [InlineData("granted")]
    [InlineData("refused")]
    [InlineData("not-held")]
    [InlineData("release")]
    public void TheClockIsReadOncePerCallAndNeverByRelease(string outcome)
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        if (outcome == "refused" || outcome == "release")
        {
            locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        }

        store.ResetLog();
        int before = clock.Reads;

        switch (outcome)
        {
            case "granted":
                Assert.Equal(OrganizeLockStatus.Granted, locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik").Status);
                break;
            case "refused":
                Assert.Equal(OrganizeLockStatus.Refused, locks.Acquire(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid").Status);
                break;
            case "not-held":
                Assert.Equal(OrganizeLockStatus.NotHeld, locks.Renew(LockFixtures.Base12(), LockFixtures.ErikId, "Erik").Status);
                break;
            default:
                Assert.True(locks.Release(LockFixtures.Base12(), LockFixtures.ErikId));
                break;
        }

        int expectedReads = outcome == "release" ? 0 : 1;
        Assert.Equal(before + expectedReads, clock.Reads);
    }

    // GW-U-268 (LK2, fail closed): Erik until 3630 read at 3590 (the clock went back) is not live, so Astrid is granted until 3620.
    [Fact]
    public void ClockDecreaseFailsClosed()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");

        clock.Elapsed = TimeSpan.FromSeconds(3590);
        OrganizeLockResult result = locks.Acquire(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid");

        Assert.Equal(OrganizeLockStatus.Granted, result.Status);
        Assert.Equal(new[] { (LockFixtures.S1, LockFixtures.Astrid3620) }, store.Contents());
    }

    // GW-U-268 (contract 5.4): on a fresh store with Erik until 3630, the clock at 3600 leaves exactly 30 s, which is live, so Astrid is refused.
    [Fact]
    public void ALiveRecordWithExactlyTheDurationRemainingRefuses()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        store.Seed(LockFixtures.S1, LockFixtures.Erik3630);
        clock.Elapsed = TimeSpan.FromSeconds(3600);

        OrganizeLockResult result = locks.Acquire(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid");

        Assert.Equal(OrganizeLockStatus.Refused, result.Status);
        Assert.Equal("Erik", result.HolderName);
    }

    // GW-U-268 (LK2, saturation): at TimeSpan.MaxValue minus 10 s, the expiry saturates at TimeSpan.MaxValue with no OverflowException.
    [Fact]
    public void TheLimitSaturatesAtTimeSpanMaxValue()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.MaxValue - TimeSpan.FromSeconds(10);

        OrganizeLockResult granted = locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");

        Assert.Equal(OrganizeLockStatus.Granted, granted.Status);
        Assert.Equal(new OrganizeLockRecord(LockFixtures.ErikId, "Erik", TimeSpan.MaxValue), granted.Record);
        Assert.Equal(new[] { (LockFixtures.S1, "L1;1923487561234;9223372036854775807;Erik") }, store.Contents());

        OrganizeLockResult refused = locks.Acquire(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid");
        Assert.Equal(OrganizeLockStatus.Refused, refused.Status);
    }
}
