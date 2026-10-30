namespace BaseStorage.Core.Tests.Organize;

using System;
using System.Linq;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.PlanAssert;

/// <summary>GW-U-556 to GW-U-562: skip classification with reasons, each chest laid out or skipped exactly once (AC-7 to AC-10).</summary>
public sealed class OrganizeSkipTests
{
    // GW-U-556
    [Theory]
    [InlineData("501-keep")]
    [InlineData("502-keep-empty")]
    [InlineData("501-keep-craftable")]
    [InlineData("513-keep-in-use")]
    public void KeepChestIsSkippedWithTheKeepReasonAndNeverPooled(string name)
    {
        uint chest = uint.Parse(name.Substring(0, 3));
        OrganizePlan plan = Run(SkipCase(name));

        Assert.Equal(Skip(chest, SkipReason.Keep, "Keep", null, ExclusionReason.Keep), Assert.Single(SkipsOf(plan)));
        Assert.Empty(plan.AutoAssign.OverflowChests);
        Assert.Null(plan.FindLayout(Id(chest)));
    }

    // GW-U-556
    [Fact]
    public void KeepChestInBaseEIsSkippedUntouchedAndNeverNamedInAMove()
    {
        PlanInput input = E();
        OrganizePlan plan = Run(input);

        Assert.Equal(Skip(205, SkipReason.Keep, "Keep", null, ExclusionReason.Keep), SkipsOf(plan)[0]);
        Assert.Same(input.Containers[4], plan.FindSkipped(Id(205))!.Container);
        Assert.Null(plan.FindLayout(Id(205)));
        Assert.DoesNotContain(plan.Moves, m => m.Source == Id(205) || m.Destination == Id(205));
        Assert.DoesNotContain(plan.NoRoom, e => e.Source == Id(205));
    }

    // GW-U-557
    [Theory]
    [InlineData("503-named")]
    [InlineData("504-flag-only")]
    [InlineData("505-dictionary-only")]
    [InlineData("506-blank-name")]
    [InlineData("507-empty")]
    public void InUseChestNamesThePlayerOnlyWhenTheNameIsKnown(string name)
    {
        (uint chest, string reason, string? player) = InUseExpected(name);
        OrganizePlan plan = Run(SkipCase(name));

        Assert.Equal(Skip(chest, SkipReason.InUse, reason, player, null), Assert.Single(SkipsOf(plan)));
        Assert.Empty(plan.AutoAssign.OverflowChests);
    }

    // GW-U-557
    [Fact]
    public void UnknownInUseIdIsIgnoredAndTheFlagAloneStillSkipsChest206()
    {
        PlanInput input = E() with { InUseBy = InUse((999, "Erik")) };
        OrganizePlan plan = Run(input);

        Assert.Equal(
            Skip(206, SkipReason.InUse, "In use by another player", null, null),
            SkipsOf(plan).Single(row => row.Id == 206));
    }

    // GW-U-558
    [Fact]
    public void InUseCategoryChestIsNotADestinationAndItsItemsAreNotCounted()
    {
        OrganizePlan plan = Run(E());

        Assert.Null(plan.AutoAssign.Find(Id(206)));
        Assert.Equal(new uint[] { 201 }, plan.AutoAssign.GetCategoryChests("Wood").Select(c => c.Id.Id));
        Assert.DoesNotContain(plan.Moves, m => m.Destination == Id(206));
        Assert.DoesNotContain(plan.Moves, m => m.Source == Id(206));
        Assert.DoesNotContain(plan.NoRoom, e => e.Source == Id(206));
        Assert.Null(plan.FindLayout(Id(206)));
    }

