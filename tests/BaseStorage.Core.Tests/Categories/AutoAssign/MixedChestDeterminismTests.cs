namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System.Collections.Generic;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Categories.AutoAssign.MixedChestFixtures;

/// <summary>
/// S-A-001 (AC-3; mixed-chest-category-contracts.md MC9, MC10, 6.3 and 6.10): the step is deterministic, a reordered snapshot changes
/// only the snapshot-order tie, and the step is idempotent.
/// </summary>
public sealed class MixedChestDeterminismTests
{
    // GW-U-2410 (AC-3, 6.3): 100 calls on one M and 100 calls on fresh copies of M all give the 6.2 result.
    [Fact]
    public void RepeatedCallsGiveTheSameResultOnOneBaseAndOnFreshCopies()
    {
        ChestRow[] expected =
        {
            new ChestRow(501, AutoAssignOutcome.AlreadyAssigned, CategoryAssignment.ForCategory("Wood"), "Wood"),
            Given(502, AutoAssignOutcome.Assigned, "Metals & Ores"),
            Given(503, AutoAssignOutcome.Assigned, "Wood"),
            Given(504, AutoAssignOutcome.Assigned, "Stone"),
            Unset(505, AutoAssignOutcome.Overflow),
            new ChestRow(506, AutoAssignOutcome.Keep, CategoryAssignment.Keep, null),
        };
        ContainerSnapshot[] chests = M();

        for (int call = 0; call < 100; call++)
        {
            Assert.Equal(expected, RowsOf(Mixed(chests, PersistedM())));
        }

        for (int call = 0; call < 100; call++)
        {
            Assert.Equal(expected, RowsOf(Mixed(M(), PersistedM())));
        }
    }

    // GW-U-2411 (AC-3, 6.3 M'): with 503 ahead of 502 in the snapshot, only the snapshot-order tie differs, and Chests are in M' order.
    [Fact]
    public void AReorderedSnapshotChangesOnlyTheSnapshotOrderTie()
    {
        ChestRow[] expected =
        {
            new ChestRow(501, AutoAssignOutcome.AlreadyAssigned, CategoryAssignment.ForCategory("Wood"), "Wood"),
            Given(503, AutoAssignOutcome.Assigned, "Metals & Ores"),
            Given(502, AutoAssignOutcome.Assigned, "Wood"),
            Given(504, AutoAssignOutcome.Assigned, "Stone"),
            Unset(505, AutoAssignOutcome.Overflow),
            new ChestRow(506, AutoAssignOutcome.Keep, CategoryAssignment.Keep, null),
        };

        for (int call = 0; call < 100; call++)
        {
            Assert.Equal(expected, RowsOf(Mixed(MPrime(), PersistedM())));
        }
    }

    // GW-U-2412 (AC-3, 6.10): the step applied to its own result gives an element-wise equal result, on every example base.
    [Fact]
    public void TheStepIsIdempotentOnEveryExampleBase()
    {
        AssertIdempotent(M(), PersistedM());
        AssertIdempotent(N(), PersistedN());
        AssertIdempotent(P(), PersistedP());
        AssertIdempotent(R(), PersistedR());
        AssertIdempotent(Q(), Assign());
    }

    private static void AssertIdempotent(ContainerSnapshot[] chests, Dictionary<ContainerId, CategoryAssignment> persisted)
    {
        CategoryCatalog catalog = Catalog();
        var kinds = Kinds();
        AutoAssignResult once = Mixed(chests, persisted);

        AutoAssignResult twice = AutoAssigner.AssignMixedChests(once, catalog, kinds);

        AssertSameResult(once, twice);
    }
}
