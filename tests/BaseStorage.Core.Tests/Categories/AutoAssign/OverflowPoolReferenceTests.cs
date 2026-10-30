namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-5 (contract 3.2 step 3, 5.6): the reference set is fixed for the life of a pool; equal distances go by snapshot order.</summary>
public sealed class OverflowPoolReferenceTests
{
    // GW-U-515
    [Fact]
    public void ContractCaseMetalsClaimsReturnTheNearestChestEachTime()
    {
        AutoAssignResult r = ResolveB();

        Assert.Equal(new uint?[] { 108, 105, 109 }, ClaimIds(r.CreateOverflowPool(), "Metals & Ores", "Metals & Ores", "Metals & Ores"));
    }

    // GW-U-515
    [Fact]
    public void ClaimedChestsDoNotJoinTheReferenceSetOfTheCategory()
    {
        ContainerSnapshot[] containers =
        {
            C(501, 0, 0, S("Stone", 10)),
            C(502, 10, 0),
            C(503, 20, 0),
            C(504, -11, 0),
        };
        var assignments = new Dictionary<ContainerId, CategoryAssignment>
        {
            [Id(501)] = CategoryAssignment.ForCategory("Stone"),
        };
        AutoAssignResult r = ResolveWith(containers, assignments);

        // The only Stone chest is 501 at (0, 0). Squared distances to it: 502 = 100, 503 = 400, 504 = 121.
        // If a claimed chest joined the reference set, the second claim would be 503 (100 from 502).
        Assert.Equal(new uint?[] { 502, 504, 503, null }, ClaimIds(r.CreateOverflowPool(), "Stone", "Stone", "Stone", "Stone"));
    }

    // GW-U-516
    [Fact]
    public void EqualDistancesGoToTheEarlierSnapshotChestNotTheLowerId()
    {
        AutoAssignResult rt = ResolveWith(T(), TAssignments());

        Assert.Equal(new uint?[] { 303, 302, 304, null }, ClaimIds(rt.CreateOverflowPool(), "Wood", "Wood", "Wood", "Wood"));
    }

    // GW-U-516
    [Fact]
    public void WithNoChestOfTheCategoryClaimsFollowSnapshotOrder()
    {
        AutoAssignResult rt = ResolveWith(T(), TAssignments());

        Assert.Equal(new uint?[] { 303, 302, 304 }, ClaimIds(rt.CreateOverflowPool(), "Stone", "Stone", "Stone"));
    }

    // GW-U-516
    [Fact]
    public void ReversedInputGivesReversedChestsAndClaimsStillFollowSnapshotOrder()
    {
        ContainerSnapshot[] reversed = T();
        Array.Reverse(reversed);

        AutoAssignResult rt = ResolveWith(reversed, TAssignments());

        Assert.Equal(new uint[] { 304, 302, 303, 301 }, EntryIds(rt.Chests));
        AssertChest("chest 301", AutoAssignOutcome.AlreadyAssigned, "Wood", At(rt, 301));
        AssertChest("chest 302", AutoAssignOutcome.Overflow, null, At(rt, 302));
        AssertChest("chest 303", AutoAssignOutcome.Overflow, null, At(rt, 303));
        AssertChest("chest 304", AutoAssignOutcome.Overflow, null, At(rt, 304));
        Assert.Equal(new uint?[] { 302, 303 }, ClaimIds(rt.CreateOverflowPool(), "Wood", "Wood"));
    }
}
