namespace BaseStorage.Core.Tests.Menu;

using System.Collections.Generic;
using BaseStorage.Core.Menu;

/// <summary>
/// The fixtures of the S-QR-002 menu layout (contract section 2.3 and 2.4). Not a test row.
/// </summary>
internal static class MenuFixtures
{
    /// <summary>The nine standard buttons in the contract order: Organize, Deposit, Restock, Mark, Close, Take all, Place, Highlight, Share.</summary>
    /// <returns>The nine specs.</returns>
    public static IReadOnlyList<MenuButtonSpec> Standard() => new List<MenuButtonSpec>
    {
        new MenuButtonSpec(150f, false),
        new MenuButtonSpec(186f, false),
        new MenuButtonSpec(110f, false),
        new MenuButtonSpec(90f, false),
        new MenuButtonSpec(90f, false),
        new MenuButtonSpec(130f, true),
        new MenuButtonSpec(160f, false),
        new MenuButtonSpec(120f, false),
        new MenuButtonSpec(108f, false),
    };

    /// <summary>The layout of the standard buttons at one panel width (contract 2.4 std(w)): panel height 700, search height 40.</summary>
    /// <param name="width">The panel width.</param>
    /// <returns>The layout.</returns>
    public static MenuLayout Std(float width) => MenuButtonLayout.Arrange(width, 700f, Standard(), 40f);

    /// <summary>A button rectangle written as the contract writes it: (X, Top, Width), with height 40.</summary>
    /// <param name="x">The left edge.</param>
    /// <param name="top">The top edge.</param>
    /// <param name="width">The width.</param>
    /// <returns>The rectangle.</returns>
    public static MenuRect R(float x, float top, float width) => new MenuRect(x, top, width, 40f);
}
