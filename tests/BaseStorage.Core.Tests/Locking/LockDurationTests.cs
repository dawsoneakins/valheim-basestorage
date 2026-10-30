namespace BaseStorage.Core.Tests.Locking;

using System;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.Settings;

/// <summary>AC-2 (contract 5.4 settings table, LK3): the duration comes from OrganizeLockSeconds, read once per call, never cached.</summary>
public sealed class LockDurationTests
{
    // GW-U-267 (LK3): one OrganizeLock instance reads the current setting on each call. Setting 60 gives 3660, then 30 gives 3631 on the next call.
    [Fact]
    public void ChangedSettingAppliesToTheNextCallOnTheSameInstance()
    {
        var store = new FakeOrganizeLockStore();
        var clock = new CountingClock();
        var source = new FakeSettingsSource().WithInt(SettingKeys.OrganizeLockSeconds, 60);
        var locks = new OrganizeLock(store, clock, new BaseStorageSettings(source));

        OrganizeLockResult first = locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        Assert.Equal(OrganizeLockStatus.Granted, first.Status);
        Assert.Equal(new OrganizeLockRecord(LockFixtures.ErikId, "Erik", TimeSpan.FromSeconds(3660)), first.Record);
        Assert.Equal(1, source.IntCalls);

        source.WithInt(SettingKeys.OrganizeLockSeconds, 30);
        clock.Elapsed = TimeSpan.FromSeconds(3601);
        OrganizeLockResult second = locks.Acquire(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid");

        Assert.Equal(OrganizeLockStatus.Granted, second.Status);
        Assert.Equal(new[] { (LockFixtures.S1, LockFixtures.Astrid3631) }, store.Contents());
        Assert.Equal(2, source.IntCalls);
    }

    // GW-U-267 (LK3, normalisation): a setting of 0 or -5 is treated as 1 s, so Erik is live to 3601 and Astrid is refused at 3600.5.
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void NonPositiveDurationIsNormalisedToOneSecond(int seconds)
    {
        var (store, clock, settings, locks) = LockFixtures.Make(seconds);
        clock.Elapsed = TimeSpan.FromSeconds(3600);

        OrganizeLockResult granted = locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        Assert.Equal(new OrganizeLockRecord(LockFixtures.ErikId, "Erik", TimeSpan.FromSeconds(3601)), granted.Record);
        Assert.Equal(new[] { (LockFixtures.S1, LockFixtures.Erik3601) }, store.Contents());

        clock.Elapsed = TimeSpan.FromSeconds(3600.5);
        OrganizeLockResult refused = locks.Acquire(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid");
        Assert.Equal(OrganizeLockStatus.Refused, refused.Status);
        Assert.Equal("Erik", refused.HolderName);
    }

    // GW-U-267 (AC-2): with no setting in the source, the default of 30 s applies.
    [Fact]
    public void TheDefaultDurationIsThirtySeconds()
    {
        var store = new FakeOrganizeLockStore();
        var clock = new CountingClock();
        var locks = new OrganizeLock(store, clock, new BaseStorageSettings(new FakeSettingsSource()));

        OrganizeLockResult result = locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");

        Assert.Equal(new OrganizeLockRecord(LockFixtures.ErikId, "Erik", TimeSpan.FromSeconds(3630)), result.Record);
    }

    // GW-U-267 (LK3): IntCalls rises by exactly one per Acquire, Renew and Release call that reads it; Release reads none.
    [Fact]
    public void SettingsAreReadOncePerAcquireAndRenewAndNeverByRelease()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();

        locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        Assert.Equal(1, settings.IntCalls);

        locks.Renew(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        Assert.Equal(2, settings.IntCalls);

        locks.Release(LockFixtures.Base12(), LockFixtures.ErikId);
        Assert.Equal(2, settings.IntCalls);
    }
}
