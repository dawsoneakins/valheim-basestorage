namespace BaseStorage.Core.Tests.Restock;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Restock.RestockFixtures;

/// <summary>S-QR-001 AC-5 (contract 7.9, rules Q4, Q14 and P2): the same inputs give the same plan, and the inputs do not change.</summary>
public sealed class RestockDeterminismTests
{
    // GW-U-2517 (Q14, P2)
    [Fact]
    public void Q14_SameInputsGiveTheSamePlanAndInputsAreUnchanged()
    {
        List<CarriedStack> k = MainCarried();
        List<ContainerSnapshot> r = MainChests();
        Dictionary<ContainerId, CategoryAssignment> assign = AssignR();

        List<CarriedStack> kCopy = MainCarried();
        List<ContainerSnapshot> rCopy = MainChests();
        Dictionary<ContainerId, CategoryAssignment> assignCopy = AssignR();

        RestockPlan first = RestockPlanner.Plan(k, Origin(), r, assign, Craftable());
        RestockPlan second = RestockPlanner.Plan(k, Origin(), r, assign, Craftable());

        Assert.Equal(Shape(first), Shape(second));
        for (int i = 0; i < first.Moves.Count; i++)
        {
            Assert.True(first.Moves[i].SourceStack == second.Moves[i].SourceStack, "SourceStack differs at " + i);
            Assert.True(first.Moves[i].TargetStack == second.Moves[i].TargetStack, "TargetStack differs at " + i);
        }

        Assert.Equal(first.CarriedAfter.Count, second.CarriedAfter.Count);
        for (int t = 0; t < first.CarriedAfter.Count; t++)
        {
            Assert.True(first.CarriedAfter[t] == second.CarriedAfter[t], "CarriedAfter[" + t + "] differs.");
        }

        Assert.Equal(first.MovedItems, second.MovedItems);
        Assert.Equal(first.ToppedUpStacks, second.ToppedUpStacks);
        Assert.Equal(first.RemainingRoom, second.RemainingRoom);
        Assert.Equal(first.LeftInUse, second.LeftInUse);
        Assert.Equal(first.LeftDifferentIdentity, second.LeftDifferentIdentity);

        for (int t = 0; t < k.Count; t++)
        {
            Assert.True(k[t].Stack == kCopy[t].Stack && k[t].Marks == kCopy[t].Marks, "carried[" + t + "] changed.");
        }

        for (int c = 0; c < r.Count; c++)
        {
            Assert.True(r[c] == rCopy[c], "chest[" + c + "] changed.");
        }

        Assert.Equal(assignCopy.Count, assign.Count);
        Assert.True(assign[Id(803)] == assignCopy[Id(803)], "The assignment of chest 803 changed.");

        Assert.True(ReferenceEquals(first.Moves[0].SourceStack, r[2].Contents[0]), "The source stack is not the chest's input instance.");
        Assert.True(ReferenceEquals(first.Moves[0].TargetStack, k[0].Stack), "The target stack is not the carried input instance.");

        Assert.Throws<NotSupportedException>(() => ((IList<RestockMove>)first.Moves).Add(first.Moves[0]));
        Assert.Throws<NotSupportedException>(() => ((IList<ItemStackSnapshot>)first.CarriedAfter).Add(k[0].Stack));
    }
}
