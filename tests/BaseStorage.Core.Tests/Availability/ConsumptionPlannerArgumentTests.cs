namespace BaseStorage.Core.Tests.Availability;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Availability.PlanFixtures;

/// <summary>AC-15 (ADR-GWIN-028, argument errors and defensive copies): the planner's exceptions, and that its plan views are read-only copies.</summary>
public sealed class ConsumptionPlannerArgumentTests
{
    // GW-U-457 (AC-15): a null snapshot is an ArgumentNullException that names "snapshot".
    [Fact]
    public void NullSnapshotNamesSnapshot()
    {
        ArgumentNullException thrown = Assert.Throws<ArgumentNullException>(
            () => ConsumptionPlanner.Plan(null!, new[] { new CraftRequirement("Wood", 1) }));

        Assert.Equal("snapshot", thrown.ParamName);
    }

    // GW-U-457 (AC-15): null requirements are an ArgumentNullException that names "requirements".
    [Fact]
    public void NullRequirementsNamesRequirements()
    {
        ArgumentNullException thrown = Assert.Throws<ArgumentNullException>(() => ConsumptionPlanner.Plan(WorldP(), null!));

        Assert.Equal("requirements", thrown.ParamName);
    }

    // GW-U-457 (AC-15): a null element is an ArgumentException (exact type) that names "requirements".
    [Fact]
    public void NullElementIsAnArgumentExceptionNamingRequirements()
    {
        ArgumentException thrown = Assert.Throws<ArgumentException>(
            () => ConsumptionPlanner.Plan(WorldP(), new CraftRequirement[] { null! }));

        Assert.Equal("requirements", thrown.ParamName);
    }

    // GW-U-457 (AC-15): clearing the caller's list after Plan returns does not change the plan's requirements or takes.
    [Fact]
    public void ClearingTheCallersListLeavesThePlanUnchanged()
    {
        var requirements = new List<CraftRequirement> { new CraftRequirement("Wood", 14) };
        BaseSnapshot snapshot = WorldP();
        ConsumptionPlan? made = ConsumptionPlanner.Plan(snapshot, requirements);
        Assert.NotNull(made);
        ConsumptionPlan plan = made!;
        ConsumptionTake[] takesBefore = plan.Takes.ToArray();

        requirements.Clear();

        Assert.Equal(new[] { new CraftRequirement("Wood", 14) }, plan.Requirements);
        Assert.Equal(takesBefore, plan.Takes);
        Assert.Equal(new[] { Take(snapshot, 1, 2, 4), Take(snapshot, 1, 0, 10) }, plan.Takes);
    }

    // GW-U-457 (AC-15): the plan's read-only views reject a write made through IList<T>.
    [Fact]
    public void PlanViewsRejectWritesThroughIList()
    {
        ConsumptionPlan plan = Planned(WorldQ(), new CraftRequirement("Wood", 8));

        Assert.Throws<NotSupportedException>(() => ((IList<CraftRequirement>)plan.Requirements).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<ConsumptionTake>)plan.Takes).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<ContainerSnapshot>)plan.ExpectedContainers).Clear());
    }
}
