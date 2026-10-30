namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Organize;
using static BaseStorage.Core.Tests.Categories.AutoAssign.MixedChestFixtures;

/// <summary>
/// S-A-001 (AC-6, AC-12; mixed-chest-category-contracts.md 2.2, MC2 and 6.10): the argument checks run in order, nothing else
/// throws, the inputs are never changed, and the result is always a new instance.
/// </summary>
public sealed class MixedChestArgumentTests
{
    // GW-U-2428 (AC-6, MC2, 6.10): the result is a new instance even with no change, the inputs are unchanged, and the result is read-only.
    [Fact]
    public void TheResultIsANewInstanceAndTheInputsAreUnchanged()
    {
        ContainerSnapshot[] chests = M();
        var persisted = PersistedM();
        int persistedCount = persisted.Count;
        CategoryCatalog catalog = Catalog();
        var kinds = Kinds();

        AutoAssignResult zeroThreshold = AutoAssigner.Resolve(M(), PersistedM(), catalog, kinds, 0);
        AutoAssignResult unchanged = AutoAssigner.AssignMixedChests(zeroThreshold, catalog, kinds);
        Assert.False(ReferenceEquals(unchanged, zeroThreshold));

        AutoAssignResult resolved = Resolve(chests, persisted);
        ChestRow[] before = RowsOf(resolved);
        AutoAssignResult stepped = AutoAssigner.AssignMixedChests(resolved, catalog, kinds);

        Assert.False(ReferenceEquals(stepped, resolved));
        Assert.Equal(before, RowsOf(resolved));
        Assert.Equal(AutoAssignOutcome.SourceOnly, resolved.Find(Id(502))!.Outcome);
        Assert.Equal(AutoAssignOutcome.SourceOnly, resolved.Find(Id(503))!.Outcome);
        Assert.Equal(AutoAssignOutcome.SourceOnly, resolved.Find(Id(504))!.Outcome);
        Assert.Equal(persistedCount, persisted.Count);
        Assert.Equal(4, chests[1].Contents.Count);
        Assert.Equal(6, chests[5].Contents.Count);

        AutoAssignResult mixed = Mixed(M(), PersistedM());
        Assert.Throws<NotSupportedException>(() => ((IList<ChestAutoAssignment>)mixed.Chests).Add(mixed.Chests[0]));
    }

    // GW-U-2460 (AC-12, 2.2, 6.10): each null argument throws with its own ParamName, checked in the order resolved, catalog, itemKinds.
    [Fact]
    public void EachNullArgumentThrowsWithItsParamName()
    {
        CategoryCatalog catalog = Catalog();
        var kinds = Kinds();
        AutoAssignResult resolved = Resolve(M(), PersistedM());

        ArgumentNullException nullResolved = Assert.Throws<ArgumentNullException>(() => AutoAssigner.AssignMixedChests(null!, catalog, kinds));
        Assert.Equal("resolved", nullResolved.ParamName);

        ArgumentNullException nullCatalog = Assert.Throws<ArgumentNullException>(() => AutoAssigner.AssignMixedChests(resolved, null!, kinds));
        Assert.Equal("catalog", nullCatalog.ParamName);

        ArgumentNullException nullKinds = Assert.Throws<ArgumentNullException>(() => AutoAssigner.AssignMixedChests(resolved, catalog, null!));
        Assert.Equal("itemKinds", nullKinds.ParamName);
    }

    // GW-U-2460 (AC-12, 3.1, organize-plan 5.2): the 8-parameter planner checks containers first and never validates the flag.
    [Fact]
    public void ThePlannerOverloadChecksContainersFirst()
    {
        OrganizeFixtures.PlanInput input = OrganizeFixtures.E();

        ArgumentNullException failure = Assert.Throws<ArgumentNullException>(() =>
            OrganizePlanner.Plan(
                null!,
                input.Assignments,
                input.InUseBy,
                input.Catalog,
                input.Kinds,
                input.Options,
                input.Threshold,
                true));
        Assert.Equal("containers", failure.ParamName);
    }

    // GW-U-2461 (AC-12, 2.2, 6.10): an empty result, an over-full chest, an undefined kind and an unknown persisted name do not throw.
    [Fact]
    public void EdgeInputsDoNotThrow()
    {
        CategoryCatalog empty = CategoryCatalog.Empty;
        var emptyKinds = new Dictionary<string, ItemKind>(StringComparer.Ordinal);
        AutoAssignResult none = AutoAssigner.Resolve(new ContainerSnapshot[0], new Dictionary<ContainerId, CategoryAssignment>(), empty, emptyKinds, Threshold);

        AutoAssignResult emptyResult = AutoAssigner.AssignMixedChests(none, empty, emptyKinds);

        Assert.Empty(emptyResult.Chests);
        Assert.Empty(emptyResult.OverflowChests);

        ContainerSnapshot overFull = C(1001, 0, 1, S("Wood"), S("Stone"), S("CopperOre"));
        ContainerSnapshot undefinedKind = C(1002, 2, 2, S("Mystery"), S("Mystery"));
        ContainerSnapshot unknownName = C(1003, 4, 2, W);
        var kinds = Kinds();
        kinds["Mystery"] = (ItemKind)999;
        var persisted = Assign((1003, CategoryAssignment.ForCategory("Ghost")));
        CategoryCatalog catalog = Catalog();
        AutoAssignResult edge = AutoAssigner.Resolve(new[] { overFull, undefinedKind, unknownName }, persisted, catalog, kinds, Threshold);

        AutoAssignResult stepped = null!;
        Exception? failure = Record.Exception(() => stepped = AutoAssigner.AssignMixedChests(edge, catalog, kinds));

        Assert.Null(failure);
        Assert.Equal(3, stepped.Chests.Count);
    }
}
