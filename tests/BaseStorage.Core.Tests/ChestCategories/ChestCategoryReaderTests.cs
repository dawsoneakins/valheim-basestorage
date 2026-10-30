namespace BaseStorage.Core.Tests.ChestCategories;

using BaseStorage.Core.Categories;
using BaseStorage.Core.ChestCategories;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Ui;
using static BaseStorage.Core.Tests.ChestCategories.ChestCategoryFixtures;

/// <summary>GW-U-1204..1207 (AC-1, AC-4): the read path, unknown names kept, the stored text per value, and the labels (contract 2.2, 5.2).</summary>
public sealed class ChestCategoryReaderTests
{
    // GW-U-1204: no key, the empty string and malformed text read as Auto; k and c: read as the codec says.
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData(" ", null)]
    [InlineData("Wood", null)]
    [InlineData("c:", null)]
    [InlineData("K", null)]
    [InlineData("k", "k")]
    [InlineData("c:Wood", "Wood")]
    [InlineData("c:Gold", "Gold")]
    public void ReadDecodesOrGivesAuto(string? raw, string? expected)
    {
        CategoryAssignment? value = ChestCategoryReader.Read(raw);

        if (expected is null)
        {
            Assert.Null(value);
        }
        else if (expected == "k")
        {
            Assert.Equal(CategoryAssignment.Keep, value);
        }
        else
        {
            Assert.Equal(Cat(expected), value);
        }
    }

    // GW-U-1205: an unknown name is returned as stored, is not known, and acts as Auto; known values pass through.
    [Fact]
    public void AnUnknownNameIsKeptButActsAsAuto()
    {
        CategoryCatalog catalog = Three();
        CategoryAssignment? stored = ChestCategoryReader.Read("c:Gold");

        Assert.Equal(Cat("Gold"), stored);
        Assert.False(ChestCategoryReader.IsKnown(stored, catalog));
        Assert.Null(ChestCategoryReader.Effective(stored, catalog));
        Assert.True(ChestCategoryReader.IsKnown(null, catalog));
        Assert.True(ChestCategoryReader.IsKnown(CategoryAssignment.Keep, catalog));
        Assert.True(ChestCategoryReader.IsKnown(Cat("Wood"), catalog));
        Assert.Equal(Cat("Wood"), ChestCategoryReader.Effective(Cat("Wood"), catalog));
        Assert.Equal(CategoryAssignment.Keep, ChestCategoryReader.Effective(CategoryAssignment.Keep, catalog));
        Assert.False(ChestCategoryReader.IsKnown(Cat("wood"), catalog));
    }

    // GW-U-1206: the stored text is the codec text, or the empty string for Auto; WouldChange compares against it.
    [Fact]
    public void StoredValueAndWouldChangeAgreeWithTheCodec()
    {
        Assert.Equal(string.Empty, ChestCategoryReader.StoredValue(null));
        Assert.Equal("k", ChestCategoryReader.StoredValue(CategoryAssignment.Keep));
        Assert.Equal("c:Metals & Ores", ChestCategoryReader.StoredValue(Cat("Metals & Ores")));

        Assert.False(ChestCategoryReader.WouldChange(null, null));
        Assert.False(ChestCategoryReader.WouldChange(null, string.Empty));
        Assert.True(ChestCategoryReader.WouldChange(null, "c:Wood"));
        Assert.True(ChestCategoryReader.WouldChange(null, "Gold"));
        Assert.False(ChestCategoryReader.WouldChange(Cat("Wood"), "c:Wood"));
        Assert.True(ChestCategoryReader.WouldChange(Cat("Wood"), "c:wood"));
        Assert.True(ChestCategoryReader.WouldChange(Cat("Wood"), null));
        Assert.False(ChestCategoryReader.WouldChange(CategoryAssignment.Keep, "k"));
        Assert.True(ChestCategoryReader.WouldChange(CategoryAssignment.Keep, string.Empty));
    }

    // GW-U-1207: labels come from the strings table for Auto and Keep and are the name otherwise.
    [Fact]
    public void LabelsComeFromTheStringsTable()
    {
        Assert.Equal("Auto", ChestCategoryReader.Label(null, StringTable.English));
        Assert.Equal("Keep", ChestCategoryReader.Label(CategoryAssignment.Keep, StringTable.English));
        Assert.Equal("Metals & Ores", ChestCategoryReader.Label(Cat("Metals & Ores"), StringTable.English));
        Assert.Equal("Keep", ChestCategoryReader.Label(Cat("Keep"), StringTable.English));
    }

    // GW-U-1207: null arguments throw by parameter name.
    [Fact]
    public void NullArgumentsThrowByParamName()
    {
        Assert.Equal("catalog", Assert.Throws<ArgumentNullException>(() => ChestCategoryReader.IsKnown(null, null!)).ParamName);
        Assert.Equal("catalog", Assert.Throws<ArgumentNullException>(() => ChestCategoryReader.Effective(null, null!)).ParamName);
        Assert.Equal("strings", Assert.Throws<ArgumentNullException>(() => ChestCategoryReader.Label(null, null!)).ParamName);
    }
}
