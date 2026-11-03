namespace BaseStorage.Core.Tests.Restock;

using System;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;

/// <summary>S-QR-001 AC-3 (contract 3.1 and 3.2): the carried-stack value and its marks.</summary>
public sealed class CarriedStackTests
{
    // GW-U-2497
    [Fact]
    public void CarriedStackHoldsAStackAndItsMarks()
    {
        Assert.True(typeof(CarriedMarks).IsDefined(typeof(FlagsAttribute), inherit: false));
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(CarriedMarks)));
        Assert.Equal(0, (int)CarriedMarks.None);
        Assert.Equal(1, (int)CarriedMarks.Hotbar);
        Assert.Equal(2, (int)CarriedMarks.FavouriteType);
        Assert.Equal(4, (int)CarriedMarks.StarredSlot);

        var wood30 = new ItemStackSnapshot("Wood", 30, 1, 50);
        var hotbar = new CarriedStack(wood30, CarriedMarks.Hotbar);
        Assert.Same(wood30, hotbar.Stack);
        Assert.Equal(CarriedMarks.Hotbar, hotbar.Marks);

        var equal = new CarriedStack(new ItemStackSnapshot("Wood", 30, 1, 50), CarriedMarks.Hotbar);
        Assert.True(hotbar == equal);
        Assert.Equal(hotbar.GetHashCode(), equal.GetHashCode());

        var withStar = new CarriedStack(new ItemStackSnapshot("Wood", 30, 1, 50), CarriedMarks.Hotbar | CarriedMarks.StarredSlot);
        Assert.True(hotbar != withStar);

        var otherCount = new CarriedStack(new ItemStackSnapshot("Wood", 31, 1, 50), CarriedMarks.Hotbar);
        Assert.True(hotbar != otherCount);

        ArgumentNullException nullStack = Assert.Throws<ArgumentNullException>(() => new CarriedStack(null!, CarriedMarks.None));
        Assert.Equal("stack", nullStack.ParamName);

        ArgumentOutOfRangeException badMarks = Assert.Throws<ArgumentOutOfRangeException>(
            () => new CarriedStack(new ItemStackSnapshot("Wood", 1, 1, 50), (CarriedMarks)8));
        Assert.Equal("marks", badMarks.ParamName);

        var overFull = new CarriedStack(new ItemStackSnapshot("Wood", 60, 1, 50), CarriedMarks.Hotbar);
        Assert.Equal(60, overFull.Stack.Count);
    }
}
