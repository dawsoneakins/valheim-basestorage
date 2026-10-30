namespace BaseStorage.Core.Availability;

using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>The success result of <see cref="ConsumptionPlanner.Plan"/>: the takes in execution order, and the chests they touch (ADR-GWIN-028).</summary>
/// <remarks>
/// Produced only by <see cref="ConsumptionPlanner.Plan"/>, so its constructor is internal. It has no value equality; callers compare its lists element-wise.
/// </remarks>
public sealed class ConsumptionPlan
{
    /// <summary>Creates a plan. Produced only by <see cref="ConsumptionPlanner.Plan"/>, so it is internal.</summary>
    /// <param name="playerInventoryId">The id of the player inventory.</param>
    /// <param name="requirements">A read-only copy of the input requirements.</param>
    /// <param name="takes">The takes in execution order.</param>
    /// <param name="expectedContainers">The chests that have at least one take, in ascending id order.</param>
    internal ConsumptionPlan(
        ContainerId playerInventoryId,
        IReadOnlyList<CraftRequirement> requirements,
        IReadOnlyList<ConsumptionTake> takes,
        IReadOnlyList<ContainerSnapshot> expectedContainers)
    {
        PlayerInventoryId = playerInventoryId;
        Requirements = requirements;
        Takes = takes;
        ExpectedContainers = expectedContainers;
    }

    /// <summary>Gets the id of the player inventory, so a caller can tell inventory takes from chest takes.</summary>
    public ContainerId PlayerInventoryId { get; }

    /// <summary>Gets a read-only copy of the input requirements, in input order.</summary>
    public IReadOnlyList<CraftRequirement> Requirements { get; }

    /// <summary>Gets the takes in execution order: requirement-major, then source order, then stack order; read-only.</summary>
    public IReadOnlyList<ConsumptionTake> Takes { get; }

    /// <summary>Gets the chest snapshots that have at least one take, in ascending id order, never including the player inventory; read-only.</summary>
    public IReadOnlyList<ContainerSnapshot> ExpectedContainers { get; }
}
