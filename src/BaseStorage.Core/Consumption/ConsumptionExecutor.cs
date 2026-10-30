namespace BaseStorage.Core.Consumption;

using System;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Settings;

/// <summary>
/// Carries out a consumption plan all or nothing: it reserves every chest the plan touches, checks that the chests still hold what was
/// planned, takes the items and releases every reservation (ADR-C-007, ADR-GWIN-030). It is polled, never blocking. An executor runs at
/// most one execution at a time. It is not thread-safe.
/// </summary>
public sealed class ConsumptionExecutor
{
    private readonly IContainerLayer layer;
    private readonly IClock clock;
    private readonly BaseStorageSettings settings;
    private ConsumptionExecution? pending;

    /// <summary>Creates an executor. It stores its arguments and makes no call on any of them.</summary>
    /// <param name="layer">The container layer that reservations, reads and takes go through. Must not be null.</param>
    /// <param name="clock">The only time source, read through the reservations. Must not be null.</param>
    /// <param name="settings">The settings read path, used once per execution for the reserve timeout. Must not be null.</param>
    /// <exception cref="ArgumentNullException">layer, clock or settings is null; ParamName names it.</exception>
    public ConsumptionExecutor(IContainerLayer layer, IClock clock, BaseStorageSettings settings)
    {
        this.layer = layer ?? throw new ArgumentNullException(nameof(layer));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>Starts executing a plan. It reads ReserveTimeoutMs once, then requests a reservation for every chest in the plan, in ascending id order. It takes nothing.</summary>
    /// <param name="plan">The plan from the planner. Must not be null.</param>
    /// <returns>The pending execution. Advance it with Poll.</returns>
    /// <exception cref="ArgumentNullException">plan is null. ParamName is "plan".</exception>
    /// <exception cref="InvalidOperationException">An earlier execution from this executor is not complete.</exception>
    public ConsumptionExecution Execute(ConsumptionPlan plan)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (pending != null && !pending.IsComplete)
        {
            throw new InvalidOperationException("An earlier execution from this executor is not complete.");
        }

        pending = null;
        int timeout = settings.ReserveTimeoutMs;
        if (timeout < 1)
        {
            timeout = 1;
        }

        var registry = new ChestReservations<ConsumptionPlan>(clock, timeout);
        var execution = new ConsumptionExecution(layer, plan, registry);
        execution.Start();
        pending = execution;
        return execution;
    }
}
