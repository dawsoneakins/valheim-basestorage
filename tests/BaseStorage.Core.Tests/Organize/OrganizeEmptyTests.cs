namespace BaseStorage.Core.Tests.Organize;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.PlanAssert;

/// <summary>GW-U-570: empty bases, nothing to move, no category chests and all-skipped bases never throw (contract 6.7, AC-13).</summary>
public sealed class OrganizeEmptyTests
{
    // GW-U-570
    [Theory]
    [InlineData("empty")]
    [InlineData("701")]
    [InlineData("201+202")]
    [InlineData("702")]
    [InlineData("702+703")]
    [InlineData("704")]
    [InlineData("all-skipped")]
    public void EmptyAndNothingToMoveCasesPlanAsTheContractSays(string name)
    {
        PlanInput input = EmptyCase(name);

        OrganizePlan plan = Run(input);

        switch (name)
        {
            case "empty":
                Assert.Empty(plan.Layouts);
                Assert.Empty(plan.Skipped);
                Assert.Empty(plan.Moves);
                Assert.Empty(plan.NoRoom);
                Assert.Empty(plan.AutoAssign.Chests);
                break;
            case "701":
                Assert.Empty(plan.Moves);
                Assert.Empty(plan.NoRoom);
                Assert.Equal(new[] { Layout(701, ChestRole.Category, "Wood", S("Wood", 50)) }, LayoutsOf(plan));
                break;
            case "201+202":
                Assert.Empty(plan.Moves);
                Assert.Empty(plan.NoRoom);
                Assert.Equal(
                    new[]
                    {
                        Layout(201, ChestRole.Category, "Wood", S("Wood", 30)),
                        Layout(202, ChestRole.Category, "Stone", S("Stone", 20)),
                    },
                    LayoutsOf(plan));
                break;
            case "702":
                Assert.Empty(plan.Moves);
                Assert.Equal(
                    new[]
                    {
                        NoRoom(702, 0, "SwordIron", 1, 1, 1, "Gear"),
                        NoRoom(702, 1, "Resin", 1, 50, 3, "Misc"),
                        NoRoom(702, 2, "Wood", 1, 50, 2, "Wood"),
                    },
                    NoRoomOf(plan));
                Assert.Equal(
                    new[] { Layout(702, ChestRole.Source, null, S("SwordIron", 1, 1), S("Resin", 3), S("Wood", 2)) },
                    LayoutsOf(plan));
                break;
            case "702+703":
                Assert.Equal(new[] { Move(702, 0, 703, "SwordIron", 1, 1, 1, "Gear") }, MovesOf(plan));
                Assert.Equal(
                    new[]
                    {
                        NoRoom(702, 1, "Resin", 1, 50, 3, "Misc"),
                        NoRoom(702, 2, "Wood", 1, 50, 2, "Wood"),
                    },
                    NoRoomOf(plan));
                Assert.Equal(
                    new[]
                    {
                        Layout(702, ChestRole.Source, null, S("Resin", 3), S("Wood", 2)),
                        Layout(703, ChestRole.ClaimedOverflow, "Gear", S("SwordIron", 1, 1)),
                    },
                    LayoutsOf(plan));
                break;
            case "704":
                Assert.Empty(plan.Moves);
                Assert.Empty(plan.NoRoom);
                Assert.Equal(new[] { Layout(704, ChestRole.UnusedOverflow, null) }, LayoutsOf(plan));
                break;
            case "all-skipped":
                Assert.Empty(plan.Moves);
                Assert.Empty(plan.NoRoom);
                Assert.Empty(plan.Layouts);
                Assert.Equal(
                    new[]
                    {
                        Skip(501, SkipReason.Keep, "Keep", null, ExclusionReason.Keep),
                        Skip(508, SkipReason.Ineligible, "Ineligible: tombstone", null, ExclusionReason.Tombstone),
                    },
                    SkipsOf(plan));
                break;
            default:
                throw new ArgumentException("Unknown empty case: " + name, nameof(name));
        }
    }

    private static PlanInput EmptyCase(string name) => name switch
    {
        "empty" => new PlanInput(
            Array.Empty<ContainerSnapshot>(),
            new Dictionary<ContainerId, CategoryAssignment>(),
            new Dictionary<ContainerId, string>(),
            CategoryCatalog.Empty,
            new Dictionary<string, ItemKind>(StringComparer.Ordinal),
            Options(),
            DefaultThreshold),
        "701" => Make(new[] { C(701, 0, 0, 2, S("Wood", 50)) }, Assign((701, CategoryAssignment.ForCategory("Wood")))),
        "201+202" => Make(
            new[] { C(201, 0, 0, 2, S("Wood", 30)), C(202, 2, 0, 4, S("Stone", 20)) },
            Assign((201, CategoryAssignment.ForCategory("Wood")), (202, CategoryAssignment.ForCategory("Stone")))),
        "702" => Solo702(),
        "702+703" => Pair702(),
        "704" => Make(new[] { C(704, 0, 0, 2) }),
        "all-skipped" => Make(
            new[] { C(501, 0, 0, 2, S("Wood", 10)), CF(508, 0, 0, 2, ContainerFlags.Tombstone, S("Wood", 3)) },
            Assign((501, CategoryAssignment.Keep))),
        _ => throw new ArgumentException("Unknown empty case: " + name, nameof(name)),
    };
}
