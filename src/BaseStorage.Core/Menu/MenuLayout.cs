namespace BaseStorage.Core.Menu;

using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// The computed base menu layout (S-QR-002 contract section 2.1): one rectangle per button, the search box and the list.
/// Created only by <see cref="MenuButtonLayout.Arrange"/>.
/// </summary>
public sealed class MenuLayout
{
    private readonly ReadOnlyCollection<MenuRect> buttons;
    private readonly MenuRect search;
    private readonly MenuRect list;
    private readonly int rows;
    private readonly bool fits;
    private readonly ReadOnlyCollection<MenuRect> headings;

    internal MenuLayout(MenuRect[] buttons, MenuRect search, MenuRect list, int rows, bool fits, MenuRect[] headings)
    {
        this.headings = new ReadOnlyCollection<MenuRect>(headings);
        this.buttons = new ReadOnlyCollection<MenuRect>(buttons);
        this.search = search;
        this.list = list;
        this.rows = rows;
        this.fits = fits;
    }

    /// <summary>Gets one rectangle per input button, in the same order.</summary>
    public IReadOnlyList<MenuRect> Buttons => buttons;

    /// <summary>Gets one rectangle per group heading, in button order.</summary>
    public IReadOnlyList<MenuRect> Headings => headings;

    /// <summary>Gets the search box rectangle.</summary>
    public MenuRect Search => search;

    /// <summary>Gets the list rectangle.</summary>
    public MenuRect List => list;

    /// <summary>Gets the number of button rows used; 0 when there are no buttons.</summary>
    public int Rows => rows;

    /// <summary>Gets a value that is true when every button fits the panel width and the list has a positive height.</summary>
    public bool Fits => fits;
}
