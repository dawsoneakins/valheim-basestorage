namespace BaseStorage.Core.Tests.Locking;

using System;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Settings;

/// <summary>AC-1 (contract 4.2 and 5.2): argument rules, their order, and that nothing is read when an argument is rejected.</summary>
public sealed class LockArgumentTests
{
    // GW-U-262 (AC-1): every throwing row of contract 5.2 and the order cases. Each row names its exact exception type and ParamName,
    // and asserts that the store log is empty, the clock was not read and the settings were not read.
    [Theory]
    [InlineData("acquire null stations")]
    [InlineData("acquire empty stations")]
    [InlineData("acquire null element")]
    [InlineData("acquire zero holder")]
    [InlineData("acquire empty name")]
    [InlineData("acquire blank name")]
    [InlineData("acquire control character name")]
    [InlineData("acquire null name")]
    [InlineData("renew null stations")]
    [InlineData("renew empty stations")]
    [InlineData("renew null element")]
    [InlineData("renew zero holder")]
    [InlineData("renew blank name")]
    [InlineData("renew null name")]
    [InlineData("release null stations")]
    [InlineData("release empty stations")]
    [InlineData("release null element")]
    [InlineData("release zero holder")]
    [InlineData("find null stations")]
    [InlineData("find empty stations")]
    [InlineData("find null element")]
    [InlineData("order: null stations, zero holder, null name")]
    [InlineData("order: zero holder, null name")]
    public void ArgumentFailuresNameTheExactTypeAndParamNameAndReadNothing(string name)
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        (Action act, Type expected, string param) = Row(name, locks);

        Exception? thrown = Record.Exception(act);

