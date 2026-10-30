namespace BaseStorage.Plugin.Favourites;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Ui;

/// <summary>
/// The profile favourites adapter over one custom data dictionary (contract S-E004-014 section 3.2). It is the only reader and
/// writer of the favourites key. Reading never throws and never writes; a toggle writes or removes that one key exactly once.
/// The decoded set is cached against the raw stored string, so a per-frame read allocates nothing.
/// </summary>
internal sealed class ProfileFavourites
{
    private const int MaxCoordinate = 255;

    private readonly IDictionary<string, string> customData;
    private string? lastRaw;
    private bool decoded;
    private FavouriteSlots current = FavouriteSlots.Empty;

    /// <summary>Creates the adapter over the custom data dictionary. Reads and writes nothing.</summary>
    /// <param name="customData">The custom data dictionary. Must not be null.</param>
    internal ProfileFavourites(IDictionary<string, string> customData)
    {
        this.customData = customData ?? throw new ArgumentNullException(nameof(customData));
    }

    /// <summary>Gets the custom data dictionary passed to the constructor.</summary>
    internal IDictionary<string, string> CustomData => customData;

    /// <summary>Gets the current favourite set, decoded from the favourites key (never throws).</summary>
    internal FavouriteSlots Current
    {
        get
        {
            customData.TryGetValue(PersistenceKeys.Favourites, out string? raw);
            if (decoded && string.Equals(lastRaw, raw, StringComparison.Ordinal))
            {
                return current;
            }

            current = FavouritesCodec.Decode(raw) ?? FavouriteSlots.Empty;
            lastRaw = raw;
            decoded = true;
            return current;
        }
    }

    /// <summary>Determines whether the slot is a favourite.</summary>
    /// <param name="slot">The slot to look for.</param>
    /// <returns>True when the slot is a favourite.</returns>
    internal bool IsFavourite(InventorySlot slot) => Current.Contains(slot);

    /// <summary>Toggles the slot and writes the favourites key once.</summary>
    /// <param name="slot">The slot to toggle.</param>
    /// <returns>Added when the slot is a favourite afterwards, Removed otherwise.</returns>
    internal FavouriteToggleOutcome Toggle(InventorySlot slot)
    {
        FavouriteSlots before = Current;
        var slots = new List<InventorySlot>(before.Count + 1);
        bool removed = false;
        foreach (InventorySlot existing in before.Slots)
        {
            if (existing == slot)
            {
                removed = true;
            }
            else
            {
                slots.Add(existing);
            }
        }

        if (!removed)
        {
            slots.Add(slot);
        }

        var next = new FavouriteSlots(slots);
        if (next.Count == 0)
        {
            customData.Remove(PersistenceKeys.Favourites);
            lastRaw = null;
        }
        else
        {
            string encoded = FavouritesCodec.Encode(next);
            customData[PersistenceKeys.Favourites] = encoded;
            lastRaw = encoded;
        }

        current = next;
        decoded = true;
        return removed ? FavouriteToggleOutcome.Removed : FavouriteToggleOutcome.Added;
    }

    /// <summary>Converts a game grid position to an inventory slot without throwing.</summary>
    /// <param name="x">The column.</param>
    /// <param name="y">The row.</param>
    /// <param name="slot">The slot, or the default value when the position is out of range.</param>
    /// <returns>True when both coordinates are in 0..255.</returns>
    internal static bool TryGetSlot(int x, int y, out InventorySlot slot)
    {
        if (x < 0 || x > MaxCoordinate || y < 0 || y > MaxCoordinate)
        {
            slot = default;
            return false;
        }

        slot = new InventorySlot(x, y);
        return true;
    }

    /// <summary>Gets the string key of the message shown for an outcome.</summary>
    /// <param name="outcome">The outcome.</param>
    /// <returns>The string table key.</returns>
    internal static string MessageKeyFor(FavouriteToggleOutcome outcome)
    {
        switch (outcome)
        {
            case FavouriteToggleOutcome.Added:
                return StringKeys.MessageFavouriteOn;
            case FavouriteToggleOutcome.Removed:
                return StringKeys.MessageFavouriteOff;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown favourite toggle outcome.");
        }
    }
}
