namespace BaseStorage.Core.Tests.Persistence;

using System;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-2 and contract 3.3: InventorySlot coordinates, validation, row-major ordering and equality.</summary>
public sealed class InventorySlotTests
{
    private const string TypeName = "InventorySlot";

    // GW-U-072
    [Fact]
    public void CoordinatesRoundTrip()
    {
        object slot = PersistenceContract.SlotOf(3, 2);

        Assert.Equal<object?>(3, PersistenceContract.Get(slot, "X"));
        Assert.Equal<object?>(2, PersistenceContract.Get(slot, "Y"));
    }

    // GW-U-072
    [Fact]
    public void DefaultIsTheTopLeftSlot()
    {
        object slot = ContractTypes.Default(PersistenceContract.Resolve(TypeName));

        Assert.Equal<object?>(0, PersistenceContract.Get(slot, "X"));
        Assert.Equal<object?>(0, PersistenceContract.Get(slot, "Y"));
    }

    // GW-U-072
    [Fact]
    public void MaximumCornerIsValid()
    {
        object slot = PersistenceContract.SlotOf(255, 255);

        Assert.Equal<object?>(255, PersistenceContract.Get(slot, "X"));
        Assert.Equal<object?>(255, PersistenceContract.Get(slot, "Y"));
    }

    // GW-U-072
    [Theory]
    [InlineData(-1, 0, "x")]
    [InlineData(256, 0, "x")]
    [InlineData(0, -1, "y")]
    [InlineData(0, 256, "y")]
    public void CoordinateOutsideZeroToTwoFiftyFiveThrowsWithItsParamName(int x, int y, string paramName)
    {
        _ = PersistenceContract.Resolve(TypeName);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => { PersistenceContract.SlotOf(x, y); });
        Assert.Equal(paramName, ex.ParamName);
    }

    // GW-U-072
    [Fact]
    public void CompareToIsRowMajor()
    {
        Assert.True(Compare(PersistenceContract.SlotOf(7, 0), PersistenceContract.SlotOf(0, 1)) < 0, "(7,0) sorts before (0,1): row first.");
        Assert.True(Compare(PersistenceContract.SlotOf(2, 1), PersistenceContract.SlotOf(0, 1)) > 0, "(2,1) sorts after (0,1) in the same row.");
        Assert.Equal(0, Compare(PersistenceContract.SlotOf(0, 1), PersistenceContract.SlotOf(0, 1)));
    }

    // GW-U-072
    [Fact]
    public void EqualSlotsAreEqualWithEqualHashCodes()
    {
        ValueSemantics.AssertEqual(PersistenceContract.SlotOf(3, 2), PersistenceContract.SlotOf(3, 2));
    }

    // GW-U-072
    [Fact]
    public void TransposedSlotsAreUnequal()
    {
        ValueSemantics.AssertNotEqual(PersistenceContract.SlotOf(3, 2), PersistenceContract.SlotOf(2, 3));
    }

    // GW-U-072
    [Fact]
    public void BoxedEqualsUsesValueEquality()
    {
        object left = PersistenceContract.SlotOf(3, 2);
        object right = PersistenceContract.SlotOf(3, 2);

        Assert.True(left.Equals(right));
        Assert.False(left.Equals(PersistenceContract.SlotOf(2, 3)));
    }

    private static int Compare(object left, object right) => (int)PersistenceContract.CallInstance(left, "CompareTo", right)!;
}
