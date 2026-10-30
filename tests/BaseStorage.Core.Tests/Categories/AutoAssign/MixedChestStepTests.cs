namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Categories.AutoAssign.MixedChestFixtures;

/// <summary>
/// S-A-001 (AC-1, AC-2, AC-4, AC-5, AC-6; mixed-chest-category-contracts.md 2.3 and 6.2 to 6.7): the mixed-chest step gives the
/// remaining mixed chests a category where a category is short, with both tie rules, both stop conditions and the untouched entries.
/// </summary>
public sealed class MixedChestStepTests
{
    // GW-U-2400 (AC-1): base M of contract 6.2, the exact result, the same instance and the category lists.
    [Fact]
    public void BaseMGivesTheContractResultWithBothTiesAndTheKeepExclusion()
    {
        ContainerSnapshot[] chests = M();

        AutoAssignResult result = Mixed(chests, PersistedM());

        Assert.Equal(
            new[]
            {
                new ChestRow(501, AutoAssignOutcome.AlreadyAssigned, CategoryAssignment.ForCategory("Wood"), "Wood"),
                Given(502, AutoAssignOutcome.Assigned, "Metals & Ores"),
                Given(503, AutoAssignOutcome.Assigned, "Wood"),
                Given(504, AutoAssignOutcome.Assigned, "Stone"),
                Unset(505, AutoAssignOutcome.Overflow),
                new ChestRow(506, AutoAssignOutcome.Keep, CategoryAssignment.Keep, null),
            },
            RowsOf(result));
        Assert.Equal(CategoryAssignment.ForCategory("Metals & Ores"), result.Find(Id(502))!.Assignment);
        Assert.Same(chests[3], result.Find(Id(504))!.Container);
        Assert.Equal(new uint[] { 501, 503 }, result.GetCategoryChests("Wood").Select(c => c.Id.Id));
        Assert.Equal(50, result.AutoAssignThresholdPct);
    }

    // GW-U-2401 (AC-1, 6.6 base R): one category takes two chests while it stays short.
    [Fact]
    public void OneCategoryTakesTwoChestsWhileItStaysShort()
    {
        AutoAssignResult result = Mixed(R(), PersistedR());

        Assert.Equal(
            new[]
            {
                new ChestRow(900, AutoAssignOutcome.AlreadyAssigned, CategoryAssignment.ForCategory("Stone"), "Stone"),
                Given(901, AutoAssignOutcome.Assigned, "Wood"),
                Given(902, AutoAssignOutcome.Assigned, "Wood"),
            },
            RowsOf(result));
        Assert.Equal(new uint[] { 901, 902 }, result.GetCategoryChests("Wood").Select(c => c.Id.Id));
    }

    // GW-U-2405 (AC-2, 6.2 Keep paragraph): Keep adds no demand, so the result is the same without chest 506.
    [Fact]
    public void KeepChestAddsNoDemandAndTheResultIsTheSameWithoutIt()
    {
        AutoAssignResult withKeep = Mixed(M(), PersistedM());
        ContainerSnapshot[] withoutKeep = M().Take(5).ToArray();

        AutoAssignResult withoutKeepResult = Mixed(withoutKeep, PersistedM());

        Assert.NotEqual("Cooked Food", withKeep.Find(Id(502))!.CategoryName);
        Assert.Equal(RowsOf(withKeep).Where(row => row.Id != 506).ToArray(), RowsOf(withoutKeepResult));
    }

    // GW-U-2406 (AC-2, 6.4 base N and MC5, MC6): demand counts stacks, capacity counts the slots of holders only.
    [Fact]
    public void DemandCountsStacksAndCapacityCountsTheSlotsOfHolders()
    {
        AutoAssignResult result = Mixed(N(), PersistedN());

        Assert.Equal(
            new[]
            {
                new ChestRow(601, AutoAssignOutcome.AlreadyAssigned, CategoryAssignment.ForCategory("Metals & Ores"), "Metals & Ores"),
                new ChestRow(602, AutoAssignOutcome.AlreadyAssigned, CategoryAssignment.ForCategory("Stone"), "Stone"),
                Given(603, AutoAssignOutcome.Assigned, "Wood"),
                Unset(604, AutoAssignOutcome.SourceOnly),
            },
            RowsOf(result));
    }

