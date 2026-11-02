namespace BaseStorage.Core.Persistence;

using System;
using System.Collections.Generic;

/// <summary>
/// The item types a character has marked as favourites, stored as their shared names joined by <c>|</c>. A favourite belongs to the item
/// type, not to one stack, because the game merges stacks without looking at any data on them. Immutable; decoding never throws.
/// </summary>
public sealed class FavouriteItems
{
    private const char Separator = '|';

    private readonly HashSet<string> names;

    private FavouriteItems(HashSet<string> names)
    {
        this.names = names;
    }

    /// <summary>Gets the empty set.</summary>
    public static FavouriteItems Empty { get; } = new FavouriteItems(new HashSet<string>(StringComparer.Ordinal));

    /// <summary>Gets the number of item types.</summary>
    public int Count => names.Count;

    /// <summary>Reads stored text.</summary>
    /// <param name="raw">The stored text, or null for none.</param>
    /// <returns>The set; empty parts are skipped.</returns>
    public static FavouriteItems Decode(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return Empty;
        }

        var read = new HashSet<string>(StringComparer.Ordinal);
        foreach (var part in raw!.Split(Separator))
        {
            if (part.Length > 0)
            {
                read.Add(part);
            }
        }

        return read.Count == 0 ? Empty : new FavouriteItems(read);
    }

    /// <summary>Writes the set as stored text, sorted, so the same set always gives the same text.</summary>
    /// <returns>The text; empty for no items.</returns>
    public string Encode()
    {
        var sorted = new List<string>(names);
        sorted.Sort(StringComparer.Ordinal);
        return string.Join(Separator.ToString(), sorted);
    }

    /// <summary>Tells whether an item type is a favourite.</summary>
    /// <param name="sharedName">The item's shared name.</param>
    /// <returns>True when it is.</returns>
    public bool Contains(string sharedName) => names.Contains(sharedName);

    /// <summary>Adds an item type.</summary>
    /// <param name="sharedName">The item's shared name.</param>
    /// <returns>The new set.</returns>
    public FavouriteItems With(string sharedName)
    {
        if (string.IsNullOrEmpty(sharedName) || sharedName.IndexOf(Separator) >= 0 || names.Contains(sharedName))
        {
            return this;
        }

        return new FavouriteItems(new HashSet<string>(names, StringComparer.Ordinal) { sharedName });
    }

    /// <summary>Removes an item type.</summary>
    /// <param name="sharedName">The item's shared name.</param>
    /// <returns>The new set.</returns>
    public FavouriteItems Without(string sharedName)
    {
        if (!names.Contains(sharedName))
        {
            return this;
        }

        var left = new HashSet<string>(names, StringComparer.Ordinal);
        left.Remove(sharedName);
        return left.Count == 0 ? Empty : new FavouriteItems(left);
    }
}
