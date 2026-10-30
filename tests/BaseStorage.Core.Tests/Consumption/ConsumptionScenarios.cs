namespace BaseStorage.Core.Tests.Consumption;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Tests.Settings;
using Xunit;
using static BaseStorage.Core.Tests.Availability.AvailabilityFixtures;
using static BaseStorage.Core.Tests.Consumption.ConsumptionFixtures;
using static BaseStorage.Core.Tests.Fakes.FakeFixtures;

/// <summary>
/// One run of a named scenario from contract 4.3 (ADR-GWIN-031): the world, the plan, the state before and after, and the result.
/// Each run builds a fresh world. Not a test class.
/// </summary>
internal sealed class ScenarioRun
{
    /// <summary>Creates a run record.</summary>
    /// <param name="layer">The recording layer of the run.</param>
    /// <param name="clock">The manual clock of the run.</param>
    /// <param name="plan">The plan that was executed.</param>
    /// <param name="before">The world before Execute.</param>
    /// <param name="after">The world once the result is terminal.</param>
    /// <param name="result">The terminal result.</param>
    internal ScenarioRun(
        RecordingContainerLayer layer,
        ManualClock clock,
        ConsumptionPlan plan,
        IReadOnlyList<ContainerSnapshot> before,
        IReadOnlyList<ContainerSnapshot> after,
        ConsumptionResult result)
    {
        Layer = layer;
        Clock = clock;
        Plan = plan;
        Before = before;
        After = after;
        Result = result;
    }

    /// <summary>Gets the recording layer.</summary>
    internal RecordingContainerLayer Layer { get; }

    /// <summary>Gets the manual clock.</summary>
    internal ManualClock Clock { get; }

    /// <summary>Gets the plan.</summary>
    internal ConsumptionPlan Plan { get; }

    /// <summary>Gets the world before Execute.</summary>
    internal IReadOnlyList<ContainerSnapshot> Before { get; }

    /// <summary>Gets the world once the result is terminal.</summary>
    internal IReadOnlyList<ContainerSnapshot> After { get; }

    /// <summary>Gets the terminal result.</summary>
    internal ConsumptionResult Result { get; }
}

/// <summary>
/// The named scenarios of contract 4.3 (F1 to F15 subset), as one table. Each name builds a fresh world on W, arranges the faults, executes
/// and drives the execution to a result. "happy" is contract 4.2. "unknown" is the chest-missing case of GW-U-505 and GW-U-507. Not a test class.
/// </summary>
internal static class ConsumptionScenarios
{
    private static readonly string[] Failures = { "F1", "F2", "F4", "F5", "F6", "F7", "F8", "F10", "F12" };

    /// <summary>The nine failure scenarios of GW-U-537, as MemberData rows.</summary>
    /// <returns>One row per scenario name.</returns>
    public static IEnumerable<object[]> FailureCases() => Failures.Select(name => new object[] { name });

    /// <summary>Runs a named scenario on a fresh world and returns the run.</summary>
    /// <param name="name">The scenario name.</param>
    /// <returns>The run.</returns>
    internal static ScenarioRun Run(string name)
    {
        ManualClock clock = new(TimeSpan.FromSeconds(10));
        FakeSettingsSource settings = new();
        RecordingContainerLayer layer = name switch
        {
            "F5" => LayerW(chest7: Make(7u, 6f, 0f, 8f, 5, 2, ContainerFlags.PlayerBuilt, Wood(9))),
            "F6" => LayerW(inventory: Make(1u, 0f, 0f, 0f, 8, 4, ContainerFlags.None, Wood(2))),
            "unknown" => LayerOf(InventoryW(), Chest5W()),
            _ => LayerW(),
        };
        ConsumptionPlan plan = PlanW();
        Arrange(name, layer);
        IReadOnlyList<ContainerSnapshot> before = layer.Inner.Capture();
        ConsumptionExecution execution = Executor(layer, clock, settings).Execute(plan);

        ConsumptionResult result;
        switch (name)
        {
            case "F2":
                Assert.Null(execution.Poll());
                clock.Advance(TimeSpan.FromMilliseconds(1499));
                Assert.Null(execution.Poll());
                clock.Advance(TimeSpan.FromMilliseconds(1));
                result = Done(execution.Poll());
                break;
            case "F4":
                Assert.Null(execution.Poll());
                layer.Inner.InjectFault(Id(5), FakeFault.OwnerDisconnect);
                result = Drive(execution);
                break;
            case "F12":
                Assert.Null(execution.Poll());
                execution.Cancel();
                result = Done(execution.Result);
                break;
            case "F10":
                try
                {
                    result = Drive(execution);
                }
                catch (InvalidOperationException)
                {
                    result = Done(execution.Result);
                }

                break;
            default:
                result = Drive(execution);
                break;
        }

        return new ScenarioRun(layer, clock, plan, before, layer.Inner.Capture(), result);
    }

    private static void Arrange(string name, RecordingContainerLayer layer)
    {
        int removes = 0;
        switch (name)
        {
            case "F1":
                layer.Inner.InjectFault(Id(7), FakeFault.ReservationRefused);
                break;
            case "F2":
            case "F12":
                layer.Inner.AutoResolveReservations = false;
                break;
            case "F7":
                layer.Inner.InjectFault(Id(7), FakeFault.ChestChanged);
                break;
            case "F8":
                layer.Inner.InjectFault(Id(7), FakeFault.ChestChanged);
                layer.Inner.InjectFault(Id(5), FakeFault.DestinationFull);
                break;
            case "F9":
                layer.Inner.InjectFault(Id(7), FakeFault.ChestChanged);
                layer.Inner.InjectFault(Id(5), FakeFault.DestinationFull);
                layer.Inner.InjectFault(Id(1), FakeFault.DestinationFull);
                break;
            case "F10":
                layer.ThrowOn = call => call.StartsWith("Remove:", StringComparison.Ordinal) && ++removes == 2
                    ? new InvalidOperationException("layer")
                    : null;
                break;
            default:
                break;
        }
    }
}
