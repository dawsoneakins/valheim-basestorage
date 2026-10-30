namespace BaseStorage.Core.Snapshots;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>An immutable view of one container's size, flags and contents.</summary>
public sealed class ContainerSnapshot : IEquatable<ContainerSnapshot>
{
    private const int MaxDimension = 256;
    private const ContainerFlags AllFlags = ContainerFlags.PlayerBuilt | ContainerFlags.Vehicle
        | ContainerFlags.Tombstone | ContainerFlags.AccessDenied | ContainerFlags.InUseByOther;

    /// <summary>Creates a container snapshot; the contents are copied.</summary>
    /// <param name="id">The container id; must not be none.</param>
    /// <param name="position">The container's world position.</param>
    /// <param name="width">Inventory grid columns, 1 to 256.</param>
    /// <param name="height">Inventory grid rows, 1 to 256.</param>
    /// <param name="contents">The stacks, in inventory order; not null and without null elements.</param>
    /// <param name="flags">Game-observed state; only defined bits are allowed.</param>
    public ContainerSnapshot(
        ContainerId id,
        WorldPosition position,
        int width,
        int height,
        IEnumerable<ItemStackSnapshot> contents,
        ContainerFlags flags)
    {
        if (id.IsNone)
        {
            throw new ArgumentException("Must not be none.", nameof(id));
        }

        if (width < 1 || width > MaxDimension)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Must be between 1 and 256.");
        }

        if (height < 1 || height > MaxDimension)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Must be between 1 and 256.");
        }

        if (contents is null)
        {
            throw new ArgumentNullException(nameof(contents));
        }

        if ((flags & ~AllFlags) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(flags), "Contains undefined bits.");
        }

        ItemStackSnapshot[] copy = System.Linq.Enumerable.ToArray(contents);
        foreach (ItemStackSnapshot stack in copy)
        {
            if (stack is null)
            {
                throw new ArgumentException("Must not contain null elements.", nameof(contents));
            }
        }

        Id = id;
        Position = position;
        Width = width;
        Height = height;
        Flags = flags;
        Contents = new ReadOnlyCollection<ItemStackSnapshot>(copy);
    }

    /// <summary>Gets the container id.</summary>
    public ContainerId Id { get; }

    /// <summary>Gets the container's world position.</summary>
    public WorldPosition Position { get; }

    /// <summary>Gets the inventory grid columns.</summary>
    public int Width { get; }

    /// <summary>Gets the inventory grid rows.</summary>
    public int Height { get; }

    /// <summary>Gets the slot count, width times height.</summary>
    public int SlotCount => Width * Height;

    /// <summary>Gets the read-only stacks, in inventory order; an index identifies a stack.</summary>
    public IReadOnlyList<ItemStackSnapshot> Contents { get; }

    /// <summary>Gets the game-observed state flags.</summary>
    public ContainerFlags Flags { get; }

    /// <summary>Determines whether two snapshots are equal.</summary>
    /// <param name="left">The first snapshot.</param>
    /// <param name="right">The second snapshot.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(ContainerSnapshot? left, ContainerSnapshot? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two snapshots differ.</summary>
    /// <param name="left">The first snapshot.</param>
    /// <param name="right">The second snapshot.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(ContainerSnapshot? left, ContainerSnapshot? right) => !(left == right);

    /// <summary>Determines whether all fields are equal and the contents match element by element, in order.</summary>
    /// <param name="other">The snapshot to compare with.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(ContainerSnapshot? other)
    {
        if (other is null
            || Id != other.Id
            || Position != other.Position
            || Width != other.Width
            || Height != other.Height
            || Flags != other.Flags
            || Contents.Count != other.Contents.Count)
        {
            return false;
        }

        for (int i = 0; i < Contents.Count; i++)
        {
            if (Contents[i] != other.Contents[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether the object is an equal snapshot.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => Equals(obj as ContainerSnapshot);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int h = Id.GetHashCode();
            h = (h * 31) + Position.GetHashCode();
            h = (h * 31) + Width;
            h = (h * 31) + Height;
            h = (h * 31) + (int)Flags;
            for (int i = 0; i < Contents.Count; i++)
            {
                h = (h * 31) + Contents[i].GetHashCode();
            }

            return h;
        }
    }
}
