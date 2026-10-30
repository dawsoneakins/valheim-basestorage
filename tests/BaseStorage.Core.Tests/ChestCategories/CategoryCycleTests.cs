namespace BaseStorage.Core.Tests.ChestCategories;

using BaseStorage.Core.Categories;
using BaseStorage.Core.ChestCategories;
using BaseStorage.Core.Persistence;
using static BaseStorage.Core.Tests.ChestCategories.ChestCategoryFixtures;

/// <summary>GW-U-1200..1203 (AC-2): the cycle order Auto, catalog categories, Keep, forward and backward with wraparound (contract 2.1, 5.1).</summary>
public sealed class CategoryCycleTests
{
    // GW-U-1200: the order lists Auto, each category in catalog order, then Keep.
    [Fact]
    public void OrderIsAutoThenCatalogOrderThenKeep()
    {
        IReadOnlyList<CategoryAssignment?> order = CategoryCycle.Order(Three());

        Assert.Equal(new CategoryAssignment?[] { null, Cat("Metals & Ores"), Cat("Wood"), Cat("Stone"), CategoryAssignment.Keep }, order);
        Assert.Throws<NotSupportedException>(() => ((IList<CategoryAssignment?>)order).Add(null));
    }

    // GW-U-1200: with 14 categories the order has 16 entries and every default name in position.
    [Fact]
    public void OrderOfTheFourteenDefaultsHasSixteenEntries()
    {
        IReadOnlyList<CategoryAssignment?> order = CategoryCycle.Order(Fourteen());

        Assert.Equal(16, order.Count);
        Assert.Null(order[0]);
        Assert.Equal(CategoryAssignment.Keep, order[15]);
        for (int i = 0; i < DefaultNames.Length; i++)
        {
            Assert.Equal(Cat(DefaultNames[i]), order[i + 1]);
        }
    }

    // GW-U-1201: forward walk over the three-category catalog, wrapping Keep to Auto.
    [Fact]
    public void NextWalksForwardAndWrapsFromKeepToAuto()
    {
        CategoryCatalog catalog = Three();

        Assert.Equal(Cat("Metals & Ores"), CategoryCycle.Next(null, catalog));
        Assert.Equal(Cat("Wood"), CategoryCycle.Next(Cat("Metals & Ores"), catalog));
        Assert.Equal(Cat("Stone"), CategoryCycle.Next(Cat("Wood"), catalog));
        Assert.Equal(CategoryAssignment.Keep, CategoryCycle.Next(Cat("Stone"), catalog));
        Assert.Null(CategoryCycle.Next(CategoryAssignment.Keep, catalog));
    }

    // GW-U-1201: backward walk, wrapping Auto to Keep.
    [Fact]
    public void PreviousWalksBackwardAndWrapsFromAutoToKeep()
    {
        CategoryCatalog catalog = Three();

        Assert.Equal(CategoryAssignment.Keep, CategoryCycle.Previous(null, catalog));
        Assert.Equal(Cat("Stone"), CategoryCycle.Previous(CategoryAssignment.Keep, catalog));
        Assert.Equal(Cat("Wood"), CategoryCycle.Previous(Cat("Stone"), catalog));
        Assert.Equal(Cat("Metals & Ores"), CategoryCycle.Previous(Cat("Wood"), catalog));
        Assert.Null(CategoryCycle.Previous(Cat("Metals & Ores"), catalog));
    }

