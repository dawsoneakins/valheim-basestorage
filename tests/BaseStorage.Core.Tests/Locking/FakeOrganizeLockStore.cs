namespace BaseStorage.Core.Tests.Locking;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Snapshots;

/// <summary>
/// A dictionary-backed <see cref="IOrganizeLockStore"/> that never decodes or validates (contract 2.3, ADR-GWIN-027). It keeps an ordered
/// call log (<c>Read:</c>, <c>Write:</c> and <c>Clear:</c> followed by the station key) and counts writes and clears. It can throw on read
/// or write. The log and the counters rise before the throw, so a failed call is counted too.
/// </summary>
internal sealed class FakeOrganizeLockStore : IOrganizeLockStore
{
    private readonly Dictionary<StationId, string> values = new Dictionary<StationId, string>();

    /// <summary>Gets the ordered call log, one entry per call: the call name and the station key.</summary>
    public List<string> Calls { get; } = new List<string>();

    /// <summary>Gets the number of Write calls since the last <see cref="ResetLog"/>.</summary>
    public int WriteCount { get; private set; }

    /// <summary>Gets the number of Clear calls since the last <see cref="ResetLog"/>.</summary>
    public int ClearCount { get; private set; }

    /// <summary>When set, every Read throws this exception after it is logged and counted.</summary>
    public Exception? ThrowOnRead { get; set; }

    /// <summary>When set, every Write throws this exception after it is logged and counted; nothing is stored.</summary>
    public Exception? ThrowOnWrite { get; set; }

    /// <summary>The log key of a station, for example <c>3141592653/17</c>.</summary>
    /// <param name="station">The station.</param>
    /// <returns>The key.</returns>
    public static string KeyOf(StationId station) =>
        station.UserId.ToString(CultureInfo.InvariantCulture) + "/" + station.Id.ToString(CultureInfo.InvariantCulture);

    /// <summary>The log entry of a Read on a station.</summary>
    /// <param name="station">The station.</param>
    /// <returns>The entry.</returns>
    public static string ReadCall(StationId station) => "Read:" + KeyOf(station);

    /// <summary>The log entry of a Write on a station.</summary>
    /// <param name="station">The station.</param>
    /// <returns>The entry.</returns>
    public static string WriteCall(StationId station) => "Write:" + KeyOf(station);

    /// <summary>The log entry of a Clear on a station.</summary>
    /// <param name="station">The station.</param>
    /// <returns>The entry.</returns>
    public static string ClearCall(StationId station) => "Clear:" + KeyOf(station);

    /// <summary>Stores text on a station directly. Not logged: it models the state a test starts from.</summary>
    /// <param name="station">The station.</param>
    /// <param name="value">The raw text, stored as given.</param>
    public void Seed(StationId station, string value) => values[station] = value;

    /// <summary>Forgets a station and its value, as when the station is destroyed. Not logged.</summary>
    /// <param name="station">The station.</param>
    public void Remove(StationId station) => values.Remove(station);

    /// <summary>Empties the call log and resets both counters. The stored values are kept.</summary>
    public void ResetLog()
    {
        Calls.Clear();
        WriteCount = 0;
        ClearCount = 0;
    }

    /// <summary>Gets a snapshot of every stored entry, sorted by station id.</summary>
    /// <returns>The entries, in ascending station id order.</returns>
    public (StationId Station, string Value)[] Contents() =>
        values
            .OrderBy(pair => pair.Key, Comparer<StationId>.Default)
            .Select(pair => (pair.Key, pair.Value))
            .ToArray();

    /// <summary>Reads the raw text on a station.</summary>
    /// <param name="station">The station to read.</param>
    /// <returns>The stored text, or null when there is none.</returns>
    public string? Read(StationId station)
    {
        Calls.Add(ReadCall(station));
        if (ThrowOnRead is not null)
        {
            throw ThrowOnRead;
        }

        return values.TryGetValue(station, out string? value) ? value : null;
    }

    /// <summary>Stores the raw text on a station, replacing any previous value there.</summary>
    /// <param name="station">The station to write.</param>
    /// <param name="value">The text to store.</param>
    public void Write(StationId station, string value)
    {
        WriteCount++;
        Calls.Add(WriteCall(station));
        if (ThrowOnWrite is not null)
        {
            throw ThrowOnWrite;
        }

        values[station] = value;
    }

    /// <summary>Removes the value on a station. Idempotent.</summary>
    /// <param name="station">The station to clear.</param>
    public void Clear(StationId station)
    {
        ClearCount++;
        Calls.Add(ClearCall(station));
        values.Remove(station);
    }
}
