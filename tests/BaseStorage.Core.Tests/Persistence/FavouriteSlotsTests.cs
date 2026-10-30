namespace BaseStorage.Core.Tests.Persistence;

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-2 and contract 3.3: FavouriteSlots sorting, de-duplication, copying, read-only exposure and equality.</summary>
public sealed class FavouriteSlotsTests
{
    private const string TypeName = "FavouriteSlots";

    // GW-U-073
    [Fact]
    public void InputIsSortedAndDeduplicated()
    {
        object set = Sample();

        Assert.Equal(3, PersistenceContract.Int(set, "Count"));
        Assert.Equal(
            new object?[] { PersistenceContract.SlotOf(7, 0), PersistenceContract.SlotOf(0, 1), PersistenceContract.SlotOf(2, 1) },
            PersistenceContract.SlotsOf(set));
    }

    // GW-U-073
    [Fact]
    public void EqualSetBuiltFromAListIsEqualWithEqualHashCodes()
    {
        object fromList = PersistenceContract.FavouritesFromList(
            PersistenceContract.SlotOf(0, 1),
            PersistenceContract.SlotOf(2, 1),
            PersistenceContract.SlotOf(7, 0));

        ValueSemantics.AssertEqual(Sample(), fromList);
    }

    // GW-U-073
    [Fact]
    public void SetWithoutRowSevenIsUnequal()
    {
        ValueSemantics.AssertNotEqual(Sample(), PersistenceContract.FavouritesOf((0, 1), (2, 1)));
    }

    // GW-U-073
    [Fact]
    public void ContainsReportsMembership()
    {
        object set = Sample();

        Assert.True(PersistenceContract.Contains(set, PersistenceContract.SlotOf(0, 1)));
        Assert.False(PersistenceContract.Contains(set, PersistenceContract.SlotOf(1, 0)));
    }

    // GW-U-073
    [Fact]
    public void EmptyHasNoSlotsAndEqualsAnEmptyArrayBuild()
    {
        object empty = PersistenceContract.EmptyFavourites();

        Assert.Equal(0, PersistenceContract.Int(empty, "Count"));
        ValueSemantics.AssertEqual(empty, PersistenceContract.FavouritesOf());
    }

    // GW-U-073
    [Fact]
    public void LaterChangesToTheSourceListDoNotChangeTheSet()
    {
        IList source = ContractTypes.TypedList(PersistenceContract.Resolve("InventorySlot"), new object?[] { PersistenceContract.SlotOf(0, 1) });
        object set = PersistenceContract.Make(TypeName, source);

        source.Add(PersistenceContract.SlotOf(5, 3));

        Assert.Equal(1, PersistenceContract.Int(set, "Count"));
    }

    // GW-U-073
    [Fact]
    public void SlotsIsAReadOnlyCollectionThatRejectsMutation()
    {
        object set = Sample();
        object slots = PersistenceContract.Get(set, "Slots")!;
        Type slotType = PersistenceContract.Resolve("InventorySlot");

        Assert.IsType(typeof(ReadOnlyCollection<>).MakeGenericType(slotType), slots);
        Assert.Throws<NotSupportedException>(() => { ((IList)slots).Add(PersistenceContract.SlotOf(5, 3)); });
    }

    // GW-U-073
    [Fact]
    public void NullSlotsIsAnArgumentNullException()
    {
        _ = PersistenceContract.Resolve(TypeName);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => { PersistenceContract.Make(TypeName, new object?[] { null }); });
        Assert.Equal("slots", ex.ParamName);
    }

    // GW-U-073
    [Fact]
    public void NullHandlingFollowsR5()
    {
        ValueSemantics.AssertNullHandling(PersistenceContract.Resolve(TypeName), Sample());
    }

    private static object Sample() => PersistenceContract.FavouritesOf((2, 1), (7, 0), (0, 1), (2, 1));
}
