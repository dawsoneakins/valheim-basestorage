namespace BaseStorage.Core.Menu;

using System;
using System.Collections.Generic;

/// <summary>
/// The pure base menu layout (S-QR-002 contract section 2.2). The constants are the contract values.
/// </summary>
public static class MenuButtonLayout
{
    /// <summary>The left, right and bottom inset; matches BaseMenuPanel.Margin.</summary>
    public const float Margin = 12f;

    /// <summary>The gap between buttons on a row.</summary>
    public const float Gap = 6f;

    /// <summary>The height of every button.</summary>
    public const float ButtonHeight = 40f;

    /// <summary>The gap between rows.</summary>
    public const float RowGap = 4f;

    /// <summary>The top of the first row, below the header.</summary>
    public const float FirstRowTop = 48f;

    /// <summary>The gap from the last row's bottom to the search box top.</summary>
    public const float SearchGap = 6f;

    /// <summary>The gap from the search box bottom to the list top.</summary>
    public const float ListGap = 6f;

    /// <summary>Computes the layout of the buttons, the search box and the list.</summary>
    /// <param name="panelWidth">The panel width; must be finite and positive.</param>
    /// <param name="panelHeight">The panel height; must be finite and positive.</param>
    /// <param name="buttons">The buttons in input order; must not be null or contain null.</param>
    /// <param name="searchHeight">The search box height; must be finite and positive.</param>
    /// <returns>The layout.</returns>
    public static MenuLayout Arrange(
        float panelWidth,
        float panelHeight,
        IReadOnlyList<MenuButtonSpec> buttons,
        float searchHeight)
    {
        if (!IsFinite(panelWidth) || panelWidth <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(panelWidth), "Must be finite and above 0.");
        }

        if (!IsFinite(panelHeight) || panelHeight <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(panelHeight), "Must be finite and above 0.");
        }

        if (buttons is null)
        {
            throw new ArgumentNullException(nameof(buttons));
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] is null)
            {
                throw new ArgumentException("A button spec is null.", nameof(buttons));
            }
        }

        if (!IsFinite(searchHeight) || searchHeight <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(searchHeight), "Must be finite and above 0.");
        }

        float limit = panelWidth - Margin;
        float x = Margin;
        int row = 0;
        bool rowEmpty = true;
        bool fits = true;
        var rects = new MenuRect[buttons.Count];
        for (int i = 0; i < buttons.Count; i++)
        {
            MenuButtonSpec spec = buttons[i];
            if (!rowEmpty && (spec.StartsRow || x + spec.Width > limit))
            {
                row++;
                x = Margin;
                rowEmpty = true;
            }

            var rect = new MenuRect(x, FirstRowTop + (row * (ButtonHeight + RowGap)), spec.Width, ButtonHeight);
            rects[i] = rect;
            if (rect.Right > limit)
            {
                fits = false;
            }

            x = rect.Right + Gap;
            rowEmpty = false;
        }

        int rows = buttons.Count == 0 ? 0 : row + 1;
        float searchTop = FirstRowTop + (rows * (ButtonHeight + RowGap)) - RowGap + SearchGap;
        float inner = Math.Max(0f, panelWidth - (2f * Margin));
        var search = new MenuRect(Margin, searchTop, inner, searchHeight);
        float listTop = search.Bottom + ListGap;
        var list = new MenuRect(Margin, listTop, inner, Math.Max(0f, panelHeight - Margin - listTop));
        return new MenuLayout(rects, search, list, rows, fits && list.Height > 0f);
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
