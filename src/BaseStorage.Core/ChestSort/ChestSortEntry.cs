namespace BaseStorage.Core.ChestSort;

using System;
using BaseStorage.Core.Snapshots;

/// <summary>One stack of the chest being sorted, with what decides its place.</summary>
public sealed class ChestSortEntry
{
    /// <summary>Creates an entry.</summary>
    /// <param name="stack">The stack.</param>
    /// <param name="categoryRank">The position of the stack's category in the catalog; lower sorts first.</param>
    /// <param name="displayName">The item's name as the player reads it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stack"/> or <paramref name="displayName"/> is null.</exception>
    public ChestSortEntry(ItemStackSnapshot stack, int categoryRank, string displayName)
    {
        Stack = stack ?? throw new ArgumentNullException(nameof(stack));
        CategoryRank = categoryRank;
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
    }

    /// <summary>Gets the stack.</summary>
    public ItemStackSnapshot Stack { get; }

    /// <summary>Gets the category's position in the catalog.</summary>
    public int CategoryRank { get; }

    /// <summary>Gets the item's name.</summary>
    public string DisplayName { get; }
}
