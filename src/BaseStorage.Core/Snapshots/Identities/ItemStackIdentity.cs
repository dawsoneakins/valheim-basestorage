namespace BaseStorage.Core.Snapshots.Identities;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The per-stack item data that decides whether two stacks of one item may merge (S-QR-001, contract 2.1).
/// Equality is by value; the custom data is kept sorted by ordinal key so input order does not matter.
/// </summary>
public sealed class ItemStackIdentity : IEquatable<ItemStackIdentity>
{
    private static readonly ItemStackIdentity EmptyValue = new ItemStackIdentity();
    private static readonly Comparison<KeyValuePair<string, string>> ByKey =
        (a, b) => string.CompareOrdinal(a.Key, b.Key);

    private readonly float durability;
    private readonly long crafterId;
    private readonly string crafterName;
    private readonly int variant;
    private readonly int worldLevel;
    private readonly ReadOnlyCollection<KeyValuePair<string, string>> customData;

    /// <summary>Creates an identity from the item's durability, crafter, variant, world level and custom data.</summary>
    /// <param name="durability">The item durability; NaN and infinite values are rejected.</param>
    /// <param name="crafterId">The crafter id; 0 means no crafter.</param>
    /// <param name="crafterName">The crafter name; "" means no crafter; not null.</param>
    /// <param name="variant">The item variant.</param>
    /// <param name="worldLevel">The world level the item was made in.</param>
    /// <param name="customData">The custom data pairs; not null, and without null or duplicate keys or values.</param>
    public ItemStackIdentity(
        float durability,
        long crafterId,
        string crafterName,
        int variant,
        int worldLevel,
        IEnumerable<KeyValuePair<string, string>> customData)
    {
        this.durability = SnapshotGuard.Finite(durability, nameof(durability));
        if (crafterName is null)
        {
            throw new ArgumentNullException(nameof(crafterName));
        }

        if (customData is null)
        {
            throw new ArgumentNullException(nameof(customData));
        }

        // Copy through the List constructor: a foreach would bind the non-generic IEnumerator, which the dependency pin forbids.
        var list = new List<KeyValuePair<string, string>>(customData);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Key is null || list[i].Value is null)
            {
                throw new ArgumentException("Must not contain a null key or value.", nameof(customData));
            }
        }

        KeyValuePair<string, string>[] array = list.ToArray();
        Array.Sort(array, ByKey);
        for (int i = 1; i < array.Length; i++)
        {
            if (string.Equals(array[i - 1].Key, array[i].Key, StringComparison.Ordinal))
            {
                throw new ArgumentException("Must not contain duplicate keys.", nameof(customData));
            }
        }

        this.crafterId = crafterId;
        this.crafterName = crafterName;
        this.variant = variant;
        this.worldLevel = worldLevel;
        this.customData = new ReadOnlyCollection<KeyValuePair<string, string>>(array);
    }

    private ItemStackIdentity()
    {
        durability = 0f;
        crafterId = 0L;
        crafterName = string.Empty;
        variant = 0;
        worldLevel = 0;
        customData = new ReadOnlyCollection<KeyValuePair<string, string>>(Array.Empty<KeyValuePair<string, string>>());
    }

    /// <summary>Gets the identity with every field at its zero value; the same instance on every read.</summary>
    public static ItemStackIdentity Empty => EmptyValue;

    /// <summary>Gets the item durability, normalised so that -0 is stored as +0.</summary>
    public float Durability => durability;

    /// <summary>Gets the crafter id; 0 means no crafter.</summary>
    public long CrafterId => crafterId;

    /// <summary>Gets the crafter name; "" means no crafter. Never null.</summary>
    public string CrafterName => crafterName;

    /// <summary>Gets the item variant.</summary>
    public int Variant => variant;

    /// <summary>Gets the world level the item was made in.</summary>
    public int WorldLevel => worldLevel;

    /// <summary>Gets the custom data, sorted by key with ordinal comparison. Read-only.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> CustomData => customData;

    /// <summary>Gets a value that is true when this identity equals <see cref="Empty"/>.</summary>
    public bool IsEmpty => Equals(EmptyValue);

    /// <summary>Determines whether this identity equals another one.</summary>
    /// <param name="other">The identity to compare with; may be null.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(ItemStackIdentity? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (durability != other.durability
            || crafterId != other.crafterId
            || !string.Equals(crafterName, other.crafterName, StringComparison.Ordinal)
            || variant != other.variant
            || worldLevel != other.worldLevel
            || customData.Count != other.customData.Count)
        {
            return false;
        }

        for (int i = 0; i < customData.Count; i++)
        {
            if (!string.Equals(customData[i].Key, other.customData[i].Key, StringComparison.Ordinal)
                || !string.Equals(customData[i].Value, other.customData[i].Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether the object is an equal identity.</summary>
    /// <param name="obj">The object to compare with; may be null.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => Equals(obj as ItemStackIdentity);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int h = durability.GetHashCode();
            h = (h * 31) + crafterId.GetHashCode();
            h = (h * 31) + StringComparer.Ordinal.GetHashCode(crafterName);
            h = (h * 31) + variant;
            h = (h * 31) + worldLevel;
            for (int i = 0; i < customData.Count; i++)
            {
                h = (h * 31) + StringComparer.Ordinal.GetHashCode(customData[i].Key);
                h = (h * 31) + StringComparer.Ordinal.GetHashCode(customData[i].Value);
            }

            return h;
        }
    }

    /// <summary>Determines whether two identities are equal.</summary>
    /// <param name="left">The first identity; may be null.</param>
    /// <param name="right">The second identity; may be null.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(ItemStackIdentity? left, ItemStackIdentity? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two identities differ.</summary>
    /// <param name="left">The first identity; may be null.</param>
    /// <param name="right">The second identity; may be null.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(ItemStackIdentity? left, ItemStackIdentity? right) => !(left == right);
}
