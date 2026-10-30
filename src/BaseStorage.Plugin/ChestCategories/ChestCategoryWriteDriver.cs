namespace BaseStorage.Plugin.ChestCategories;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.ChestCategories;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.Reservations;

/// <summary>
/// The game-free driver that writes a chest category through the reservation adapter (contract S-E004-003 section 3.2). Every
/// write reserves the chest with <see cref="ReservationDriver{TChest}"/>, writes only while <c>CanWrite</c> is true, releases
/// at once, and reports its end through one callback. A chest the local peer owns completes inside the call; a chest owned by
/// another peer stays pending and completes from <see cref="Tick"/> when the owner answers, or is refused on deny, timeout or
/// loss. Decisions come from <see cref="CategoryWriteRules"/> and <see cref="ChestCategoryReader"/>.
/// </summary>
/// <typeparam name="TChest">The chest type of the game.</typeparam>
internal sealed class ChestCategoryWriteDriver<TChest>
    where TChest : class
{
    /// <summary>The reservation operation label of a category write (a Debug label, not user-facing text).</summary>
    internal const string Operation = "category";

    private readonly ReservationDriver<TChest> reservations;
    private readonly IChestCategoryStorage<TChest> storage;
    private readonly IClock clock;
    private readonly int reserveTimeoutMs;
    private readonly Action<CategoryWriteResult>? onFinished;
    private readonly Dictionary<ContainerId, Entry> pending = new Dictionary<ContainerId, Entry>();
    private readonly List<ContainerId> scratch = new List<ContainerId>();

    /// <summary>Creates a driver over the reservation driver, the storage port and a clock.</summary>
    /// <param name="reservations">The reservation driver that grants and releases chests.</param>
    /// <param name="storage">The storage port.</param>
    /// <param name="clock">The only time source.</param>
    /// <param name="reserveTimeoutMs">The reservation timeout; values below 1 are read as 1.</param>
    /// <param name="onFinished">Called once for every write that ends, applied or refused; may be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reservations"/>, <paramref name="storage"/> or <paramref name="clock"/> is null.</exception>
    internal ChestCategoryWriteDriver(
        ReservationDriver<TChest> reservations,
        IChestCategoryStorage<TChest> storage,
        IClock clock,
        int reserveTimeoutMs,
        Action<CategoryWriteResult>? onFinished)
    {
        if (reservations is null)
        {
            throw new ArgumentNullException(nameof(reservations));
        }

        if (storage is null)
        {
            throw new ArgumentNullException(nameof(storage));
        }

        if (clock is null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        this.reservations = reservations;
        this.storage = storage;
        this.clock = clock;
        this.reserveTimeoutMs = Math.Max(1, reserveTimeoutMs);
        this.onFinished = onFinished;
    }

    /// <summary>Gets the number of writes that wait for their reservation.</summary>
    internal int PendingCount => pending.Count;

    /// <summary>Tells whether a write for a chest is waiting for its reservation.</summary>
    /// <param name="chest">The chest id.</param>
    /// <returns>True while a write is pending for the chest.</returns>
    internal bool IsPending(ContainerId chest) => pending.ContainsKey(chest);

    /// <summary>Reads the stored category of a chest. A pure read.</summary>
    /// <param name="chest">The chest.</param>
    /// <returns>The stored assignment, or null for Auto, no key, an invalid view or an undecodable value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="chest"/> is null.</exception>
    internal CategoryAssignment? Read(TChest chest)
    {
        if (chest is null)
        {
            throw new ArgumentNullException(nameof(chest));
        }

        return ChestCategoryReader.Read(storage.ReadRaw(chest));
    }

    /// <summary>Asks for a chest category write through the reservation protocol.</summary>
    /// <param name="chest">The chest.</param>
    /// <param name="desired">The value to write; null for Auto.</param>
    /// <returns>Applied or Refused when the write ended inside the call, Pending when a request was sent, Unchanged when nothing had to be written. A chest whose view is invalid gives Refused without a callback.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="chest"/> is null.</exception>
    internal CategoryWriteStatus TrySet(TChest chest, CategoryAssignment? desired)
    {
        if (chest is null)
        {
            throw new ArgumentNullException(nameof(chest));
        }

        if (!storage.TryGetId(chest, out ContainerId id))
        {
            return CategoryWriteStatus.Refused;
        }

        if (pending.ContainsKey(id))
        {
            return CategoryWriteStatus.Pending;
        }

        if (!ChestCategoryReader.WouldChange(desired, storage.ReadRaw(chest)))
        {
            return CategoryWriteStatus.Unchanged;
        }

        ChestInUse? inUse = reservations.QueryInUse(chest);
        ReservationRoute? route = reservations.Begin(chest, Operation);
        if (route is null)
        {
            Finish(id, desired, CategoryWriteOutcome.Unavailable, null);
            return CategoryWriteStatus.Refused;
        }

        TimeSpan now = clock.Elapsed;
        var entry = new Entry(chest, new PendingCategoryWrite(id, desired, now, CategoryWriteRules.Deadline(now, reserveTimeoutMs)), inUse?.HolderName);
        pending[id] = entry;
        return Advance(entry, now);
    }

    /// <summary>Advances every pending write by one frame. Called after the reservation driver's own tick.</summary>
    internal void Tick()
    {
        if (pending.Count == 0)
        {
            return;
        }

        TimeSpan now = clock.Elapsed;
        scratch.Clear();
        scratch.AddRange(pending.Keys);
        for (int i = 0; i < scratch.Count; i++)
        {
            if (pending.TryGetValue(scratch[i], out Entry? entry))
            {
                Advance(entry, now);
            }
        }

        scratch.Clear();
    }

    private CategoryWriteStatus Advance(Entry entry, TimeSpan now)
    {
        ContainerId id = entry.Write.Chest;
        Reservation<string>? reservation = reservations.Find(id);
        ReservationState? state = reservation?.State;
        switch (CategoryWriteRules.ActionFor(state))
        {
            case PendingWriteAction.Wait:
                if (!entry.Write.IsExpired(now))
                {
                    return CategoryWriteStatus.Pending;
                }

                reservations.Release(id);
                return End(entry, CategoryWriteOutcome.Timeout);
            case PendingWriteAction.Write:
                reservations.BeginWrite(id);
                bool written = reservations.CanWrite(id) && storage.Write(entry.Chest, ChestCategoryReader.StoredValue(entry.Write.Desired));
                reservations.Release(id);
                return End(entry, written ? CategoryWriteOutcome.Applied : CategoryWriteOutcome.Unavailable);
            default:
                reservations.Release(id);
                return End(entry, CategoryWriteRules.FailureOutcome(state, reservation?.Failure ?? ReservationFailure.None));
        }
    }

    private CategoryWriteStatus End(Entry entry, CategoryWriteOutcome outcome)
    {
        pending.Remove(entry.Write.Chest);
        Finish(entry.Write.Chest, entry.Write.Desired, outcome, entry.HolderLabel);
        return CategoryWriteRules.StatusOf(outcome);
    }

    private void Finish(ContainerId id, CategoryAssignment? desired, CategoryWriteOutcome outcome, string? holderLabel)
    {
        onFinished?.Invoke(new CategoryWriteResult(id, desired, outcome, holderLabel));
    }

    private sealed class Entry
    {
        internal Entry(TChest chest, PendingCategoryWrite write, string? holderLabel)
        {
            Chest = chest;
            Write = write;
            HolderLabel = holderLabel;
        }

        internal TChest Chest { get; }

        internal PendingCategoryWrite Write { get; }

        internal string? HolderLabel { get; }
    }
}
