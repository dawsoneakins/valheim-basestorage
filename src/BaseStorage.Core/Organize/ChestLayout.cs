namespace BaseStorage.Core.Organize;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The contents of one usable chest after the plan (contract 4.6). A category and a claimed overflow chest are compacted; a
/// source chest keeps its working stacks in input order; an unused overflow chest stays empty.
/// </summary>
public sealed class ChestLayout
{
    /// <summary>Creates a layout. Only the planner creates layouts.</summary>
    /// <param name="container">The input snapshot.</param>
    /// <param name="role">The role of the chest in the plan.</param>
    /// <param name="categoryName">The category for a category or claimed overflow chest; otherwise null.</param>
    /// <param name="stacks">The chest's contents after the plan, as new stack instances.</param>
    internal ChestLayout(ContainerSnapshot container, ChestRole role, string? categoryName, ItemStackSnapshot[] stacks)
    {
        Container = container;
        Id = container.Id;
        Role = role;
        CategoryName = categoryName;
        Stacks = new ReadOnlyCollection<ItemStackSnapshot>(stacks);
    }

    /// <summary>Gets the input snapshot, the same instance as passed in the containers argument.</summary>
    public ContainerSnapshot Container { get; }

    /// <summary>Gets the container id, the same as <see cref="Container"/> id.</summary>
    public ContainerId Id { get; }

    /// <summary>Gets the role of the chest in the plan.</summary>
    public ChestRole Role { get; }

    /// <summary>Gets the category for a category or claimed overflow chest; null for a source or unused overflow chest.</summary>
    public string? CategoryName { get; }

    /// <summary>Gets the chest's contents after the plan, read-only, with new stack instances.</summary>
    public IReadOnlyList<ItemStackSnapshot> Stacks { get; }
}
