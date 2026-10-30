namespace BaseStorage.Core.Organize.Run;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The game facts of one base that the planner needs besides the snapshots (contract section 3): the persisted assignment per
/// chest and the label of the player who has a chest open. Both dictionaries are kept by reference; the planner validates their values.
/// </summary>
public sealed class OrganizeBaseFacts
{
    /// <summary>Creates the facts.</summary>
    /// <param name="assignments">The persisted assignment per chest id; a missing id is unassigned. Not null.</param>
    /// <param name="inUseBy">The holder label per chest id for chests another player has open. Not null.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public OrganizeBaseFacts(
        IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments,
        IReadOnlyDictionary<ContainerId, string> inUseBy)
    {
        Assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));
        InUseBy = inUseBy ?? throw new ArgumentNullException(nameof(inUseBy));
    }

    /// <summary>Gets the persisted assignment per chest id.</summary>
    public IReadOnlyDictionary<ContainerId, CategoryAssignment> Assignments { get; }

    /// <summary>Gets the holder label per chest id for chests another player has open.</summary>
    public IReadOnlyDictionary<ContainerId, string> InUseBy { get; }
}
