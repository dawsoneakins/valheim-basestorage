namespace BaseStorage.Core.Tests.Snapshots;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

/// <summary>AC-2, AC-5 and AC-6: ContainerSnapshot exposure, validation, value equality and contents isolation.</summary>
public sealed class ContainerSnapshotTests
{
    private const string TypeName = "ContainerSnapshot";

    // GW-U-026
    [Fact]
    public void ExposesIdPositionSizeContentsAndFlags()
    {
        dynamic chest = Fixtures.ContainerA();
        object id = chest.Id;
        object position = chest.Position;
        object flags = chest.Flags;
        object ore = chest.Contents[0];
        object wood = chest.Contents[1];

        ValueSemantics.AssertEqual(id, Fixtures.ContainerIdOf(3141592653L, 42u));
        ValueSemantics.AssertEqual(position, Fixtures.PositionOf(14f, 31.25f, -836.5f));
        Assert.Equal(5, (int)chest.Width);
        Assert.Equal(2, (int)chest.Height);
        Assert.Equal(10, (int)chest.SlotCount);
        Assert.Equal(Fixtures.FlagsOf(1), flags);
        Assert.Equal(2, (int)chest.Contents.Count);
        ValueSemantics.AssertEqual(ore, Fixtures.Ore());
        ValueSemantics.AssertEqual(wood, Fixtures.Wood());
    }

    // GW-U-026
    [Fact]
    public void EmptyContentsGivesAnEmptyCollection()
    {
        dynamic chest = Fixtures.ChestOf(5, 2, Fixtures.StackArray(), 1);

        Assert.Equal(0, (int)chest.Contents.Count);
    }

    // GW-U-026
    [Fact]
    public void OverFullOneByOneChestWithTwoStacksIsRepresentable()
    {
        dynamic chest = Fixtures.ChestOf(1, 1, Fixtures.OreAndWood(), 1);

        Assert.Equal(1, (int)chest.SlotCount);
        Assert.Equal(2, (int)chest.Contents.Count);
    }

    // GW-U-026
    [Theory]
    [InlineData(17)]
    [InlineData(3)]
    public void FlagCombinationsOfTheDefinedBitsAreAccepted(int flags)
    {
        dynamic chest = Fixtures.ChestOf(5, 2, Fixtures.OreAndWood(), flags);

        Assert.Equal(Fixtures.FlagsOf(flags), (object)chest.Flags);
    }

    // GW-U-026
    [Fact]
    public void DefaultIdIsArgumentExceptionOnId()
    {
        object none = ContractTypes.Default(ContractTypes.Snapshot("ContainerId"));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
        {
            Fixtures.ContainerOf(none, Fixtures.ChestPosition(), 5, 2, Fixtures.OreAndWood(), 1);
        });

