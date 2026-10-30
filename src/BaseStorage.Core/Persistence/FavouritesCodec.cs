namespace BaseStorage.Core.Persistence;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>Encodes and decodes the value stored under the favourites key.</summary>
public static class FavouritesCodec
{
    private const string Tag = "F1";

    /// <summary>Encodes a set as F1 followed by a semicolon and x,y for each slot in ascending order.</summary>
    /// <param name="value">The set to encode.</param>
    /// <returns>The encoded text.</returns>
    public static string Encode(FavouriteSlots value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var text = new StringBuilder(Tag);
        foreach (InventorySlot slot in value.Slots)
        {
            text.Append(';')
                .Append(slot.X.ToString(CultureInfo.InvariantCulture))
                .Append(',')
                .Append(slot.Y.ToString(CultureInfo.InvariantCulture));
        }

        return text.ToString();
    }

    /// <summary>Decodes text produced by Encode; anything else gives no value.</summary>
    /// <param name="encoded">The stored text, possibly null.</param>
    /// <returns>The set, or null for no value.</returns>
    public static FavouriteSlots? Decode(string? encoded)
    {
        if (encoded is null || !encoded.StartsWith(Tag, StringComparison.Ordinal))
        {
            return null;
        }

        if (encoded.Length == Tag.Length)
        {
            return FavouriteSlots.Empty;
        }

        if (encoded[Tag.Length] != ';')
        {
            return null;
        }

        var slots = new List<InventorySlot>();
        int start = Tag.Length + 1;
        while (true)
        {
            int end = encoded.IndexOf(';', start);
            int stop = end < 0 ? encoded.Length : end;
            int comma = encoded.IndexOf(',', start, stop - start);
            if (comma < 0
                || !WireNumbers.TryParseSlotNumber(encoded, start, comma - start, out int x)
                || !WireNumbers.TryParseSlotNumber(encoded, comma + 1, stop - comma - 1, out int y))
            {
                return null;
            }

            var slot = new InventorySlot(x, y);
            if (slots.Count > 0 && slots[slots.Count - 1].CompareTo(slot) >= 0)
            {
                return null;
            }

            slots.Add(slot);
            if (end < 0)
            {
                // The constructor re-normalises (Distinct + sort) on purpose; the input is already ascending, so this is a cheap no-op.
                return new FavouriteSlots(slots);
            }

            start = end + 1;
        }
    }
}
