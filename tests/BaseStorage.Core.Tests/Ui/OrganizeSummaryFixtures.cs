namespace BaseStorage.Core.Tests.Ui;

using System.Collections.Generic;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Organize;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>
/// The S-E004-009 preview and summary fixtures (ui-strings contract section 10.3). Every plan is built through the planner from
/// literal chests, and every expected text is a literal copied from the contract. A fixture holder, not a test class.
/// </summary>
internal static class OrganizeSummaryFixtures
{
    /// <summary>A threshold no chest reaches, so chests holding items stay sources (GW-U-571 no-throw value).</summary>
    internal const int NoAutoAssign = 101;

    /// <summary>Contract 10.3 row G: the spec AC-4.7 example line.</summary>
    internal const string GoldenPreview =
        "Organize base: 214 stacks to move across 23 chests. 2 chests will be claimed for Metals & Ores. 4 stacks have no room. Some categories ran out of space. Add a chest or give one that category. 1 chest skipped (in use by Erik).";

    /// <summary>Contract 10.3 row G: the body of the spec example.</summary>
    internal const string GoldenBody =
        "214 stacks to move across 23 chests. 2 chests will be claimed for Metals & Ores. 4 stacks have no room. Some categories ran out of space. Add a chest or give one that category. 1 chest skipped (in use by Erik).";

    /// <summary>Contract 10.3 row G: the end-of-run line of the spec example after a complete run.</summary>
    internal const string GoldenSummary = "Organized 214 stacks; 1 chest skipped (in use by Erik); 4 stacks had no room";

    /// <summary>Contract 10.3 row S1: every clause in its singular form, plus two reason groups.</summary>
    internal const string SingularBody =
        "1 stack to move across 1 chest. 1 chest will be claimed for Wood. 1 stack has no room. Some categories ran out of space. Add a chest or give one that category. 1 chest skipped (in use). 1 chest skipped (Keep).";

    /// <summary>Contract 10.3 row R: nothing to move and three reason groups, names joined and de-duplicated.</summary>
    internal const string GroupsBody =
        "Nothing to move. 3 chests skipped (in use by Erik, Freya). 2 chests skipped (not eligible). 2 chests skipped (unclear contents).";

    /// <summary>Contract 10.3 row C: claimed sentences in catalog order, not claim order.</summary>
    internal const string TwoCategoriesBody =
        "2 stacks to move across 2 chests. 1 chest will be claimed for Metals & Ores. 1 chest will be claimed for Wood.";

    /// <summary>Contract 10.3 row Z: an empty base.</summary>
    internal const string EmptyPreview = "Organize base: Nothing to move.";

    /// <summary>
    /// The golden base: 21 Metals &amp; Ores chests of 10 slots, 2 empty overflow chests of 2 slots, 11 source chests holding 218 full
    /// CopperOre stacks, and one chest Erik has open. Threshold 101, so the sources stay sources. The plan moves 210 stacks into the
    /// category chests and 4 into the two claimed chests; the other 4 stacks have no room.
    /// </summary>
    internal static PlanInput Golden()
    {
        var chests = new List<ContainerSnapshot>();
        var assignments = new List<(uint, CategoryAssignment)>();
        for (uint id = 1; id <= 21; id++)
        {
            chests.Add(C(id, id, 0, 10));
            assignments.Add((id, CategoryAssignment.ForCategory("Metals & Ores")));
        }

        chests.Add(C(31, 31, 0, 2));
        chests.Add(C(32, 32, 0, 2));

        for (uint id = 41; id <= 51; id++)
        {
            int stacks = id == 51 ? 18 : 20;
            var contents = new ItemStackSnapshot[stacks];
            for (int i = 0; i < stacks; i++)
            {
                contents[i] = S("CopperOre", 50);
            }

            chests.Add(C(id, id, 5, 20, contents));
        }

        chests.Add(CF(61, 61, 5, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Wood", 10)));

        return Make(chests.ToArray(), Assign(assignments.ToArray()), InUse((61, "Erik")), NoAutoAssign);
    }

    /// <summary>The singular base: one full Wood chest, one overflow slot, a source with two Wood stacks, a Keep chest and a nameless in-use chest.</summary>
    internal static PlanInput Singular() => Make(
        new[]
        {
            C(1, 0, 0, 1, S("Wood", 50)),
            C(2, 1, 0, 1),
            C(3, 2, 0, 2, S("Wood", 50), S("Wood", 50)),
            C(4, 3, 0, 1, S("Stone", 5)),
            CF(5, 4, 0, 1, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Stone", 5)),
        },
        Assign((1, CategoryAssignment.ForCategory("Wood")), (4, CategoryAssignment.Keep)),
        threshold: NoAutoAssign);

    /// <summary>The reason-group base: nothing usable, three in-use chests (Erik twice, Freya once), two tombstones and two chests with unclear contents.</summary>
    internal static PlanInput Groups() => Make(
        new[]
        {
            CF(11, 0, 0, 1, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Wood", 5)),
            CF(12, 1, 0, 1, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Wood", 5)),
            CF(13, 2, 0, 1, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Wood", 5)),
            CF(14, 3, 0, 1, ContainerFlags.Tombstone, S("Wood", 5)),
            CF(15, 4, 0, 1, ContainerFlags.Tombstone, S("Wood", 5)),
            C(16, 5, 0, 1, S("Wood", 10), S("Wood", 10)),
            C(17, 6, 0, 1, S("Wood", 60)),
        },
        inUseBy: InUse((11, "Erik"), (12, "Freya"), (13, "Erik")),
        threshold: NoAutoAssign);

    /// <summary>The two-category base: Wood is claimed first (its source comes first), Metals &amp; Ores second; the catalog lists Metals &amp; Ores first.</summary>
    internal static PlanInput TwoCategories() => Make(
        new[]
        {
            C(1, 0, 0, 1, S("Wood", 50)),
            C(2, 1, 0, 1, S("CopperOre", 50)),
            C(3, 2, 0, 1, S("Wood", 50)),
            C(4, 3, 0, 1, S("CopperOre", 50)),
            C(5, 4, 0, 1),
            C(6, 5, 0, 1),
        },
        Assign((1, CategoryAssignment.ForCategory("Wood")), (2, CategoryAssignment.ForCategory("Metals & Ores"))),
        threshold: NoAutoAssign);

    /// <summary>A base whose only chest is open by a player whose name holds a line break.</summary>
    internal static PlanInput BrokenName() => Make(
        new[] { CF(1, 0, 0, 1, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Wood", 5)) },
        inUseBy: InUse((1, "Erik\r\nthe Red")),
        threshold: NoAutoAssign);

    /// <summary>An empty base.</summary>
    internal static PlanInput Empty() => Make(new ContainerSnapshot[0]);

    /// <summary>Plans one input.</summary>
    internal static OrganizePlan Plan(PlanInput input) => Run(input);
}