        Assert.Equal("id", ex.ParamName);
    }

    // GW-U-026
    [Theory]
    [InlineData(0, 2, "width")]
    [InlineData(257, 2, "width")]
    [InlineData(5, 0, "height")]
    [InlineData(5, 257, "height")]
    public void SizeOutsideOneToTwoFiftySixIsOutOfRange(int width, int height, string paramName)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Fixtures.ChestOf(width, height, Fixtures.OreAndWood(), 1);
        });

        Assert.Equal(paramName, ex.ParamName);
    }

    // GW-U-026
    [Fact]
    public void NullContentsIsArgumentNull()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
        {
            Fixtures.ChestOf(5, 2, null, 1);
        });

        Assert.Equal("contents", ex.ParamName);
    }

    // GW-U-026
    [Fact]
    public void NullElementIsArgumentExceptionOnContents()
    {
        Array withNull = Fixtures.StackArray(Fixtures.Ore(), null);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
        {
            Fixtures.ChestOf(5, 2, withNull, 1);
        });

        Assert.Equal("contents", ex.ParamName);
    }

    // GW-U-026
    [Fact]
    public void FlagBitOutsideTheDefinedSetIsOutOfRangeOnFlags()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Fixtures.ChestOf(5, 2, Fixtures.StackArray(), 64);
        });

        Assert.Equal("flags", ex.ParamName);
    }

    // GW-U-036
    [Fact]
    public void EqualContainersAreEqualWhenContentsMatchElementByElement()
    {
        object copy = Fixtures.ChestOf(5, 2, Fixtures.StackList(Fixtures.Ore(), Fixtures.Wood()), 1);

        ValueSemantics.AssertEqual(Fixtures.ContainerA(), copy);
    }

    // GW-U-036
    [Theory]
    [InlineData("id")]
    [InlineData("position")]
    [InlineData("width")]
    [InlineData("height")]
    [InlineData("flags")]
    [InlineData("order")]
    [InlineData("stackCount")]
    [InlineData("length")]
    [InlineData("empty")]
    public void AnyChangedFieldOrContentsDetailMakesTheContainerUnequal(string change)
    {
        ValueSemantics.AssertNotEqual(Fixtures.ContainerA(), VariantOf(change));
    }

    // GW-U-036
    [Fact]
    public void NullHandlingFollowsClassEquality()
    {
        ValueSemantics.AssertNullHandling(ContractTypes.Snapshot(TypeName), Fixtures.ContainerA());
    }

    // GW-U-038
    [Fact]
    public void LaterChangesToTheCallerListDoNotReachContents()
    {
        IList source = Fixtures.StackList(Fixtures.Ore());
        dynamic chest = Fixtures.ChestOf(5, 2, source, 1);

        source.Add(Fixtures.Wood());
        source[0] = Fixtures.Wood();

        Assert.Equal(1, (int)chest.Contents.Count);
        object first = chest.Contents[0];
        ValueSemantics.AssertEqual(first, Fixtures.Ore());
    }

    // GW-U-038
    [Fact]
    public void MutatingTheSourceArrayAfterConstructionHasNoEffect()
    {
        Array source = Fixtures.StackArray(Fixtures.Ore());
        dynamic chest = Fixtures.ChestOf(5, 2, source, 1);

        source.SetValue(Fixtures.Wood(), 0);

        Assert.Equal(1, (int)chest.Contents.Count);
        object first = chest.Contents[0];
        ValueSemantics.AssertEqual(first, Fixtures.Ore());
    }

    // GW-U-038
    [Fact]
    public void ContentsRejectsEveryMutationThroughIListAndKeepsItsCount()
    {
        dynamic chest = Fixtures.ContainerA();
        object contents = chest.Contents;
        Type item = ContractTypes.Snapshot("ItemStackSnapshot");
        Type list = typeof(IList<>).MakeGenericType(item);
        Type collection = typeof(ICollection<>).MakeGenericType(item);
        object stack = Fixtures.Wood();

        Assert.True(list.IsInstanceOfType(contents), "Contents must implement IList<ItemStackSnapshot>.");
        Assert.Throws<NotSupportedException>(() =>
        {
            ContractTypes.Invoke(collection.GetMethod("Add")!, contents, new object?[] { stack });
        });
        Assert.Throws<NotSupportedException>(() =>
        {
            ContractTypes.Invoke(collection.GetMethod("Remove")!, contents, new object?[] { stack });
        });
        Assert.Throws<NotSupportedException>(() =>
        {
            ContractTypes.Invoke(collection.GetMethod("Clear")!, contents, Array.Empty<object?>());
        });
        Assert.Throws<NotSupportedException>(() =>
        {
            ContractTypes.Invoke(list.GetMethod("Insert")!, contents, new object?[] { 0, stack });
        });
        Assert.Throws<NotSupportedException>(() =>
        {
            ContractTypes.Invoke(list.GetProperty("Item")!.GetSetMethod()!, contents, new object?[] { 0, stack });
        });

        Assert.Equal(2, (int)chest.Contents.Count);
    }

    // GW-U-038
    [Fact]
    public void ContentsIsAReadOnlyCollectionNotAListOrAnArray()
    {
        dynamic chest = Fixtures.ContainerA();
        object contents = chest.Contents;

        Type expected = typeof(ReadOnlyCollection<>).MakeGenericType(ContractTypes.Snapshot("ItemStackSnapshot"));
        Assert.Equal(expected, contents.GetType());
    }

    private static object VariantOf(string change) => change switch
    {
        "id" => Fixtures.ContainerOf(Fixtures.ContainerIdOf(3141592653L, 43u), Fixtures.ChestPosition(), 5, 2, Fixtures.OreAndWood(), 1),
        "position" => Fixtures.ContainerOf(Fixtures.ChestId(), Fixtures.PositionOf(14f, 32f, -836.5f), 5, 2, Fixtures.OreAndWood(), 1),
        "width" => Fixtures.ChestOf(6, 2, Fixtures.OreAndWood(), 1),
        "height" => Fixtures.ChestOf(5, 3, Fixtures.OreAndWood(), 1),
        "flags" => Fixtures.ChestOf(5, 2, Fixtures.OreAndWood(), 17),
        "order" => Fixtures.ChestOf(5, 2, Fixtures.StackArray(Fixtures.Wood(), Fixtures.Ore()), 1),
        "stackCount" => Fixtures.ChestOf(5, 2, Fixtures.StackArray(Fixtures.Ore(), Fixtures.StackOf("Wood", 18, 1, 50)), 1),
        "length" => Fixtures.ChestOf(5, 2, Fixtures.StackArray(Fixtures.Ore()), 1),
        "empty" => Fixtures.ChestOf(5, 2, Fixtures.StackArray(), 1),
        _ => throw new ArgumentOutOfRangeException(nameof(change)),
    };
}
