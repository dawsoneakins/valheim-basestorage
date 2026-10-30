namespace BaseStorage.Core.Tests.Locking;

using System;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Ui;

/// <summary>AC-3 (contract 3.2, 4.2, 5.3 row 2 and 5.2): a refusal names the holder and maps to the notice through the one strings table.</summary>
public sealed class LockRefusalTests
{
    // GW-U-270 (AC-3): a refused Acquire at 3610 carries the holder, the record, the message key and the English notice, and writes nothing.
    [Fact]
    public void AcquireRefusalCarriesTheHolderAndMapsToTheMessageThroughTheTable()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        store.ResetLog();
        clock.Elapsed = TimeSpan.FromSeconds(3610);

        OrganizeLockResult refused = locks.Acquire(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid");

        Assert.Equal(OrganizeLockStatus.Refused, refused.Status);
        Assert.False(refused.IsGranted);
        Assert.Equal("Erik", refused.HolderName);
        Assert.Equal(new OrganizeLockRecord(LockFixtures.ErikId, "Erik", TimeSpan.FromSeconds(3630)), refused.Record);
        Assert.Equal("message.base.in-progress", refused.MessageKey);
        Assert.Equal("Organize in progress by Erik", refused.FormatMessage(StringTable.English));
        Assert.Equal(StringTable.English.Format(StringKeys.MessageBaseInProgress, "Erik"), refused.FormatMessage(StringTable.English));
        LockFixtures.AssertNoWrite(store);
    }

    // GW-U-270 (AC-3): a refused Renew at 3620 gives the same refusal: the holder Erik, the same key and the same notice.
    [Fact]
    public void RenewRefusalGivesTheSameMessage()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        clock.Elapsed = TimeSpan.FromSeconds(3600);
        locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik");
        store.ResetLog();
        clock.Elapsed = TimeSpan.FromSeconds(3620);

        OrganizeLockResult refused = locks.Renew(LockFixtures.Base12(), LockFixtures.AstridId, "Astrid");

        Assert.Equal(OrganizeLockStatus.Refused, refused.Status);
        Assert.Equal("Erik", refused.HolderName);
        Assert.Equal("message.base.in-progress", refused.MessageKey);
        Assert.Equal("Organize in progress by Erik", refused.FormatMessage(StringTable.English));
        LockFixtures.AssertNoWrite(store);
    }

    // GW-U-273 (AC-3): with Erik live on s1 and Astrid live on s2, the refusal names the holder on the lowest station, Erik.
    [Fact]
    public void RefusalNamesTheHolderOnTheLowestStationWithAForeignLiveLock()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        store.Seed(LockFixtures.S1, LockFixtures.Erik3630);
        store.Seed(LockFixtures.S2, LockFixtures.Astrid3620);
        store.ResetLog();

        OrganizeLockResult result = locks.Acquire(LockFixtures.Base12(), LockFixtures.BjornId, "Bjorn");

        Assert.Equal(OrganizeLockStatus.Refused, result.Status);
        Assert.Equal("Erik", result.HolderName);
        LockFixtures.AssertNoWrite(store);
    }

    // GW-U-273 (AC-3): with the seeds swapped, the lowest foreign live station is s1 again, so the refusal names Astrid.
    [Fact]
    public void RefusalNamesTheHolderOfTheLowestStationWhenTheSeedsAreSwapped()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        store.Seed(LockFixtures.S1, LockFixtures.Astrid3620);
        store.Seed(LockFixtures.S2, LockFixtures.Erik3630);
        store.ResetLog();

        OrganizeLockResult result = locks.Acquire(LockFixtures.Base12(), LockFixtures.BjornId, "Bjorn");

        Assert.Equal(OrganizeLockStatus.Refused, result.Status);
        Assert.Equal("Astrid", result.HolderName);
        LockFixtures.AssertNoWrite(store);
    }
}
