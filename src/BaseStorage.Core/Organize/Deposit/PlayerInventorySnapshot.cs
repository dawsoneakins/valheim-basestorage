namespace BaseStorage.Core.Organize.Deposit;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The player inventory as the Deposit planner reads it (deposit contract 3.2): the grid size and every occupied slot, including the
/// slots a Deposit will not touch. <see cref="Container"/> is the same inventory addressed like a chest, with one stack per occupied
/// slot in <see cref="Stacks"/> order, so the executor can verify and address it through the container layer. Immutable.
/// </summary>
public sealed class PlayerInventorySnapshot
{
    private const int MaxDimension = 256;

    /// <summary>Creates a snapshot; the stacks are copied.</summary>
    /// <param name="id">The inventory's container id, the layer's PlayerInventoryId; must not be none.</param>
    /// <param name="width">Grid columns, 1 to 256 (8 in the vanilla game).</param>
    /// <param name="height">Grid rows, 1 to 256 (4 in the vanilla game); row 0 is the hotbar.</param>
    /// <param name="stacks">Every occupied slot, in any order; not null, no null element, no two stacks in one slot, every slot inside the grid.</param>
    /// <exception cref="ArgumentException"><paramref name="id"/> is none, or <paramref name="stacks"/> has a null element, a repeated slot or a slot outside the grid.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> or <paramref name="height"/> is outside 1 to 256.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="stacks"/> is null.</exception>
    public PlayerInventorySnapshot(ContainerId id, int width, int height, IEnumerable<PlayerInventoryStack> stacks)
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

        if (stacks is null)
        {
            throw new ArgumentNullException(nameof(stacks));
        }

        var copy = new List<PlayerInventoryStack>();
        var seen = new HashSet<InventorySlot>();
        foreach (var stack in stacks)
        {
            if (stack is null)
            {
                throw new ArgumentException("Must not contain null elements.", nameof(stacks));
            }

            if (stack.Slot.X >= width || stack.Slot.Y >= height)
            {
                throw new ArgumentException("A slot lies outside the grid.", nameof(stacks));
            }

            if (!seen.Add(stack.Slot))
            {
                throw new ArgumentException("Two stacks share one slot.", nameof(stacks));
            }

            copy.Add(stack);
        }

        var contents = new ItemStackSnapshot[copy.Count];
        for (var i = 0; i < contents.Length; i++)
        {
            contents[i] = copy[i].Stack;
        }

        Id = id;
        Width = width;
        Height = height;
        Stacks = new ReadOnlyCollection<PlayerInventoryStack>(copy.ToArray());
        Container = new ContainerSnapshot(id, new WorldPosition(0f, 0f, 0f), width, height, contents, ContainerFlags.None);
    }

    /// <summary>Gets the inventory's container id.</summary>
    public ContainerId Id { get; }

    /// <summary>Gets the grid columns.</summary>
    public int Width { get; }

    /// <summary>Gets the grid rows.</summary>
    public int Height { get; }

    /// <summary>Gets the occupied slots in the order given, read-only. Index <c>i</c> here is stack index <c>i</c> in <see cref="Container"/> and in every move.</summary>
    public IReadOnlyList<PlayerInventoryStack> Stacks { get; }

    /// <summary>Gets the inventory addressed like a chest: the id, the grid, flags None, position zero, and one stack per occupied slot in <see cref="Stacks"/> order.</summary>
    public ContainerSnapshot Container { get; }
}
