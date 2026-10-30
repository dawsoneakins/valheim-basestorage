namespace BaseStorage.Core.Bases.Fill;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>The share of the slots of a set of containers that hold an item (contract base-fill BF1-BF8).</summary>
public readonly struct BaseFill : IEquatable<BaseFill>
{
    /// <summary>Creates a fill value.</summary>
    /// <param name="occupiedSlots">The slots that hold at least one item. Must not be negative.</param>
    /// <param name="totalSlots">The total slot capacity. Must not be negative.</param>
    public BaseFill(int occupiedSlots, int totalSlots)
    {
        if (occupiedSlots < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(occupiedSlots), occupiedSlots, "Occupied slots must not be negative.");
        }

        if (totalSlots < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalSlots), totalSlots, "Total slots must not be negative.");
        }

        if (occupiedSlots > totalSlots)
        {
            throw new ArgumentOutOfRangeException(nameof(occupiedSlots), occupiedSlots, "Occupied slots must not exceed total slots.");
        }

        OccupiedSlots = occupiedSlots;
        TotalSlots = totalSlots;
        Percent = totalSlots == 0 ? 0 : (int)(((200L * occupiedSlots) + totalSlots) / (2L * totalSlots));
    }

    /// <summary>Gets the slots that hold at least one item.</summary>
    public int OccupiedSlots { get; }

    /// <summary>Gets the total slot capacity.</summary>
    public int TotalSlots { get; }

    /// <summary>Gets the fill as a whole percent from 0 to 100, with an exact half rounded up.</summary>
    public int Percent { get; }

    /// <summary>Compares two values for equality.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when both counts are equal.</returns>
    public static bool operator ==(BaseFill left, BaseFill right) => left.Equals(right);

    /// <summary>Compares two values for inequality.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when a count differs.</returns>
    public static bool operator !=(BaseFill left, BaseFill right) => !left.Equals(right);

    /// <summary>Sums the slots of the given containers, as given.</summary>
    /// <param name="containers">The container snapshots, normally the eligible set.</param>
    /// <returns>The fill over the containers.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="containers"/> is null.</exception>
    /// <exception cref="ArgumentException">An element is null, or the total slots exceed <see cref="int.MaxValue"/>.</exception>
    public static BaseFill FromContainers(IEnumerable<ContainerSnapshot> containers)
    {
        if (containers is null)
        {
            throw new ArgumentNullException(nameof(containers));
        }

        long occupied = 0;
        long total = 0;
        var copy = new List<ContainerSnapshot>(containers);
        for (int i = 0; i < copy.Count; i++)
        {
            ContainerSnapshot c = copy[i];
            if (c is null)
            {
                throw new ArgumentException("Containers must not contain null.", nameof(containers));
            }

            occupied += Math.Min(c.Contents.Count, c.SlotCount);
            total += c.SlotCount;
        }

        if (total > int.MaxValue)
        {
            throw new ArgumentException("Total slots exceed the supported range.", nameof(containers));
        }

        return new BaseFill((int)occupied, (int)total);
    }

    /// <summary>Compares both counts with another value.</summary>
    /// <param name="other">The value to compare with.</param>
    /// <returns>True when both counts are equal.</returns>
    public bool Equals(BaseFill other) => OccupiedSlots == other.OccupiedSlots && TotalSlots == other.TotalSlots;

    /// <summary>Compares this value with an object.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when the object is an equal BaseFill.</returns>
    public override bool Equals(object? obj) => obj is BaseFill other && Equals(other);

    /// <summary>Gets a hash code that is equal for equal values.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => unchecked((OccupiedSlots * 397) ^ TotalSlots);
}
