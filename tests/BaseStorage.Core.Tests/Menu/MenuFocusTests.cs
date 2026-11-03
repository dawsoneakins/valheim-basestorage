namespace BaseStorage.Core.Tests.Menu;

using System;
using BaseStorage.Core.Menu;

/// <summary>The gamepad focus model (S-QG-001).</summary>
public sealed class MenuFocusTests
{
    private static MenuFocus Menu(int buttons = 9, int columns = 5, bool search = true, int rows = 3, int cells = 2)
    {
        var focus = new MenuFocus();
        focus.Set(buttons, columns, search, rows, cells);
        return focus;
    }

    [Fact]
    public void ButtonsWrapLeftAndRight()
    {
        MenuFocus focus = Menu();

        Assert.Equal(MenuFocusAction.Moved, focus.Apply(MenuFocusInput.Left));
        Assert.Equal(8, focus.Index);
        Assert.Equal(MenuFocusAction.Moved, focus.Apply(MenuFocusInput.Right));
        Assert.Equal(0, focus.Index);
    }

    [Fact]
    public void DownWalksTheButtonRowsThenSearchThenTheList()
    {
        MenuFocus focus = Menu();

        focus.Apply(MenuFocusInput.Down);
        Assert.Equal(5, focus.Index);
        focus.Apply(MenuFocusInput.Down);
        Assert.Equal(MenuFocusZone.Search, focus.Zone);
        focus.Apply(MenuFocusInput.Down);
        Assert.Equal(MenuFocusZone.Rows, focus.Zone);
        Assert.Equal(0, focus.Index);
    }

    [Fact]
    public void DownFromAboveAShortLastRowLandsOnItsLastButton()
    {
        MenuFocus focus = Menu();

        for (int i = 0; i < 4; i++)
        {
            focus.Apply(MenuFocusInput.Right);
        }

        focus.Apply(MenuFocusInput.Down);

        Assert.Equal(MenuFocusZone.Buttons, focus.Zone);
        Assert.Equal(8, focus.Index);
    }

    [Fact]
    public void TheListClampsAtTheBottomAndReturnsToSearchAtTheTop()
    {
        MenuFocus focus = Menu();
        focus.Apply(MenuFocusInput.Down);
        focus.Apply(MenuFocusInput.Down);
        focus.Apply(MenuFocusInput.Down);
        focus.Apply(MenuFocusInput.Down);
        focus.Apply(MenuFocusInput.Down);

        Assert.Equal(2, focus.Index);
        Assert.Equal(MenuFocusAction.None, focus.Apply(MenuFocusInput.Down));

        focus.Apply(MenuFocusInput.Up);
        focus.Apply(MenuFocusInput.Up);
        focus.Apply(MenuFocusInput.Up);

        Assert.Equal(MenuFocusZone.Search, focus.Zone);
    }

    [Fact]
    public void LeftAndRightChangeColumnOnARowAndClamp()
    {
        MenuFocus focus = Menu();
        focus.Apply(MenuFocusInput.Down);
        focus.Apply(MenuFocusInput.Down);
        focus.Apply(MenuFocusInput.Down);

        Assert.Equal(MenuFocusAction.None, focus.Apply(MenuFocusInput.Left));
        Assert.Equal(MenuFocusAction.Moved, focus.Apply(MenuFocusInput.Right));
        Assert.Equal(1, focus.Column);
        Assert.Equal(MenuFocusAction.None, focus.Apply(MenuFocusInput.Right));
    }

    [Fact]
    public void ConfirmPressesAndCancelCloses()
    {
        MenuFocus focus = Menu();

        Assert.Equal(MenuFocusAction.Press, focus.Apply(MenuFocusInput.Confirm));
        Assert.Equal(MenuFocusAction.Close, focus.Apply(MenuFocusInput.Cancel));
    }

    [Fact]
    public void AListThatShrinksKeepsTheFocusInRange()
    {
        MenuFocus focus = Menu(rows: 10);
        for (int i = 0; i < 8; i++)
        {
            focus.Apply(MenuFocusInput.Down);
        }

        Assert.Equal(MenuFocusZone.Rows, focus.Zone);
        focus.Set(9, 5, true, 2, 2);
        Assert.Equal(1, focus.Index);

        focus.Set(9, 5, true, 0, 2);
        Assert.Equal(MenuFocusZone.Search, focus.Zone);
    }

    [Fact]
    public void AnEmptyPanelPressesNothing()
    {
        MenuFocus focus = Menu(buttons: 0, search: false, rows: 0);

        Assert.Equal(MenuFocusAction.None, focus.Apply(MenuFocusInput.Confirm));
        Assert.Equal(MenuFocusAction.None, focus.Apply(MenuFocusInput.Down));
        Assert.Equal(MenuFocusAction.None, focus.Apply(MenuFocusInput.Right));
    }

    [Fact]
    public void ThePanelGridOfTheFeedPanelStepsByRows()
    {
        MenuFocus focus = Menu(buttons: 9, columns: 3, search: false, rows: 0, cells: 1);

        focus.Apply(MenuFocusInput.Down);
        Assert.Equal(3, focus.Index);
        focus.Apply(MenuFocusInput.Up);
        Assert.Equal(0, focus.Index);
        Assert.Equal(MenuFocusAction.None, focus.Apply(MenuFocusInput.Up));
    }

    [Fact]
    public void BadCountsAreRefused()
    {
        var focus = new MenuFocus();

        Assert.Throws<ArgumentOutOfRangeException>(() => focus.Set(-1, 1, false, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => focus.Set(0, 0, false, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => focus.Set(0, 1, false, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => focus.Set(0, 1, false, 0, 0));
    }
}