    // GW-U-2406 (AC-2): the item counts of the stacks play no part, so 603 with Count 1 or 50 gives the same result.
    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void ItemCountsOfTheDemandStacksPlayNoPart(int count)
    {
        ContainerSnapshot[] chests = N();
        chests[2] = C(603, 4, 10, S("Wood", count), S("Wood", count), Cu, St, Fl);

        AutoAssignResult result = Mixed(chests, PersistedN());

        Assert.Equal(Given(603, AutoAssignOutcome.Assigned, "Wood"), RowsOf(result)[2]);
        Assert.Equal(Unset(604, AutoAssignOutcome.SourceOnly), RowsOf(result)[3]);
    }

    // GW-U-2406 (AC-2, MC3, MC6): an Overflow chest and a Keep chest add no capacity to any category.
    [Fact]
    public void AnOverflowChestAndAKeepChestAddNoCapacity()
    {
        ContainerSnapshot[] chests = new[]
        {
            N()[0],
            N()[1],
            N()[2],
            N()[3],
            C(605, 8, 10),
            C(606, 10, 10, W, W, W, W, W, W, W, W, W, W),
        };
        var persisted = PersistedN();
        persisted[Id(606)] = CategoryAssignment.Keep;

        AutoAssignResult result = Mixed(chests, persisted);

        Assert.Equal(
            new[]
            {
                new ChestRow(601, AutoAssignOutcome.AlreadyAssigned, CategoryAssignment.ForCategory("Metals & Ores"), "Metals & Ores"),
                new ChestRow(602, AutoAssignOutcome.AlreadyAssigned, CategoryAssignment.ForCategory("Stone"), "Stone"),
                Given(603, AutoAssignOutcome.Assigned, "Wood"),
                Unset(604, AutoAssignOutcome.SourceOnly),
                Unset(605, AutoAssignOutcome.Overflow),
                new ChestRow(606, AutoAssignOutcome.Keep, CategoryAssignment.Keep, null),
            },
            RowsOf(result));
    }

    // GW-U-2415 (AC-4, 6.2 iteration 2 and 6.5 iteration 2): an equal shortfall goes to the catalog-first category.
    [Fact]
    public void AnEqualShortfallGoesToTheCatalogFirstCategory()
    {
        AutoAssignResult inM = Mixed(M(), PersistedM());
        AutoAssignResult inP = Mixed(P(), PersistedP());

        Assert.Equal("Metals & Ores", inM.Find(Id(502))!.CategoryName);
        Assert.Equal("Metals & Ores", inP.Find(Id(702))!.CategoryName);
    }

    // GW-U-2416 (AC-4, 6.2, 6.3, 6.6): an equal stack count goes to the earlier snapshot chest, not to the lower id.
    [Fact]
    public void AnEqualStackCountGoesToTheEarlierSnapshotChest()
    {
        AutoAssignResult inM = Mixed(M(), PersistedM());
        AutoAssignResult inMPrime = Mixed(MPrime(), PersistedM());
        AutoAssignResult inR = Mixed(R(), PersistedR());

        Assert.Equal("Metals & Ores", inM.Find(Id(502))!.CategoryName);
        Assert.Equal("Wood", inM.Find(Id(503))!.CategoryName);
        Assert.Equal("Metals & Ores", inMPrime.Find(Id(503))!.CategoryName);
        Assert.Equal("Wood", inMPrime.Find(Id(502))!.CategoryName);
        Assert.Equal("Wood", inR.Find(Id(901))!.CategoryName);
        Assert.Equal("Wood", inR.Find(Id(902))!.CategoryName);
    }

    // GW-U-2417 (AC-4, 6.7 base Q, A6): a catalog name beats a name the catalog does not define, and the fallback name is valid.
    [Fact]
    public void ACatalogNameBeatsANonCatalogNameAndTheFallbackNameIsAssigned()
    {
        AutoAssignResult result = Mixed(Q(), Assign());

        Assert.Equal(
            new[]
            {
                Given(801, AutoAssignOutcome.Assigned, "Misc"),
                Given(802, AutoAssignOutcome.Assigned, "Ammo"),
                Unset(803, AutoAssignOutcome.Overflow),
            },
            RowsOf(result));
        Assert.Equal(CategoryAssignment.ForCategory("Ammo"), result.Find(Id(802))!.Assignment);
    }

    // GW-U-2420 (AC-5, 6.2 iteration 4): the loop stops when no mixed chest remains, with Cooked Food and Trophies still short.
    [Fact]
    public void TheLoopStopsWhenNoMixedChestRemains()
    {
        AutoAssignResult result = Mixed(M(), PersistedM());

        Assert.DoesNotContain(result.Chests, chest => chest.Outcome == AutoAssignOutcome.SourceOnly);
        Assert.DoesNotContain(result.Chests, chest => chest.CategoryName == "Cooked Food" || chest.CategoryName == "Trophies");
        Assert.Equal(
            new uint[] { 502, 503, 504 },
            result.Chests.Where(chest => chest.Outcome == AutoAssignOutcome.Assigned).Select(chest => chest.Id.Id));
    }