        Assert.NotNull(thrown);
        Assert.Equal(expected, thrown!.GetType());
        Assert.Equal(param, ((ArgumentException)thrown).ParamName);
        Assert.Empty(store.Calls);
        Assert.Equal(0, clock.Reads);
        Assert.Equal(0, settings.IntCalls);
    }

    // GW-U-262 (AC-1): the constructor rejects a null store, clock or settings by name, and reads nothing when it is given valid arguments.
    [Theory]
    [InlineData("store")]
    [InlineData("clock")]
    [InlineData("settings")]
    public void ConstructorRejectsANullDependencyByName(string missing)
    {
        var store = new FakeOrganizeLockStore();
        var clock = new CountingClock();
        var settings = new BaseStorageSettings(new FakeSettingsSource());

        Action act;
        switch (missing)
        {
            case "store":
                act = () => _ = new OrganizeLock(null!, clock, settings);
                break;
            case "clock":
                act = () => _ = new OrganizeLock(store, null!, settings);
                break;
            default:
                act = () => _ = new OrganizeLock(store, clock, null!);
                break;
        }

        ArgumentNullException thrown = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal(missing, thrown.ParamName);
    }

    // GW-U-262 (AC-1): the constructor reads nothing, so the store, the clock and the settings are untouched after it.
    [Fact]
    public void ConstructorReadsNothing()
    {
        var store = new FakeOrganizeLockStore();
        var clock = new CountingClock();
        var source = new FakeSettingsSource();

        _ = new OrganizeLock(store, clock, new BaseStorageSettings(source));

        Assert.Empty(store.Calls);
        Assert.Equal(0, clock.Reads);
        Assert.Equal(0, source.IntCalls);
    }

    // GW-U-262 (AC-1): holder names that break the rule are rejected by ParamName "holderName". The values are built here, so the test
    // list holds only plain names.
    [Theory]
    [InlineData("blank name")]
    [InlineData("control character name")]
    public void HolderNameRuleRejectsBlankAndControlCharacters(string name)
    {
        var (store, clock, settings, locks) = LockFixtures.Make();
        string holder = name == "blank name" ? " " : "Er\u0001ik";

        ArgumentException thrown = Assert.Throws<ArgumentException>(() => locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, holder));

        Assert.Equal("holderName", thrown.ParamName);
        Assert.Empty(store.Calls);
    }

    // GW-U-262 (AC-1): Renew and Release reject a zero holder id and a null station element before they read anything.
    [Fact]
    public void RenewAndReleaseRejectZeroHolderBeforeAnyRead()
    {
        var (store, clock, settings, locks) = LockFixtures.Make();

        ArgumentOutOfRangeException renew = Assert.Throws<ArgumentOutOfRangeException>(() => locks.Renew(LockFixtures.Base12(), 0L, "Erik"));
        ArgumentOutOfRangeException release = Assert.Throws<ArgumentOutOfRangeException>(() => locks.Release(LockFixtures.Base12(), 0L));

        Assert.Equal("holderId", renew.ParamName);
        Assert.Equal("holderId", release.ParamName);
        Assert.Empty(store.Calls);
        Assert.Equal(0, clock.Reads);
    }

    private static (Action Act, Type Expected, string Param) Row(string name, OrganizeLock locks)
    {
        StationSnapshot[] stationsWithNull = { LockFixtures.Station1, null! };
        return name switch
        {
            "acquire null stations" => Entry(() => locks.Acquire(null!, LockFixtures.ErikId, "Erik"), typeof(ArgumentNullException), "stations"),
            "acquire empty stations" => Entry(() => locks.Acquire(Array.Empty<StationSnapshot>(), LockFixtures.ErikId, "Erik"), typeof(ArgumentException), "stations"),
            "acquire null element" => Entry(() => locks.Acquire(stationsWithNull, LockFixtures.ErikId, "Erik"), typeof(ArgumentException), "stations"),
            "acquire zero holder" => Entry(() => locks.Acquire(LockFixtures.Base12(), 0L, "Erik"), typeof(ArgumentOutOfRangeException), "holderId"),
            "acquire empty name" => Entry(() => locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, string.Empty), typeof(ArgumentException), "holderName"),
            "acquire blank name" => Entry(() => locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, " "), typeof(ArgumentException), "holderName"),
            "acquire control character name" => Entry(() => locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, "Er\u0001ik"), typeof(ArgumentException), "holderName"),
            "acquire null name" => Entry(() => locks.Acquire(LockFixtures.Base12(), LockFixtures.ErikId, null!), typeof(ArgumentNullException), "holderName"),
            "renew null stations" => Entry(() => locks.Renew(null!, LockFixtures.ErikId, "Erik"), typeof(ArgumentNullException), "stations"),
            "renew empty stations" => Entry(() => locks.Renew(Array.Empty<StationSnapshot>(), LockFixtures.ErikId, "Erik"), typeof(ArgumentException), "stations"),
            "renew null element" => Entry(() => locks.Renew(stationsWithNull, LockFixtures.ErikId, "Erik"), typeof(ArgumentException), "stations"),
            "renew zero holder" => Entry(() => locks.Renew(LockFixtures.Base12(), 0L, "Erik"), typeof(ArgumentOutOfRangeException), "holderId"),
            "renew blank name" => Entry(() => locks.Renew(LockFixtures.Base12(), LockFixtures.ErikId, " "), typeof(ArgumentException), "holderName"),
            "renew null name" => Entry(() => locks.Renew(LockFixtures.Base12(), LockFixtures.ErikId, null!), typeof(ArgumentNullException), "holderName"),
            "release null stations" => Entry(() => locks.Release(null!, LockFixtures.ErikId), typeof(ArgumentNullException), "stations"),
            "release empty stations" => Entry(() => locks.Release(Array.Empty<StationSnapshot>(), LockFixtures.ErikId), typeof(ArgumentException), "stations"),
            "release null element" => Entry(() => locks.Release(stationsWithNull, LockFixtures.ErikId), typeof(ArgumentException), "stations"),
            "release zero holder" => Entry(() => locks.Release(LockFixtures.Base12(), 0L), typeof(ArgumentOutOfRangeException), "holderId"),
            "find null stations" => Entry(() => OrganizeLock.FindLockStation(null!), typeof(ArgumentNullException), "stations"),
            "find empty stations" => Entry(() => OrganizeLock.FindLockStation(Array.Empty<StationSnapshot>()), typeof(ArgumentException), "stations"),
            "find null element" => Entry(() => OrganizeLock.FindLockStation(stationsWithNull), typeof(ArgumentException), "stations"),
            "order: null stations, zero holder, null name" => Entry(() => locks.Acquire(null!, 0L, null!), typeof(ArgumentNullException), "stations"),
            "order: zero holder, null name" => Entry(() => locks.Acquire(LockFixtures.Base12(), 0L, null!), typeof(ArgumentOutOfRangeException), "holderId"),
            _ => throw new ArgumentException("Unknown row.", nameof(name)),
        };
    }

    private static (Action Act, Type Expected, string Param) Entry(Action act, Type expected, string param) => (act, expected, param);
}
