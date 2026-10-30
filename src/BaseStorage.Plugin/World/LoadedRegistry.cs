namespace BaseStorage.Plugin.World;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// A set of loaded game objects keyed by reference identity (contract section 2, ADR-GWIN-030). A change event fires on
/// every add or remove that actually changes the set, and never on a no-op. Main thread only (W2): nothing here is
/// thread-safe and nothing locks. Game-free (W1): it compiles unchanged into the Plugin and into the Core tests.
/// </summary>
/// <typeparam name="TKey">The key type. Keys are compared by reference identity, never by an overridden Equals.</typeparam>
/// <typeparam name="TItem">The stored item type.</typeparam>
internal sealed class LoadedRegistry<TKey, TItem>
    where TKey : class
    where TItem : class
{
    private readonly Action onAdded;
    private readonly Action onRemoved;
    private readonly Dictionary<TKey, TItem> items = new Dictionary<TKey, TItem>(ReferenceComparer.Instance);
    private int version;

    /// <summary>Creates an empty registry with its two change callbacks.</summary>
    /// <param name="onAdded">Called exactly once after each effective add. Must not call Add or Remove.</param>
    /// <param name="onRemoved">Called exactly once after each effective remove. Must not call Add or Remove.</param>
    /// <exception cref="ArgumentNullException"><paramref name="onAdded"/> or <paramref name="onRemoved"/> is null.</exception>
    internal LoadedRegistry(Action onAdded, Action onRemoved)
    {
        if (onAdded is null)
        {
            throw new ArgumentNullException(nameof(onAdded));
        }

        if (onRemoved is null)
        {
            throw new ArgumentNullException(nameof(onRemoved));
        }

        this.onAdded = onAdded;
        this.onRemoved = onRemoved;
    }

    /// <summary>Gets the number of stored pairs.</summary>
    internal int Count => this.items.Count;

    /// <summary>Gets the number of effective changes so far (unchecked, incremented on every add and remove that changes the set).</summary>
    internal int Version => this.version;

    /// <summary>Adds a pair when the key is not yet present.</summary>
    /// <param name="key">The key, compared by reference. Null gives false with no change.</param>
    /// <param name="item">The item to store. Null gives false with no change.</param>
    /// <returns>True when the pair was stored and <c>onAdded</c> ran; false for a no-op (the first item is kept).</returns>
    internal bool Add(TKey key, TItem item)
    {
        if (key is null || item is null || this.items.ContainsKey(key))
        {
            return false;
        }

        this.items.Add(key, item);
        unchecked
        {
            this.version++;
        }

        this.onAdded();
        return true;
    }

    /// <summary>Removes the pair stored under the key.</summary>
    /// <param name="key">The key, compared by reference. Null or absent gives false with no change.</param>
    /// <returns>True when the pair was removed and <c>onRemoved</c> ran; false for a no-op.</returns>
    internal bool Remove(TKey key)
    {
        if (key is null || !this.items.Remove(key))
        {
            return false;
        }

        unchecked
        {
            this.version++;
        }

        this.onRemoved();
        return true;
    }

    /// <summary>Tells whether a key is present.</summary>
    /// <param name="key">The key, compared by reference. Null gives false.</param>
    /// <returns>True when the key is present.</returns>
    internal bool Contains(TKey key)
    {
        return key is not null && this.items.ContainsKey(key);
    }

    /// <summary>Gets the item stored under a key.</summary>
    /// <param name="key">The key, compared by reference. Null gives null.</param>
    /// <returns>The stored item, or null when the key is absent.</returns>
    internal TItem? Find(TKey key)
    {
        if (key is not null && this.items.TryGetValue(key, out TItem? item))
        {
            return item;
        }

        return null;
    }

    /// <summary>Copies every stored item into a caller-owned buffer. Allocates nothing when the buffer capacity is at least Count.</summary>
    /// <param name="buffer">The buffer. It is cleared first; the order of the items is not contracted.</param>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
    internal void CopyItemsTo(List<TItem> buffer)
    {
        if (buffer is null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        buffer.Clear();
        foreach (KeyValuePair<TKey, TItem> pair in this.items)
        {
            buffer.Add(pair.Value);
        }
    }

    private sealed class ReferenceComparer : IEqualityComparer<TKey>
    {
        internal static readonly ReferenceComparer Instance = new ReferenceComparer();

        public bool Equals(TKey? x, TKey? y) => ReferenceEquals(x, y);

        public int GetHashCode(TKey obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
