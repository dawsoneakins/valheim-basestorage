namespace BaseStorage.Core.Tests.Menu;

using System;
using System.Linq;
using BaseStorage.Core.Menu;
using BaseStorage.Core.Snapshots;

/// <summary>The menu's headed button groups, and the items a chest's row previews.</summary>
public sealed class MenuGroupsAndPreviewTests
{
    private static ItemStackSnapshot Stack(string prefab, int count) => new ItemStackSnapshot(prefab, count, 1, 100);

    [Fact]
    public void AHeadingSitsAboveItsButtonAndNothingOverlaps()
    {
        var specs = new[]
        {
            new MenuButtonSpec(100f, false, true),
            new MenuButtonSpec(100f, false),
            new MenuButtonSpec(100f, false, true),
        };

        MenuLayout layout = MenuButtonLayout.Arrange(560f, 700f, specs, 40f);

        Assert.Equal(2, layout.Headings.Count);
        Assert.Equal(MenuButtonLayout.FirstRowTop, layout.Headings[0].Top);
        Assert.Equal(layout.Headings[0].Bottom, layout.Buttons[0].Top);
        Assert.Equal(layout.Buttons[0].Top, layout.Buttons[1].Top);
        Assert.True(layout.Headings[1].Top >= layout.Buttons[0].Bottom);
        Assert.Equal(layout.Headings[1].Bottom, layout.Buttons[2].Top);
        Assert.Equal(2, layout.Rows);
        Assert.True(layout.Search.Top > layout.Buttons[2].Bottom);
    }

    [Fact]
    public void AGroupBelowAWrappedRowStartsItsOwnRow()
    {
        var specs = new[]
        {
            new MenuButtonSpec(300f, false, true),
            new MenuButtonSpec(300f, false),
            new MenuButtonSpec(100f, false, true),
        };

        MenuLayout layout = MenuButtonLayout.Arrange(400f, 700f, specs, 40f);

        Assert.Equal(3, layout.Rows);
        Assert.True(layout.Buttons[1].Top > layout.Buttons[0].Top);
        Assert.Equal(MenuButtonLayout.Margin, layout.Buttons[2].X);
    }

    [Fact]
    public void WithoutHeadingsTheLayoutIsUnchanged()
    {
        MenuLayout layout = MenuButtonLayout.Arrange(560f, 700f, new[] { new MenuButtonSpec(100f, false) }, 40f);

        Assert.Empty(layout.Headings);
        Assert.Equal(MenuButtonLayout.FirstRowTop, layout.Buttons[0].Top);
    }

    [Fact]
    public void ThePreviewShowsTheMostPlentifulItemsCountingEveryStack()
    {
        var contents = new[] { Stack("Wood", 50), Stack("Stone", 30), Stack("Stone", 30), Stack("Flint", 5) };

        ChestPreview preview = ChestPreview.Of(contents, 2);

        Assert.Equal(new[] { "Stone", "Wood" }, preview.Prefabs);
        Assert.Equal(1, preview.Hidden);
    }

    [Fact]
    public void EqualAmountsStayInNameOrderSoTheRowDoesNotFlicker()
    {
        ChestPreview preview = ChestPreview.Of(new[] { Stack("Wood", 5), Stack("Flint", 5) }, 5);

        Assert.Equal(new[] { "Flint", "Wood" }, preview.Prefabs);
        Assert.Equal(0, preview.Hidden);
    }

    [Fact]
    public void AnEmptyChestHasNothingToShow()
    {
        ChestPreview preview = ChestPreview.Of(Array.Empty<ItemStackSnapshot>(), 5);

        Assert.Empty(preview.Prefabs);
        Assert.Equal(0, preview.Hidden);
    }

    [Fact]
    public void WithNoRoomEveryKindCountsAsHidden()
    {
        ChestPreview preview = ChestPreview.Of(new[] { Stack("Wood", 5), Stack("Flint", 5) }, 0);

        Assert.Empty(preview.Prefabs);
        Assert.Equal(2, preview.Hidden);
    }

    [Fact]
    public void BadArgumentsAreRefused()
    {
        Assert.Throws<ArgumentNullException>(() => ChestPreview.Of(null!, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChestPreview.Of(Array.Empty<ItemStackSnapshot>(), -1));
    }
}
