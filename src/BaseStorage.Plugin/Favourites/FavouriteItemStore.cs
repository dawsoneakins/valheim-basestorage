namespace BaseStorage.Plugin.Favourites;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Persistence;

/// <summary>
/// The favourite item types over one custom data dictionary, saved with the character. The decoded set is cached against the raw text,
/// so a per-frame read allocates nothing. Main thread only.
/// </summary>
internal sealed class FavouriteItemStore
{
    private readonly IDictionary<string, string> customData;
    private string? lastRaw;
    private bool decoded;
    private FavouriteItems current = FavouriteItems.Empty;

    /// <summary>Creates the store over a custom data dictionary.</summary>
    /// <param name="customData">The dictionary.</param>
    internal FavouriteItemStore(IDictionary<string, string> customData)
    {
        this.customData = customData ?? throw new ArgumentNullException(nameof(customData));
    }

    /// <summary>Gets the favourite item types.</summary>
    internal FavouriteItems Current
    {
        get
        {
            customData.TryGetValue(PersistenceKeys.FavouriteItems, out string? raw);
            if (!decoded || !string.Equals(lastRaw, raw, StringComparison.Ordinal))
            {
                current = FavouriteItems.Decode(raw);
                lastRaw = raw;
                decoded = true;
            }

            return current;
        }
    }

    /// <summary>Adds an item type, or removes it when it is a favourite already.</summary>
    /// <param name="sharedName">The item's shared name.</param>
    /// <returns>Added when it is a favourite afterwards, Removed otherwise.</returns>
    internal FavouriteToggleOutcome Toggle(string sharedName)
    {
        bool remove = Current.Contains(sharedName);
        Store(remove ? Current.Without(sharedName) : Current.With(sharedName));
        return remove ? FavouriteToggleOutcome.Removed : FavouriteToggleOutcome.Added;
    }

    /// <summary>Turns the slots of an older version into item types, then removes the old value.</summary>
    /// <param name="slots">The old slots.</param>
    /// <param name="inventory">The character's inventory.</param>
    internal void AdoptSlots(FavouriteSlots slots, Inventory inventory)
    {
        FavouriteItems adopted = Current;
        foreach (InventorySlot slot in slots.Slots)
        {
            ItemDrop.ItemData? item = inventory.GetItemAt(slot.X, slot.Y);
            if (item != null)
            {
                adopted = adopted.With(item.m_shared.m_name);
            }
        }

        Store(adopted);
        customData.Remove(PersistenceKeys.Favourites);
    }

    private void Store(FavouriteItems items)
    {
        if (items.Count == 0)
        {
            customData.Remove(PersistenceKeys.FavouriteItems);
        }
        else
        {
            customData[PersistenceKeys.FavouriteItems] = items.Encode();
        }
    }
}
