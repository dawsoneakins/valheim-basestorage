namespace BaseStorage.Core.Tests.Locking;

using System;
using BaseStorage.Core.Locking;

/// <summary>AC-1 and LK6 (ADR-GWIN-026): an injected store, clock or settings failure propagates unchanged, and a failed write is followed by no clear.</summary>
public sealed class LockFailureTests
{
    // GW-U-265 (LK6): a store read, clock or settings failure propagates out of Acquire and Renew as the same instance.
    [Theory]
    [InlineData("acquire", "store")]
    [InlineData("acquire", "clock")]
    [InlineData("acquire", "settings")]
    [InlineData("renew", "store")]
    [InlineData("renew", "clock")]
    [InlineData("renew", "settings")]
    public void InjectedFailuresPropagateUnchangedFromAcquireAndRenew(string operation, string fault)
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        var thrown = new InvalidOperationException("injected " + fault);
        switch (fault)
        {
            case "store":
                store.ThrowOnRead = thrown;
                break;
            case "clock":
                clock.Throws = thrown;
                break;
            default:
                settings.Throws = thrown;
                break;
        }

        Exception? caught = operation == "acquire"
            ? Record.Exception(() => locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik"))
            : Record.Exception(() => locks.Renew(LockFixtures.Base12(), LockFixtures.ErikId, "Erik"));

        Assert.Same(thrown, caught);
    }

    // GW-U-265 (LK6): a store read failure propagates out of Release as the same instance.
    [Fact]
    public void StoreReadFailurePropagatesFromRelease()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        var thrown = new InvalidOperationException("injected read");
        store.ThrowOnRead = thrown;

        Exception? caught = Record.Exception(() => locks.Release(LockFixtures.Base12(), LockFixtures.ErikId));

        Assert.Same(thrown, caught);
    }

    // GW-U-265 (ADR-GWIN-026): a write failure on a granting Acquire propagates, and no Clear follows, even with a stale own record present.
    [Fact]
    public void WriteFailureOnAGrantPropagatesAndNoClearFollows()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        store.Seed(LockFixtures.S2, LockFixtures.Erik3630);
        var thrown = new InvalidOperationException("injected write");
        store.ThrowOnWrite = thrown;

        Exception? caught = Record.Exception(() => locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Erik"));

        Assert.Same(thrown, caught);
        Assert.Equal(0, store.ClearCount);
        Assert.Equal(new[] { (LockFixtures.S2, LockFixtures.Erik3630) }, store.Contents());
    }
}
