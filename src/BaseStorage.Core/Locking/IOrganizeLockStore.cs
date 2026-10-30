namespace BaseStorage.Core.Locking;

using BaseStorage.Core.Snapshots;

/// <summary>
/// The per-station storage of the organize lock (contract section 2). It always addresses the value stored under
/// <c>PersistenceKeys.OrganizeLock</c>, and it never decodes or validates the text it holds. The Plugin implements it over the
/// station's networked object data; the Core tests implement it with a fake.
/// </summary>
public interface IOrganizeLockStore
{
    /// <summary>Reads the raw text stored under the organize lock key on a station.</summary>
    /// <param name="station">The station to read.</param>
    /// <returns>The raw text exactly as last written, or null when the station holds no value, is unknown or is not loaded.</returns>
    string? Read(StationId station);

    /// <summary>Stores text under the organize lock key on a station, replacing any previous value on that station only.</summary>
    /// <param name="station">The station to write.</param>
    /// <param name="value">The text to store; never null when called by <c>OrganizeLock</c>.</param>
    void Write(StationId station, string value);

    /// <summary>Removes the value stored under the organize lock key on a station. Idempotent: a no-op when there is none.</summary>
    /// <param name="station">The station to clear.</param>
    void Clear(StationId station);
}
