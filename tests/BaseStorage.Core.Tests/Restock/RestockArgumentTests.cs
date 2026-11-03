namespace BaseStorage.Core.Tests.Restock;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Restock.RestockFixtures;

/// <summary>S-QR-001 AC-6 (contract 5.3 and 7.11, R8): the planner validates its arguments in contract order and nothing else throws.</summary>
public sealed class RestockArgumentTests
{
    // GW-U-2533
    [Theory]
    [InlineData(0, typeof(ArgumentNullException), "carried")]
    [InlineData(1, typeof(ArgumentException), "carried")]
    [InlineData(2, typeof(ArgumentNullException), "containers")]
    [InlineData(3, typeof(ArgumentException), "containers")]
    [InlineData(4, typeof(ArgumentException), "containers")]
    [InlineData(5, typeof(ArgumentNullException), "assignments")]
    [InlineData(6, typeof(ArgumentException), "assignments")]
    [InlineData(7, typeof(ArgumentNullException), "carried")]
    public void PlanValidatesArgumentsInContractOrder(int caseIndex, Type expectedType, string paramName)
    {
        Exception? thrown = Record.Exception(() => { Call(caseIndex); });

        Assert.NotNull(thrown);
        Assert.Equal(expectedType, thrown!.GetType());
        Assert.Equal(paramName, ((ArgumentException)thrown).ParamName);
    }

    private static RestockPlan Call(int caseIndex)
    {
        List<CarriedStack> k = MainCarried();
        List<ContainerSnapshot> r = MainChests();
        return caseIndex switch
        {
            0 => RestockPlanner.Plan(null!, Origin(), r, AssignR(), Craftable()),
            1 => RestockPlanner.Plan(new[] { k[0], null! }, Origin(), r, AssignR(), Craftable()),
            2 => RestockPlanner.Plan(k, Origin(), null!, AssignR(), Craftable()),
            3 => RestockPlanner.Plan(k, Origin(), new[] { r[0], null! }, AssignR(), Craftable()),
            4 => RestockPlanner.Plan(k, Origin(), new[] { r[0], r[0] }, AssignR(), Craftable()),
            5 => RestockPlanner.Plan(k, Origin(), r, null!, Craftable()),
            6 => RestockPlanner.Plan(k, Origin(), r, new Dictionary<ContainerId, CategoryAssignment> { [Id(803)] = null! }, Craftable()),
            7 => RestockPlanner.Plan(null!, Origin(), null!, null!, Craftable()),
            _ => throw new ArgumentOutOfRangeException(nameof(caseIndex)),
        };
    }
}