    // GW-U-559
    [Theory]
    [InlineData("508-tombstone")]
    [InlineData("509-vehicle")]
    [InlineData("510-not-player-built")]
    [InlineData("511-access-denied")]
    [InlineData("512-keep-tombstone")]
    [InlineData("514-tombstone-in-use")]
    public void IneligibleChestIsSkippedWithTheEligibilityReason(string name)
    {
        (uint chest, string reason, ExclusionReason exclusion) = IneligibleExpected(name);
        OrganizePlan plan = Run(SkipCase(name));

        Assert.Equal(Skip(chest, SkipReason.Ineligible, reason, null, exclusion), Assert.Single(SkipsOf(plan)));
    }

    // GW-U-559
    [Fact]
    public void VehicleIsUsableWhenVehiclesAreNotExcluded()
    {
        OrganizePlan plan = Run(SkipCase("509-vehicle-allowed"));

        Assert.NotNull(plan.FindLayout(Id(509)));
        Assert.Null(plan.FindSkipped(Id(509)));
    }

    // GW-U-560
    [Theory]
    [InlineData("515-too-many-stacks", "Invalid contents: more stacks than slots")]
    [InlineData("516-above-max", "Invalid contents: stack above max stack")]
    public void UnclearContentsAreSkippedAsInvalidAndNeverUsed(string name, string reason)
    {
        uint chest = uint.Parse(name.Substring(0, 3));
        OrganizePlan plan = Run(SkipCase(name));

        Assert.Equal(Skip(chest, SkipReason.InvalidContents, reason, null, null), Assert.Single(SkipsOf(plan)));
    }

    // GW-U-560 (derived)
    [Fact]
    public void MoreStacksThanSlotsIsReportedBeforeAboveMaxStack()
    {
        OrganizePlan plan = Run(SkipCase("precedence-both-problems"));

        Assert.Equal(
            Skip(1201, SkipReason.InvalidContents, "Invalid contents: more stacks than slots", null, null),
            Assert.Single(SkipsOf(plan)));
    }

    // GW-U-560 (derived)
    [Fact]
    public void WoodIsNotPlacedIntoAnInvalidWoodChestEvenWithFreeSlots()
    {
        PlanInput input = Make(
            new[]
            {
                C(1301, 0, 0, 2, S("Wood", 5)),
                C(1302, 2, 0, 3, S("Wood", 60)),
            },
            Assign((1301, CategoryAssignment.ForCategory("Stone")), (1302, CategoryAssignment.ForCategory("Wood"))));
        OrganizePlan plan = Run(input);

        Assert.Empty(plan.Moves);
        Assert.Equal(new[] { NoRoom(1301, 0, "Wood", 1, 50, 5, "Wood") }, NoRoomOf(plan));
        Assert.Equal(
            Skip(1302, SkipReason.InvalidContents, "Invalid contents: stack above max stack", null, null),
            Assert.Single(SkipsOf(plan)));
    }

    // GW-U-561
    [Fact]
    public void CombinedSkipBaseSkipsSixteenChestsInSnapshotOrderWithOneReasonEach()
    {
        PlanInput input = SkipCombined();
        OrganizePlan plan = Run(input);

        Assert.Equal(
            new[]
            {
                Skip(501, SkipReason.Keep, "Keep", null, ExclusionReason.Keep),
                Skip(502, SkipReason.Keep, "Keep", null, ExclusionReason.Keep),
                Skip(503, SkipReason.InUse, "In use by Erik", "Erik", null),
                Skip(504, SkipReason.InUse, "In use by another player", null, null),
                Skip(505, SkipReason.InUse, "In use by Astrid", "Astrid", null),
                Skip(506, SkipReason.InUse, "In use by another player", null, null),
                Skip(507, SkipReason.InUse, "In use by another player", null, null),
                Skip(508, SkipReason.Ineligible, "Ineligible: tombstone", null, ExclusionReason.Tombstone),
                Skip(509, SkipReason.Ineligible, "Ineligible: vehicle", null, ExclusionReason.Vehicle),
                Skip(510, SkipReason.Ineligible, "Ineligible: not player-built", null, ExclusionReason.NotPlayerBuilt),
                Skip(511, SkipReason.Ineligible, "Ineligible: access denied", null, ExclusionReason.AccessDenied),
                Skip(512, SkipReason.Ineligible, "Ineligible: tombstone", null, ExclusionReason.Tombstone),
                Skip(513, SkipReason.Keep, "Keep", null, ExclusionReason.Keep),
                Skip(514, SkipReason.Ineligible, "Ineligible: tombstone", null, ExclusionReason.Tombstone),
                Skip(515, SkipReason.InvalidContents, "Invalid contents: more stacks than slots", null, null),
                Skip(516, SkipReason.InvalidContents, "Invalid contents: stack above max stack", null, null),
            },
            SkipsOf(plan));
        Assert.Equal(16, SkipsOf(plan).Select(row => row.Id).Distinct().Count());
        Assert.All(plan.Skipped, s => Assert.False(string.IsNullOrEmpty(s.Reason)));
        Assert.Empty(plan.Layouts);
        Assert.Empty(plan.Moves);
        Assert.Empty(plan.NoRoom);
    }