    // GW-U-1201: a full forward or backward walk of N + 2 steps returns to the start, for 0, 1, 3 and 14 categories.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(14)]
    public void AFullWalkInEitherDirectionReturnsToTheStart(int categories)
    {
        CategoryCatalog catalog = categories switch { 0 => CategoryCatalog.Empty, 1 => One(), 3 => Three(), _ => Fourteen() };
        IReadOnlyList<CategoryAssignment?> order = CategoryCycle.Order(catalog);
        Assert.Equal(categories + 2, order.Count);

        CategoryAssignment? forward = null;
        CategoryAssignment? backward = null;
        var seenForward = new List<CategoryAssignment?>();
        var seenBackward = new List<CategoryAssignment?>();
        for (int i = 0; i < order.Count; i++)
        {
            seenForward.Add(forward);
            seenBackward.Add(backward);
            forward = CategoryCycle.Next(forward, catalog);
            backward = CategoryCycle.Previous(backward, catalog);
        }

        Assert.Null(forward);
        Assert.Null(backward);
        Assert.Equal(order, seenForward);
        Assert.Equal(order.Reverse().Skip(order.Count - 1).Concat(order.Reverse().Take(order.Count - 1)), seenBackward);
    }

    // GW-U-1202: with no categories the cycle is Auto, Keep; with one it is Auto, Wood, Keep.
    [Fact]
    public void EmptyAndSingleCatalogsCycleBetweenAutoAndKeep()
    {
        Assert.Equal(CategoryAssignment.Keep, CategoryCycle.Next(null, CategoryCatalog.Empty));
        Assert.Null(CategoryCycle.Next(CategoryAssignment.Keep, CategoryCatalog.Empty));
        Assert.Equal(CategoryAssignment.Keep, CategoryCycle.Previous(null, CategoryCatalog.Empty));
        Assert.Equal(2, CategoryCycle.Order(CategoryCatalog.Empty).Count);

        Assert.Equal(Cat("Wood"), CategoryCycle.Next(null, One()));
        Assert.Equal(CategoryAssignment.Keep, CategoryCycle.Next(Cat("Wood"), One()));
        Assert.Equal(Cat("Wood"), CategoryCycle.Previous(CategoryAssignment.Keep, One()));
    }

    // GW-U-1203: a name the catalog does not know cycles as Auto in both directions, and IndexOf says 0.
    [Fact]
    public void AnUnknownNameCyclesAsAuto()
    {
        CategoryCatalog catalog = Three();
        CategoryAssignment unknown = Cat("Gold");

        Assert.Equal(0, CategoryCycle.IndexOf(unknown, catalog));
        Assert.Equal(CategoryCycle.Next(null, catalog), CategoryCycle.Next(unknown, catalog));
        Assert.Equal(CategoryCycle.Previous(null, catalog), CategoryCycle.Previous(unknown, catalog));
        Assert.Equal(0, CategoryCycle.IndexOf(Cat("wood"), catalog));
    }

    // GW-U-1203: positions are 0 for Auto, 1..N in catalog order, N + 1 for Keep.
    [Fact]
    public void IndexOfFollowsTheOrder()
    {
        CategoryCatalog catalog = Fourteen();

        Assert.Equal(0, CategoryCycle.IndexOf(null, catalog));
        Assert.Equal(1, CategoryCycle.IndexOf(Cat("Metals & Ores"), catalog));
        Assert.Equal(14, CategoryCycle.IndexOf(Cat("Misc"), catalog));
        Assert.Equal(15, CategoryCycle.IndexOf(CategoryAssignment.Keep, catalog));
        Assert.Equal(2, CategoryCycle.IndexOf(CategoryAssignment.Keep, CategoryCatalog.Empty) + 1);
    }

    // GW-U-1203: a null catalog throws by parameter name on every member.
    [Fact]
    public void NullCatalogThrowsByParamName()
    {
        Assert.Equal("catalog", Assert.Throws<ArgumentNullException>(() => CategoryCycle.Order(null!)).ParamName);
        Assert.Equal("catalog", Assert.Throws<ArgumentNullException>(() => CategoryCycle.Next(null, null!)).ParamName);
        Assert.Equal("catalog", Assert.Throws<ArgumentNullException>(() => CategoryCycle.Previous(null, null!)).ParamName);
        Assert.Equal("catalog", Assert.Throws<ArgumentNullException>(() => CategoryCycle.IndexOf(null, null!)).ParamName);
    }
}
