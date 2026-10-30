namespace BaseStorage.Core.Locking;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The stateless organize lock of one base (contract section 4). At most one holder at a time: a requester is refused while another
/// holder has a live record in the base. The record lives on the base's lowest-id station, which is the only station written.
/// Expiry and the clock decrease rule come from <see cref="IClock"/>; the duration comes from <c>OrganizeLockSeconds</c>.
/// Core operations are not atomic against the store (LK7): callers serialise the calls, and host-authoritative ordering is a
/// Plugin concern of later stories. The runner stops its operation on <c>Refused</c> or <c>NotHeld</c> from <see cref="Renew"/>.
/// </summary>
public sealed class OrganizeLock
{
    private readonly IOrganizeLockStore store;
    private readonly IClock clock;
    private readonly BaseStorageSettings settings;

    /// <summary>Creates the lock service over a store, a clock and the settings. The constructor reads nothing.</summary>
    /// <param name="store">The per-station store of the lock values.</param>
    /// <param name="clock">The only time source.</param>
    /// <param name="settings">The settings that give the lock duration.</param>
    public OrganizeLock(IOrganizeLockStore store, IClock clock, BaseStorageSettings settings)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>Finds the lock station of a base: the station with the lowest id.</summary>
    /// <param name="stations">The stations of one base; not null, not empty, no null element.</param>
    /// <returns>The lowest <see cref="StationId"/> in the list.</returns>
    public static StationId FindLockStation(IReadOnlyList<StationSnapshot> stations)
    {
        ValidateStations(stations);
        return DistinctSorted(stations)[0];
    }

    /// <summary>Acquires the lock for a holder: granted when free, expired or already the holder's, refused while a foreign holder is live.</summary>
    /// <param name="stations">The stations of one base; not null, not empty, no null element.</param>
    /// <param name="holderId">The holder's game player id; not zero.</param>
    /// <param name="holderName">The holder's display name; not blank, no control characters.</param>
    /// <returns>The granted or refused result.</returns>
    public OrganizeLockResult Acquire(IReadOnlyList<StationSnapshot> stations, long holderId, string holderName) =>
        Take(stations, holderId, holderName, requireOwnLive: false);

    /// <summary>Renews the holder's own live lock, moving its expiry forward; <c>NotHeld</c> when the holder has no live lock.</summary>
    /// <param name="stations">The stations of one base; not null, not empty, no null element.</param>
    /// <param name="holderId">The holder's game player id; not zero.</param>
    /// <param name="holderName">The holder's display name; not blank, no control characters.</param>
    /// <returns>The granted, refused or not-held result.</returns>
    public OrganizeLockResult Renew(IReadOnlyList<StationSnapshot> stations, long holderId, string holderName) =>
        Take(stations, holderId, holderName, requireOwnLive: true);

    /// <summary>Releases the holder's own records (live or expired) on the stations of a base; never clears a foreign or malformed value.</summary>
    /// <param name="stations">The stations of one base; not null, not empty, no null element.</param>
    /// <param name="holderId">The holder's game player id; not zero.</param>
    /// <returns>True when at least one value was cleared; otherwise false.</returns>
    public bool Release(IReadOnlyList<StationSnapshot> stations, long holderId)
    {
        ValidateStations(stations);
        ValidateHolderId(holderId);
        var cleared = false;
        foreach (var id in DistinctSorted(stations))
        {
            var record = OrganizeLockCodec.Decode(store.Read(id));
            if (record is not null && record.HolderId == holderId)
            {
                store.Clear(id);
                cleared = true;
            }
        }

        return cleared;
    }

    private static void ValidateStations(IReadOnlyList<StationSnapshot> stations)
    {
        if (stations is null)
        {
            throw new ArgumentNullException(nameof(stations));
        }

        if (stations.Count == 0)
        {
            throw new ArgumentException("The stations must not be empty.", nameof(stations));
        }

        for (var i = 0; i < stations.Count; i++)
        {
            if (stations[i] is null)
            {
                throw new ArgumentException("The stations must not contain a null element.", nameof(stations));
            }
        }
    }

    private static void ValidateHolderId(long holderId)
    {
        if (holderId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(holderId), "The holder id must not be zero.");
        }
    }

    private static List<StationId> DistinctSorted(IReadOnlyList<StationSnapshot> stations)
    {
        var ids = new List<StationId>(stations.Count);
        for (var i = 0; i < stations.Count; i++)
        {
            ids.Add(stations[i].Id);
        }

        ids.Sort((a, b) => a.CompareTo(b));
        for (var i = ids.Count - 1; i > 0; i--)
        {
            if (ids[i] == ids[i - 1])
            {
                ids.RemoveAt(i);
            }
        }

        return ids;
    }

    private static bool IsLive(OrganizeLockRecord? record, TimeSpan now, TimeSpan limit) =>
        record is not null && now < record.ExpiresAt && record.ExpiresAt <= limit;

    private OrganizeLockResult Take(IReadOnlyList<StationSnapshot> stations, long holderId, string holderName, bool requireOwnLive)
    {
        ValidateStations(stations);
        ValidateHolderId(holderId);
        NameRule.Require(holderName, nameof(holderName));

        var now = clock.Elapsed;
        var seconds = settings.OrganizeLockSeconds;
        var durationTicks = (seconds < 1 ? 1L : seconds) * TimeSpan.TicksPerSecond;
        var limit = now.Ticks > TimeSpan.MaxValue.Ticks - durationTicks ? TimeSpan.MaxValue : TimeSpan.FromTicks(now.Ticks + durationTicks);

        var ids = DistinctSorted(stations);
        var records = new OrganizeLockRecord?[ids.Count];
        var anyOwnLive = false;
        for (var i = 0; i < ids.Count; i++)
        {
            var record = OrganizeLockCodec.Decode(store.Read(ids[i]));
            records[i] = record;
            if (!IsLive(record, now, limit))
            {
                continue;
            }

            if (record!.HolderId != holderId)
            {
                return OrganizeLockResult.Refused(record);
            }

            anyOwnLive = true;
        }

        // Renew (requireOwnLive) must find a live lock of its own; Acquire may take a free or expired one.
        if (requireOwnLive && !anyOwnLive)
        {
            return OrganizeLockResult.NotHeld;
        }

        var written = new OrganizeLockRecord(holderId, holderName, limit);
        store.Write(ids[0], OrganizeLockCodec.Encode(written));
        for (var i = 1; i < ids.Count; i++)
        {
            var record = records[i];
            if (record is not null && (record.HolderId == holderId || !IsLive(record, now, limit)))
            {
                store.Clear(ids[i]);
            }
        }

        return OrganizeLockResult.Granted(written);
    }
}
