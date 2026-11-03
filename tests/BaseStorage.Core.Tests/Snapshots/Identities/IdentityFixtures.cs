namespace BaseStorage.Core.Tests.Snapshots.Identities;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots.Identities;

/// <summary>
/// The identity fixtures of S-QR-001 contract 2.3 and 6 (wl2, worn, erik). Each builder returns a new instance on every call,
/// so a test that needs one instance twice keeps it in a local.
/// </summary>
internal static class IdentityFixtures
{
    /// <summary>Builds one key-value pair.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>The pair.</returns>
    internal static KeyValuePair<string, string> KV(string key, string value) => new KeyValuePair<string, string>(key, value);

    /// <summary>An empty custom data list.</summary>
    /// <returns>No pairs.</returns>
    internal static KeyValuePair<string, string>[] NoData() => Array.Empty<KeyValuePair<string, string>>();

    /// <summary>The custom data of <see cref="Erik"/>, in the order of contract 2.3.</summary>
    /// <returns>The pairs rune=frost and dyed=red.</returns>
    internal static KeyValuePair<string, string>[] ErikData() => new[] { KV("rune", "frost"), KV("dyed", "red") };

    /// <summary>A world-level-2 drop (contract 2.3, wl2).</summary>
    /// <returns>The identity.</returns>
    internal static ItemStackIdentity Wl2() => new ItemStackIdentity(0f, 0L, "", 0, 2, NoData());

    /// <summary>A used item with durability 37.5 (contract 2.3, worn).</summary>
    /// <returns>The identity.</returns>
    internal static ItemStackIdentity Worn() => new ItemStackIdentity(37.5f, 0L, "", 0, 0, NoData());

    /// <summary>A crafted item by Erik with custom data (contract 2.3, erik).</summary>
    /// <returns>The identity.</returns>
    internal static ItemStackIdentity Erik() => new ItemStackIdentity(100f, 8812345012L, "Erik", 1, 0, ErikData());
}
