namespace BaseStorage.Core.Organize.Deposit;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;

/// <summary>
/// The player-chosen exclusions of a Deposit (deposit contract 3.3): the favourite slots and the DepositNeverItems entries. An entry
/// is a prefab name (compared ordinally) or a <c>@</c> group token of <see cref="ItemListTokens"/> (compared ignoring case); an
/// unknown <c>@</c> token matches nothing. Immutable; built once per Deposit, outside the planner.
/// </summary>
public sealed class DepositExclusions
{
    private readonly HashSet<string> prefabs = new HashSet<string>(StringComparer.Ordinal);
    private readonly InventoryItemTraits groups;

    /// <summary>Creates the exclusions.</summary>
    /// <param name="favourites">The favourite slots; not null.</param>
    /// <param name="neverItems">The DepositNeverItems entries as <see cref="ItemListText.Parse"/> returns them; not null, no null entry. The list is copied.</param>
    /// <exception cref="ArgumentNullException"><paramref name="favourites"/> or <paramref name="neverItems"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="neverItems"/> contains a null entry.</exception>
    public DepositExclusions(FavouriteSlots favourites, IReadOnlyList<string> neverItems)
        : this(favourites, neverItems, null)
    {
    }

    private DepositExclusions(FavouriteSlots favourites, IReadOnlyList<string> neverItems, FavouriteSlots? selection)
    {
        Selection = selection;
        Favourites = favourites ?? throw new ArgumentNullException(nameof(favourites));
        if (neverItems is null)
        {
            throw new ArgumentNullException(nameof(neverItems));
        }

        var copy = new string[neverItems.Count];
        for (var i = 0; i < copy.Length; i++)
        {
            var entry = neverItems[i];
            if (entry is null)
            {
                throw new ArgumentException("neverItems contains a null entry.", nameof(neverItems));
            }

            copy[i] = entry;
            if (entry.Length > 0 && entry[0] == '@')
            {
                groups |= GroupOf(entry);
            }
            else
            {
                prefabs.Add(entry);
            }
        }

        NeverItems = new ReadOnlyCollection<string>(copy);
    }

    /// <summary>Gets the favourite slots.</summary>
    public FavouriteSlots Favourites { get; }

    /// <summary>Gets the DepositNeverItems entries, verbatim and in order, read-only.</summary>
    public IReadOnlyList<string> NeverItems { get; }

    /// <summary>Gets the slots of a Put away selected, or null for a normal Put away.</summary>
    public FavouriteSlots? Selection { get; }

    /// <summary>Makes the same exclusions for a Put away selected: only the given slots move, and picking them overrides the hotbar, favourite and never-list rules.</summary>
    /// <param name="selection">The selected slots. Not null.</param>
    /// <returns>The new exclusions.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selection"/> is null.</exception>
    public DepositExclusions OnlySelected(FavouriteSlots selection)
    {
        return new DepositExclusions(Favourites, NeverItems, selection ?? throw new ArgumentNullException(nameof(selection)));
    }

    /// <summary>Reads the never list from the settings once and pairs it with the favourites.</summary>
    /// <param name="settings">The settings read path; its DepositNeverItems is read once, now.</param>
    /// <param name="favourites">The favourite slots; not null.</param>
    /// <returns>The exclusions.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> or <paramref name="favourites"/> is null.</exception>
    public static DepositExclusions FromSettings(BaseStorageSettings settings, FavouriteSlots favourites)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        return new DepositExclusions(favourites, settings.DepositNeverItems);
    }

    /// <summary>
    /// Determines whether a stack is on the never list: its prefab name is an entry, or it belongs to a listed group. The
    /// <c>@ammo</c> group matches the Ammo trait or kind; <c>@mead</c> and <c>@bait</c> match their traits only.
    /// </summary>
    /// <param name="stack">The stack; not null.</param>
    /// <param name="kind">The item kind of the stack's prefab, as the kinds table gives it (Other when absent).</param>
    /// <returns>True when the stack must never be deposited by this list.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stack"/> is null.</exception>
    public bool IsNeverItem(PlayerInventoryStack stack, ItemKind kind)
    {
        if (stack is null)
        {
            throw new ArgumentNullException(nameof(stack));
        }

        if (prefabs.Contains(stack.Stack.PrefabName))
        {
            return true;
        }

        var traits = stack.Traits & ~InventoryItemTraits.Equipped;
        if (kind == ItemKind.Ammo)
        {
            traits |= InventoryItemTraits.Ammo;
        }

        return (traits & groups) != 0;
    }

    private static InventoryItemTraits GroupOf(string token)
    {
        if (string.Equals(token, ItemListTokens.Mead, StringComparison.OrdinalIgnoreCase))
        {
            return InventoryItemTraits.Mead;
        }

        if (string.Equals(token, ItemListTokens.Ammo, StringComparison.OrdinalIgnoreCase))
        {
            return InventoryItemTraits.Ammo;
        }

        if (string.Equals(token, ItemListTokens.Bait, StringComparison.OrdinalIgnoreCase))
        {
            return InventoryItemTraits.Bait;
        }

        return InventoryItemTraits.None;
    }
}