    // GW-U-562
    [Fact]
    public void EveryChestIsLaidOutOrSkippedExactlyOnceInEveryFixture()
    {
        foreach (string name in AllFixtureNames)
        {
            PlanInput input = Named(name);
            OrganizePlan plan = Run(input);

            foreach (ContainerSnapshot container in input.Containers)
            {
                bool laid = plan.FindLayout(container.Id) is not null;
                bool skipped = plan.FindSkipped(container.Id) is not null;
                Assert.True(laid ^ skipped, name + ": chest " + container.Id.Id + " must be laid out or skipped exactly once");
            }

            Assert.Equal(input.Containers.Length, plan.Layouts.Count + plan.Skipped.Count);
            var skippedIds = plan.Skipped.Select(s => s.Id).ToHashSet();
            Assert.DoesNotContain(plan.Moves, m => skippedIds.Contains(m.Source) || skippedIds.Contains(m.Destination));
            Assert.DoesNotContain(plan.NoRoom, e => skippedIds.Contains(e.Source));
            Assert.DoesNotContain(plan.Layouts, l => skippedIds.Contains(l.Id));
        }
    }

    // GW-U-562
    [Fact]
    public void LookupsOfSkippedUnknownAndDefaultIdsAreNull()
    {
        OrganizePlan plan = Run(E());

        Assert.Null(plan.FindLayout(Id(205)));
        Assert.Null(plan.FindSkipped(Id(201)));
        Assert.Null(plan.FindLayout(Id(999)));
        Assert.Null(plan.FindSkipped(default));
    }

    private static (uint Chest, string Reason, string? PlayerName) InUseExpected(string name) => name switch
    {
        "503-named" => (503, "In use by Erik", "Erik"),
        "504-flag-only" => (504, "In use by another player", null),
        "505-dictionary-only" => (505, "In use by Astrid", "Astrid"),
        "506-blank-name" => (506, "In use by another player", null),
        "507-empty" => (507, "In use by another player", null),
        _ => throw new ArgumentException("Unknown in-use case: " + name, nameof(name)),
    };

    private static (uint Chest, string Reason, ExclusionReason Exclusion) IneligibleExpected(string name) => name switch
    {
        "508-tombstone" => (508, "Ineligible: tombstone", ExclusionReason.Tombstone),
        "509-vehicle" => (509, "Ineligible: vehicle", ExclusionReason.Vehicle),
        "510-not-player-built" => (510, "Ineligible: not player-built", ExclusionReason.NotPlayerBuilt),
        "511-access-denied" => (511, "Ineligible: access denied", ExclusionReason.AccessDenied),
        "512-keep-tombstone" => (512, "Ineligible: tombstone", ExclusionReason.Tombstone),
        "514-tombstone-in-use" => (514, "Ineligible: tombstone", ExclusionReason.Tombstone),
        _ => throw new ArgumentException("Unknown ineligible case: " + name, nameof(name)),
    };
}
