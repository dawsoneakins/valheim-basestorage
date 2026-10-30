namespace BaseStorage.Core.Tests.Simulation;

using System.Collections.Generic;
using System.Globalization;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The simulated craft of one peer (AC-3.3, AC-3.4): plan the consumption from the peer's view with <see cref="ConsumptionPlanner"/>
/// and run it through Core's <see cref="ConsumptionExecutor"/> (S-E003-003), polled once per frame: it reserves every chest of the
/// plan, verifies the contents, takes all or nothing and releases. The operation maps the terminal <see cref="ConsumptionResult"/>
/// to an <see cref="OperationOutcome"/> and records what was consumed for the conservation ledger. Not a test row.
/// </summary>
internal sealed class CraftOperation : PeerOperation
{
    private readonly ConsumptionExecution? execution;

    /// <summary>Plans the craft and starts the execution; when the materials are not available the operation ends at once with <see cref="OperationOutcome.NotAvailable"/>.</summary>
    /// <param name="executor">The peer's consumption executor.</param>
    /// <param name="snapshot">The peer's view of the base.</param>
    /// <param name="requirements">The recipe.</param>
    internal CraftOperation(ConsumptionExecutor executor, BaseSnapshot snapshot, IReadOnlyList<CraftRequirement> requirements)
    {
        Requirements = requirements;
        Plan = ConsumptionPlanner.Plan(snapshot, requirements);
        if (Plan is null)
        {
            Outcome = OperationOutcome.NotAvailable;
            return;
        }

        execution = executor.Execute(Plan);
    }

    /// <inheritdoc />
    internal override OperationKind Kind => OperationKind.Craft;

    /// <summary>Gets the recipe.</summary>
    internal IReadOnlyList<CraftRequirement> Requirements { get; }

    /// <summary>Gets the plan, or null when the materials were not available.</summary>
    internal ConsumptionPlan? Plan { get; }

    /// <summary>Gets the terminal result of the execution, once there is one.</summary>
    internal ConsumptionResult? Result { get; private set; }

    /// <summary>Gets the stacks taken; empty unless the craft completed.</summary>
    internal List<ItemStackSnapshot> Consumed { get; } = new List<ItemStackSnapshot>();

    /// <summary>Gets the number of takes that address a chest rather than the inventory.</summary>
    internal int ChestTakes
    {
        get
        {
            if (Plan is null)
            {
                return 0;
            }

            int count = 0;
            foreach (ConsumptionTake take in Plan.Takes)
            {
                if (take.ContainerId != Plan.PlayerInventoryId)
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>Gets the detail of a failed take, for the report.</summary>
    internal string FailureDetail { get; private set; } = string.Empty;

    /// <inheritdoc />
    internal override void Step()
    {
        if (IsDone)
        {
            return;
        }

        ConsumptionResult? result = execution!.Poll();
        if (result is null)
        {
            return;
        }

        Result = result;
        switch (result.Outcome)
        {
            case ConsumptionOutcome.Success:
                foreach (ConsumptionTake take in Plan!.Takes)
                {
                    Consumed.Add(new ItemStackSnapshot(take.Stack.PrefabName, take.Count, take.Stack.Quality, take.Stack.MaxStack));
                }

                Outcome = OperationOutcome.Completed;
                break;
            case ConsumptionOutcome.ReservationFailed:
                Outcome = result.ReservationFailure switch
                {
                    ReservationFailure.Denied => OperationOutcome.Denied,
                    ReservationFailure.Timeout => OperationOutcome.TimedOut,
                    ReservationFailure.OwnerLost => OperationOutcome.Lost,
                    _ => OperationOutcome.Stopped,
                };
                break;
            case ConsumptionOutcome.ContentsChanged:
                Outcome = OperationOutcome.Changed;
                break;
            case ConsumptionOutcome.TakeFailed:
                FailureDetail = string.Format(
                    CultureInfo.InvariantCulture,
                    "a take from #{0} came back {1} after verification; {2} stacks unrestored",
                    result.FailedContainer.Id,
                    result.FailedOperation,
                    result.Unrestored.Count);
                Outcome = OperationOutcome.WriteFailed;
                break;
            default:
                FailureDetail = "the execution ended with " + result.Outcome;
                Outcome = OperationOutcome.WriteFailed;
                break;
        }
    }

    /// <inheritdoc />
    internal override string Describe()
    {
        var parts = new List<string>();
        foreach (CraftRequirement requirement in Requirements)
        {
            parts.Add(requirement.PrefabName + " x" + requirement.Amount.ToString(CultureInfo.InvariantCulture));
        }

        string chests = Plan is null ? "no plan" : Plan.ExpectedContainers.Count.ToString(CultureInfo.InvariantCulture) + " chests";
        return "craft [" + string.Join(", ", parts) + "] " + chests;
    }
}
