namespace BaseStorage.Core.Categories.AutoAssign;

using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>The resolved entry of one chest of a base, as produced by <see cref="AutoAssigner.Resolve"/>. Immutable.</summary>
public sealed class ChestAutoAssignment
{
    /// <summary>Creates an entry. Only the resolver creates entries.</summary>
    /// <param name="container">The input snapshot.</param>
    /// <param name="outcome">The outcome decided for the chest.</param>
    /// <param name="assignment">The effective assignment after resolution, or null when the chest stays unassigned.</param>
    internal ChestAutoAssignment(ContainerSnapshot container, AutoAssignOutcome outcome, CategoryAssignment? assignment)
    {
        Container = container;
        Id = container.Id;
        Outcome = outcome;
        Assignment = assignment;
        CategoryName = assignment?.CategoryName;
    }

    /// <summary>Gets the input snapshot, the same instance that was passed in.</summary>
    public ContainerSnapshot Container { get; }

    /// <summary>Gets the container id, which is Container.Id.</summary>
    public ContainerId Id { get; }

    /// <summary>Gets the outcome decided for this chest.</summary>
    public AutoAssignOutcome Outcome { get; }

    /// <summary>Gets the chest's effective assignment after resolution, or null when it stays unassigned.</summary>
    public CategoryAssignment? Assignment { get; }

    /// <summary>Gets the category name for AlreadyAssigned and Assigned chests, verbatim, and null for every other outcome.</summary>
    public string? CategoryName { get; }
}
