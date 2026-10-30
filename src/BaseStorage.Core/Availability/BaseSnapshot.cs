namespace BaseStorage.Core.Availability;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>The inputs to one availability answer: the player inventory, the base's containers, their Keep assignments and the eligibility options (contract section 9.5).</summary>
public sealed class BaseSnapshot
{
    /// <summary>Creates a snapshot and takes defensive copies of <paramref name="containers"/> and <paramref name="assignments"/>.</summary>
    /// <param name="playerInventory">The local player's inventory; its flags are ignored and it is always counted in full.</param>
    /// <param name="containers">The base's member containers in caller order; duplicate ids are allowed, and the first one wins.</param>
    /// <param name="assignments">The persisted category per container id; a missing id means not Keep. Ids not in <paramref name="containers"/> are ignored.</param>
    /// <param name="options">The ExcludeVehicles and KeepChestsCraftable settings.</param>
    /// <exception cref="ArgumentNullException">A parameter that must not be null is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="containers"/> holds a null element or the player inventory's id, or <paramref name="assignments"/> holds a null value.
    /// </exception>
    public BaseSnapshot(
        ContainerSnapshot playerInventory,
        IReadOnlyList<ContainerSnapshot> containers,
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        EligibilityOptions options)
    {
        if (playerInventory is null)
        {
            throw new ArgumentNullException(nameof(playerInventory));
        }

        if (containers is null)
        {
            throw new ArgumentNullException(nameof(containers));
        }

        if (assignments is null)
        {
            throw new ArgumentNullException(nameof(assignments));
        }

        var containerCopy = new List<ContainerSnapshot>(containers.Count);
        foreach (ContainerSnapshot container in containers)
        {
            if (container is null)
            {
                throw new ArgumentException("Must not contain null elements.", nameof(containers));
            }

            if (container.Id == playerInventory.Id)
            {
                throw new ArgumentException("Must not contain the player inventory.", nameof(containers));
            }

            containerCopy.Add(container);
        }

        var assignmentCopy = new Dictionary<ContainerId, CategoryAssignment>();
        foreach (KeyValuePair<ContainerId, CategoryAssignment> pair in assignments)
        {
            if (pair.Value is null)
            {
                throw new ArgumentException("Must not contain null values.", nameof(assignments));
            }

            assignmentCopy[pair.Key] = pair.Value;
        }

        PlayerInventory = playerInventory;
        Containers = containerCopy.AsReadOnly();
        Assignments = new ReadOnlyDictionary<ContainerId, CategoryAssignment>(assignmentCopy);
        Options = options;
    }

    /// <summary>Gets the local player's inventory, as passed to the constructor.</summary>
    public ContainerSnapshot PlayerInventory { get; }

    /// <summary>Gets a read-only copy of the member containers, in caller order, with duplicates kept.</summary>
    public IReadOnlyList<ContainerSnapshot> Containers { get; }

    /// <summary>Gets a read-only copy of the category assignments.</summary>
    public IReadOnlyDictionary<ContainerId, CategoryAssignment> Assignments { get; }

    /// <summary>Gets the eligibility options.</summary>
    public EligibilityOptions Options { get; }
}