    // GW-U-2421 (AC-5, 6.4 base N): the loop stops when no category is short, and chest 604 stays a source.
    [Fact]
    public void TheLoopStopsWhenNoCategoryIsShort()
    {
        AutoAssignResult result = Mixed(N(), PersistedN());

        ChestAutoAssignment chest604 = result.Find(Id(604))!;
        Assert.Equal(AutoAssignOutcome.Assigned, result.Find(Id(603))!.Outcome);
        Assert.Equal("Wood", result.Find(Id(603))!.CategoryName);
        Assert.Equal(AutoAssignOutcome.SourceOnly, chest604.Outcome);
        Assert.Null(chest604.Assignment);
        Assert.Equal(AutoAssignOutcome.AlreadyAssigned, result.Find(Id(601))!.Outcome);
        Assert.Equal(CategoryAssignment.ForCategory("Metals & Ores"), result.Find(Id(601))!.Assignment);
        Assert.Equal(AutoAssignOutcome.AlreadyAssigned, result.Find(Id(602))!.Outcome);
        Assert.Equal(CategoryAssignment.ForCategory("Stone"), result.Find(Id(602))!.Assignment);
    }

    // GW-U-2422 (AC-5, 6.5 base P): a short category with no holder is set aside, and the loop goes on to the next category.
    [Fact]
    public void AShortCategoryWithNoHolderIsSetAsideAndTheLoopContinues()
    {
        AutoAssignResult result = Mixed(P(), PersistedP());

        Assert.Equal(
            new[]
            {
                new ChestRow(701, AutoAssignOutcome.AlreadyAssigned, CategoryAssignment.ForCategory("Wood"), "Wood"),
                Given(702, AutoAssignOutcome.Assigned, "Metals & Ores"),
            },
            RowsOf(result));
    }

    // GW-U-2425 (AC-6, MC8, 6.2): Keep and manually assigned entries are untouched, and so is a threshold-Assigned entry.
    [Fact]
    public void KeepAndManualEntriesAreUntouched()
    {
        AutoAssignResult result = Mixed(M(), PersistedM());

        ChestAutoAssignment manual = result.Find(Id(501))!;
        Assert.Equal(AutoAssignOutcome.AlreadyAssigned, manual.Outcome);
        Assert.Equal(CategoryAssignment.ForCategory("Wood"), manual.Assignment);
        Assert.Equal("Wood", manual.CategoryName);

        ChestAutoAssignment keep = result.Find(Id(506))!;
        Assert.Equal(AutoAssignOutcome.Keep, keep.Outcome);
        Assert.Equal(CategoryAssignment.Keep, keep.Assignment);
        Assert.Null(keep.CategoryName);
    }

    // GW-U-2425 (AC-6, MC8): at threshold 0 every chest with items is Assigned by the threshold step, so the step changes nothing.
    [Fact]
    public void AThresholdAssignedEntryIsUnchangedByTheStep()
    {
        CategoryCatalog catalog = Catalog();
        var kinds = Kinds();
        AutoAssignResult resolved = AutoAssigner.Resolve(M(), PersistedM(), catalog, kinds, 0);
        Assert.Equal(AutoAssignOutcome.Assigned, resolved.Find(Id(502))!.Outcome);

        AutoAssignResult stepped = AutoAssigner.AssignMixedChests(resolved, catalog, kinds);

        AssertSameResult(resolved, stepped);
    }

    // GW-U-2426 (AC-6, AC-4.6 empty chests): an empty chest stays in the overflow pool.
    [Fact]
    public void AnEmptyChestStaysInTheOverflowPool()
    {
        AutoAssignResult inM = Mixed(M(), PersistedM());
        AutoAssignResult inQ = Mixed(Q(), Assign());

        Assert.Equal(AutoAssignOutcome.Overflow, inM.Find(Id(505))!.Outcome);
        Assert.Equal(new uint[] { 505 }, inM.OverflowChests.Select(c => c.Id.Id));
        Assert.Equal(1, inM.CreateOverflowPool().Count);

        Assert.Equal(AutoAssignOutcome.Overflow, inQ.Find(Id(803))!.Outcome);
        Assert.Equal(new uint[] { 803 }, inQ.OverflowChests.Select(c => c.Id.Id));
    }
}
