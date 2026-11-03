namespace BaseStorage.Core.Menu;

using System;

/// <summary>
/// Where the gamepad focus is in a panel. Pure, and it needs no game types. The buttons wrap left and right and step by rows up and down, the
/// search box sits between the buttons and the list, and the list clamps at both ends. On a list row, left and right change column.
/// </summary>
public sealed class MenuFocus
{
    private int buttonCount;
    private int columns = 1;
    private bool hasSearch;
    private int rowCount;
    private int rowColumns = 1;

    /// <summary>Gets the zone that has the focus.</summary>
    public MenuFocusZone Zone { get; private set; }

    /// <summary>Gets the focused button or row.</summary>
    public int Index { get; private set; }

    /// <summary>Gets the focused column of a list row.</summary>
    public int Column { get; private set; }

    /// <summary>Tells the model what the panel holds now, keeping the focus on the same place when it still exists.</summary>
    /// <param name="buttons">The number of buttons.</param>
    /// <param name="buttonColumns">The buttons in a row of buttons; at least 1.</param>
    /// <param name="search">True when the panel has a search box.</param>
    /// <param name="rows">The number of list rows.</param>
    /// <param name="rowCells">The cells in a list row; at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">A count is negative, or a column count is below 1.</exception>
    public void Set(int buttons, int buttonColumns, bool search, int rows, int rowCells)
    {
        if (buttons < 0 || rows < 0)
        {
            throw new ArgumentOutOfRangeException(buttons < 0 ? nameof(buttons) : nameof(rows), "Must not be negative.");
        }

        if (buttonColumns < 1 || rowCells < 1)
        {
            throw new ArgumentOutOfRangeException(buttonColumns < 1 ? nameof(buttonColumns) : nameof(rowCells), "Must be at least 1.");
        }

        buttonCount = buttons;
        columns = buttonColumns;
        hasSearch = search;
        rowCount = rows;
        rowColumns = rowCells;
        Settle();
    }

    /// <summary>Puts the focus back on the first button.</summary>
    public void Reset()
    {
        Zone = MenuFocusZone.Buttons;
        Index = 0;
        Column = 0;
        Settle();
    }

    /// <summary>Applies one press.</summary>
    /// <param name="input">The press.</param>
    /// <returns>What the panel should do.</returns>
    public MenuFocusAction Apply(MenuFocusInput input)
    {
        switch (input)
        {
            case MenuFocusInput.Cancel:
                return MenuFocusAction.Close;
            case MenuFocusInput.Confirm:
                return HasTarget() ? MenuFocusAction.Press : MenuFocusAction.None;
            case MenuFocusInput.Up:
                return Vertical(-1);
            case MenuFocusInput.Down:
                return Vertical(1);
            case MenuFocusInput.Left:
                return Horizontal(-1);
            case MenuFocusInput.Right:
                return Horizontal(1);
            default:
                return MenuFocusAction.None;
        }
    }

    private bool HasTarget() =>
        (Zone == MenuFocusZone.Buttons && buttonCount > 0) || (Zone == MenuFocusZone.Search && hasSearch) || (Zone == MenuFocusZone.Rows && rowCount > 0);

    private void Settle()
    {
        if (Zone == MenuFocusZone.Rows && rowCount == 0)
        {
            Zone = hasSearch ? MenuFocusZone.Search : MenuFocusZone.Buttons;
            Index = 0;
        }

        if (Zone == MenuFocusZone.Search && !hasSearch)
        {
            Zone = MenuFocusZone.Buttons;
            Index = 0;
        }

        if (Zone == MenuFocusZone.Buttons && buttonCount == 0)
        {
            Zone = hasSearch ? MenuFocusZone.Search : rowCount > 0 ? MenuFocusZone.Rows : MenuFocusZone.Buttons;
            Index = 0;
        }

        var limit = Zone == MenuFocusZone.Rows ? rowCount : Zone == MenuFocusZone.Buttons ? buttonCount : 1;
        Index = limit == 0 ? 0 : Math.Min(Index, limit - 1);
        Column = Math.Min(Column, rowColumns - 1);
    }

    private MenuFocusAction Vertical(int step)
    {
        switch (Zone)
        {
            case MenuFocusZone.Buttons:
                return VerticalInButtons(step);
            case MenuFocusZone.Search:
                return step < 0 ? ToButtons() : ToRows();
            default:
                return VerticalInRows(step);
        }
    }

    private MenuFocusAction VerticalInButtons(int step)
    {
        var next = Index + (step * columns);
        if (next >= 0 && next < buttonCount)
        {
            Index = next;
            return MenuFocusAction.Moved;
        }

        // A short last row still counts: down from above it lands on its last button.
        var lastRowStart = buttonCount == 0 ? 0 : (buttonCount - 1) / columns * columns;
        if (step > 0 && Index < lastRowStart)
        {
            Index = buttonCount - 1;
            return MenuFocusAction.Moved;
        }

        if (step < 0)
        {
            return MenuFocusAction.None;
        }

        return hasSearch ? ToSearch() : ToRows();
    }

    private MenuFocusAction VerticalInRows(int step)
    {
        var next = Index + step;
        if (next >= 0 && next < rowCount)
        {
            Index = next;
            return MenuFocusAction.Moved;
        }

        if (step > 0)
        {
            return MenuFocusAction.None;
        }

        return hasSearch ? ToSearch() : ToButtons();
    }

    private MenuFocusAction Horizontal(int step)
    {
        if (Zone == MenuFocusZone.Rows)
        {
            var next = Column + step;
            if (next < 0 || next >= rowColumns)
            {
                return MenuFocusAction.None;
            }

            Column = next;
            return MenuFocusAction.Moved;
        }

        if (Zone != MenuFocusZone.Buttons || buttonCount < 2)
        {
            return MenuFocusAction.None;
        }

        Index = (Index + step + buttonCount) % buttonCount;
        return MenuFocusAction.Moved;
    }

    private MenuFocusAction ToButtons()
    {
        if (buttonCount == 0)
        {
            return MenuFocusAction.None;
        }

        Zone = MenuFocusZone.Buttons;
        Index = buttonCount - 1;
        return MenuFocusAction.Moved;
    }

    private MenuFocusAction ToSearch()
    {
        Zone = MenuFocusZone.Search;
        Index = 0;
        return MenuFocusAction.Moved;
    }

    private MenuFocusAction ToRows()
    {
        if (rowCount == 0)
        {
            return MenuFocusAction.None;
        }

        Zone = MenuFocusZone.Rows;
        Index = 0;
        return MenuFocusAction.Moved;
    }
}
