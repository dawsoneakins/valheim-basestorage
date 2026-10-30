namespace BaseStorage.Core.Tests.CategoryButton;

using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.Defaults;
using BaseStorage.Core.ChestCategories;
using BaseStorage.Core.Persistence;

/// <summary>
/// GW-U-1318 (S-E004-004 AC-2, contract section 2 and CB4): the Category button walks <see cref="CategoryCycle"/> over the shipped
/// catalog (the fallback of PluginCategoryCatalog). Left click steps forward and right click steps back; both wrap at their ends.
/// </summary>
public sealed class CategoryButtonCycleWalkTests
{
    // GW-U-1318 (AC-2): forward from Auto visits the 14 shipped categories in catalog order, then Keep, then wraps to Auto;
    // backward from Auto wraps to Keep, then walks the categories in reverse catalog order back to Auto.
    [Fact]
    public void TheButtonWalkOnTheShippedCatalogWrapsAtBothEnds()
    {
        CategoryCatalog catalog = DefaultCategories.Catalog;
        string[] names = catalog.CategoryNames.ToArray();

        var expectedForward = new List<CategoryAssignment?>();
        foreach (string name in names)
        {
            expectedForward.Add(CategoryAssignment.ForCategory(name));
        }

        expectedForward.Add(CategoryAssignment.Keep);
        expectedForward.Add(null);

        CategoryAssignment? value = null;
        var forward = new List<CategoryAssignment?>();
        for (int step = 0; step < expectedForward.Count; step++)
        {
            value = CategoryCycle.Next(value, catalog);
            forward.Add(value);
        }

        Assert.Equal(expectedForward.ToArray(), forward.ToArray());

        var expectedBackward = new List<CategoryAssignment?> { CategoryAssignment.Keep };
        for (int i = names.Length - 1; i >= 0; i--)
        {
            expectedBackward.Add(CategoryAssignment.ForCategory(names[i]));
        }

        expectedBackward.Add(null);

        value = null;
        var backward = new List<CategoryAssignment?>();
        for (int step = 0; step < expectedBackward.Count; step++)
        {
            value = CategoryCycle.Previous(value, catalog);
            backward.Add(value);
        }

        Assert.Equal(expectedBackward.ToArray(), backward.ToArray());
    }
}
