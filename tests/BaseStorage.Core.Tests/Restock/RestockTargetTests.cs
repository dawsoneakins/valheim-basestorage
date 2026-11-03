namespace BaseStorage.Core.Tests.Restock;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Restock.RestockFixtures;

/// <summary>S-QR-001 AC-3 (contract 7.5, rule Q3 and O4): which carried entries are targets, and the moves they get.</summary>
public sealed class RestockTargetTests
{
    // GW-U-2491 (Q3)
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void Q03_OnlyHotbarOrFavouriteTypeStacksBelowMaxAreTargets(int caseIndex)
    {
        (List<CarriedStack> carried, MoveRow[] moves) = Case(caseIndex);
        var chests = new List<ContainerSnapshot> { C(950, 1, 0, 4, S("Wood", 50), S("SwordIron", 1, 1), S("Wood", 50, 60)) };

        RestockPlan plan = RestockPlanner.Plan(carried, Origin(), chests, NoAssign(), Craftable());

        Assert.Equal(moves, Shape(plan));
        Assert.Equal(0, plan.RemainingRoom);
        Assert.Equal(carried.Count, plan.CarriedAfter.Count);
    }

    private static (List<CarriedStack> Carried, MoveRow[] Moves) Case(int caseIndex) => caseIndex switch
    {
        0 => (new List<CarriedStack> { U(S("Wood", 10)) }, Array.Empty<MoveRow>()),
        1 => (new List<CarriedStack> { Star(S("Wood", 10)) }, Array.Empty<MoveRow>()),
        2 => (new List<CarriedStack> { new CarriedStack(S("Wood", 10), CarriedMarks.Hotbar | CarriedMarks.StarredSlot) }, new[] { Row(950, 0, 0, 40) }),
        3 => (new List<CarriedStack> { F(S("Wood", 10)) }, new[] { Row(950, 0, 0, 40) }),
        4 => (new List<CarriedStack> { H(S("SwordIron", 1, 1)) }, Array.Empty<MoveRow>()),
        5 => (new List<CarriedStack> { H(S("Wood", 50)) }, Array.Empty<MoveRow>()),
        6 => (new List<CarriedStack> { H(S("Wood", 60)) }, Array.Empty<MoveRow>()),
        7 => (new List<CarriedStack> { H(S("Wood", 10, 60)) }, new[] { Row(950, 2, 0, 50) }),
        8 => (new List<CarriedStack>(), Array.Empty<MoveRow>()),
        _ => throw new ArgumentOutOfRangeException(nameof(caseIndex)),
    };
}
