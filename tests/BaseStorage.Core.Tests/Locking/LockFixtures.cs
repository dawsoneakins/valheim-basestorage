namespace BaseStorage.Core.Tests.Locking;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Settings;

/// <summary>
/// The contract 5.1 fixture of the organize lock (ADR-GWIN-027): the stations, the holder ids, and the encoded values as literal strings.
/// The literals are never computed with <c>OrganizeLockCodec</c>, so a codec slip cannot hide itself. Times are in seconds on the clock,
/// and each <c>Erik</c>, <c>Astrid</c> or <c>Bjorn</c> constant is the record of that holder until the given second.
/// </summary>
internal static class LockFixtures
{
    /// <summary>The player id of Erik.</summary>
    public const long ErikId = 1923487561234L;

    /// <summary>The player id of Astrid.</summary>
    public const long AstridId = -6017393921047218534L;

    /// <summary>A third holder, used by the refusal and base-merge rows.</summary>
    public const long BjornId = 77L;

    /// <summary>The station with the lowest id of the 5.1 fixture.</summary>
    public static readonly StationId S0 = new StationId(2718281828L, 900u);

    /// <summary>The middle station of the 5.1 fixture; the lock station of <c>base12</c>.</summary>
    public static readonly StationId S1 = new StationId(3141592653L, 17u);

    /// <summary>The highest station of the 5.1 fixture.</summary>
    public static readonly StationId S2 = new StationId(3141592653L, 18u);

    /// <summary>The snapshot of <see cref="S0"/>.</summary>
    public static readonly StationSnapshot Station0 = new StationSnapshot(S0, new WorldPosition(4f, 31.25f, -840.75f), 20f);

    /// <summary>The snapshot of <see cref="S1"/>.</summary>
    public static readonly StationSnapshot Station1 = new StationSnapshot(S1, new WorldPosition(12.5f, 31.25f, -840.75f), 20f);

    /// <summary>The snapshot of <see cref="S2"/>.</summary>
    public static readonly StationSnapshot Station2 = new StationSnapshot(S2, new WorldPosition(30f, 31.25f, -840.75f), 20f);

    /// <summary>Erik until 3630 s.</summary>
    public const string Erik3630 = "L1;1923487561234;36300000000;Erik";

    /// <summary>Erik until 3635 s.</summary>
    public const string Erik3635 = "L1;1923487561234;36350000000;Erik";

    /// <summary>Erik until 3640 s.</summary>
    public const string Erik3640 = "L1;1923487561234;36400000000;Erik";

    /// <summary>Erik until 3650 s.</summary>
    public const string Erik3650 = "L1;1923487561234;36500000000;Erik";

    /// <summary>Erik until 3601 s.</summary>
    public const string Erik3601 = "L1;1923487561234;36010000000;Erik";

    /// <summary>Erik until 3660 s.</summary>
    public const string Erik3660 = "L1;1923487561234;36600000000;Erik";

    /// <summary>Erik until 3670 s.</summary>
    public const string Erik3670 = "L1;1923487561234;36700000000;Erik";

    /// <summary>Astrid until 3590 s; already expired at 3600 s.</summary>
    public const string Astrid3590 = "L1;-6017393921047218534;35900000000;Astrid";

    /// <summary>Astrid until 3620 s.</summary>
    public const string Astrid3620 = "L1;-6017393921047218534;36200000000;Astrid";

    /// <summary>Astrid until 3630 s.</summary>
    public const string Astrid3630 = "L1;-6017393921047218534;36300000000;Astrid";

    /// <summary>Astrid until 3631 s.</summary>
    public const string Astrid3631 = "L1;-6017393921047218534;36310000000;Astrid";

    /// <summary>Astrid until 3635 s.</summary>
    public const string Astrid3635 = "L1;-6017393921047218534;36350000000;Astrid";

    /// <summary>Astrid until 3645 s.</summary>
    public const string Astrid3645 = "L1;-6017393921047218534;36450000000;Astrid";

    /// <summary>Astrid until 3650 s.</summary>
    public const string Astrid3650 = "L1;-6017393921047218534;36500000000;Astrid";

    /// <summary>Astrid until 3660 s.</summary>
    public const string Astrid3660 = "L1;-6017393921047218534;36600000000;Astrid";

    /// <summary>Bjorn until 3660 s.</summary>
    public const string Bjorn3660 = "L1;77;36600000000;Bjorn";

    /// <summary>The unsorted list <c>[s2, s1]</c> of contract 5.1; its lock station is s1.</summary>
    /// <returns>A new array of the two stations.</returns>
    public static StationSnapshot[] Base12() => new[] { Station2, Station1 };

    /// <summary>The list <c>[s1, s2]</c>, in ascending order; its lock station is s1.</summary>
    /// <returns>A new array of the two stations.</returns>
    public static StationSnapshot[] Base12Ascending() => new[] { Station1, Station2 };

    /// <summary>The unsorted list <c>[s2, s0, s1]</c> of contract 5.1; its lock station is s0.</summary>
    /// <returns>A new array of the three stations.</returns>
    public static StationSnapshot[] Base012() => new[] { Station2, Station0, Station1 };

    /// <summary>Resolves a named layout to its station list: <c>base12</c>, <c>ascending</c> or <c>base012</c>.</summary>
    /// <param name="layout">The layout name.</param>
    /// <returns>A new array of the stations of that layout.</returns>
    public static StationSnapshot[] Layout(string layout) => layout switch
    {
        "base12" => Base12(),
        "ascending" => Base12Ascending(),
        "base012" => Base012(),
        _ => throw new ArgumentException("Unknown layout.", nameof(layout)),
    };

    /// <summary>Resolves a station name of the fixture: <c>s0</c>, <c>s1</c> or <c>s2</c>.</summary>
    /// <param name="name">The station name.</param>
    /// <returns>The station id.</returns>
    public static StationId Named(string name) => name switch
    {
        "s0" => S0,
        "s1" => S1,
        "s2" => S2,
        _ => throw new ArgumentException("Unknown station.", nameof(name)),
    };

    /// <summary>Asserts that the log holds exactly one write, to the given station, and no clear.</summary>
    /// <param name="store">The store under test.</param>
    /// <param name="station">The only station that may be written.</param>
    public static void AssertSingleWriteTo(FakeOrganizeLockStore store, StationId station)
    {
        Xunit.Assert.Equal(1, store.WriteCount);
        Xunit.Assert.Equal(0, store.ClearCount);
        Xunit.Assert.Equal(new[] { FakeOrganizeLockStore.WriteCall(station) }, WriteEntries(store));
    }

    /// <summary>Asserts that the store was neither written nor cleared since the last reset.</summary>
    /// <param name="store">The store under test.</param>
    public static void AssertNoWrite(FakeOrganizeLockStore store)
    {
        Xunit.Assert.Equal(0, store.WriteCount);
        Xunit.Assert.Equal(0, store.ClearCount);
    }

    /// <summary>Builds a fresh store, a counting clock at 3600 s, a settings source with <c>OrganizeLockSeconds</c> set, and the service.</summary>
    /// <param name="seconds">The value of <c>OrganizeLockSeconds</c>; 30 by default.</param>
    /// <returns>The store, the clock, the settings source (with its call counter) and the service.</returns>
    public static (FakeOrganizeLockStore Store, CountingClock Clock, FakeSettingsSource Settings, OrganizeLock Locks) Make(int seconds = 30)
    {
        var store = new FakeOrganizeLockStore();
        var clock = new CountingClock();
        var settings = new FakeSettingsSource().WithInt(BaseStorage.Core.Settings.SettingKeys.OrganizeLockSeconds, seconds);
        return (store, clock, settings, new OrganizeLock(store, clock, new BaseStorageSettings(settings)));
    }

    private static string[] WriteEntries(FakeOrganizeLockStore store)
    {
        var writes = new List<string>();
        foreach (string call in store.Calls)
        {
            if (call.StartsWith("Write:", StringComparison.Ordinal))
            {
                writes.Add(call);
            }
        }

        return writes.ToArray();
    }
}
