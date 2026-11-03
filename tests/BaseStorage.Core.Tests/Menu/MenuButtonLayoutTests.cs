namespace BaseStorage.Core.Tests.Menu;

using System.Collections.Generic;
using BaseStorage.Core.Menu;
using static BaseStorage.Core.Tests.Menu.MenuFixtures;

/// <summary>GW-U-2542..2544: the contract examples of the menu layout (S-QR-002 contract 2.4).</summary>
public sealed class MenuButtonLayoutTests
{
    /// <summary>GW-U-2542: std(560) places every rectangle exactly as contract 2.4 writes it.</summary>
    [Fact]
    public void Std560PlacesRestockThirdOnRowOne()
    {
        MenuLayout layout = Std(560f);
        Assert.Equal(new[]
        {
            R(12f, 48f, 150f), R(168f, 48f, 186f), R(360f, 48f, 110f),
            R(12f, 92f, 90f), R(108f, 92f, 90f),
            R(12f, 136f, 130f), R(148f, 136f, 160f), R(314f, 136f, 120f), R(440f, 136f, 108f),
        }, layout.Buttons);
        Assert.True(layout.Search == new MenuRect(12f, 182f, 536f, 40f));
        Assert.True(layout.List == new MenuRect(12f, 228f, 536f, 460f));
        Assert.Equal(3, layout.Rows);
        Assert.True(layout.Fits);
    }

    /// <summary>GW-U-2543: std(380) wraps Restock to row two, and every rectangle matches contract 2.4.</summary>
    [Fact]
    public void Std380WrapsRestockToRowTwo()
    {
        MenuLayout layout = Std(380f);
        Assert.Equal(new[]
        {
            R(12f, 48f, 150f), R(168f, 48f, 186f),
            R(12f, 92f, 110f), R(128f, 92f, 90f), R(224f, 92f, 90f),
            R(12f, 136f, 130f), R(148f, 136f, 160f),
            R(12f, 180f, 120f), R(138f, 180f, 108f),
        }, layout.Buttons);
        Assert.True(layout.Search == new MenuRect(12f, 226f, 356f, 40f));
        Assert.True(layout.List == new MenuRect(12f, 272f, 356f, 416f));
        Assert.Equal(4, layout.Rows);
        Assert.True(layout.Fits);
    }

    /// <summary>GW-U-2544: the other contract examples: std(482), std(481), today's eight buttons, a narrow panel, no buttons, and startsRow on an empty row.</summary>
    [Fact]
    public void ArrangeMatchesEveryContractExample()
    {
        MenuLayout at482 = Std(482f);
        Assert.Equal(R(360f, 48f, 110f), at482.Buttons[2]);
        Assert.Equal(R(12f, 180f, 108f), at482.Buttons[8]);
        Assert.Equal(4, at482.Rows);
        Assert.Equal(226f, at482.Search.Top);

        MenuLayout at481 = Std(481f);
        Assert.Equal(R(12f, 92f, 110f), at481.Buttons[2]);
        Assert.Equal(4, at481.Rows);

        var today = new List<MenuButtonSpec>
        {
            new MenuButtonSpec(150f, false), new MenuButtonSpec(186f, false), new MenuButtonSpec(90f, false), new MenuButtonSpec(90f, false),
            new MenuButtonSpec(130f, true), new MenuButtonSpec(160f, false), new MenuButtonSpec(120f, false), new MenuButtonSpec(108f, false),
        };
        MenuLayout menu = MenuButtonLayout.Arrange(560f, 700f, today, 40f);
        Assert.Equal(2, menu.Rows);
        Assert.Equal(new[]
        {
            R(12f, 48f, 150f), R(168f, 48f, 186f), R(360f, 48f, 90f), R(456f, 48f, 90f),
            R(12f, 92f, 130f), R(148f, 92f, 160f), R(314f, 92f, 120f), R(440f, 92f, 108f),
        }, menu.Buttons);
        Assert.True(menu.Search == new MenuRect(12f, 138f, 536f, 40f));
        Assert.Equal(184f, menu.List.Top);

        MenuLayout narrow = MenuButtonLayout.Arrange(160f, 700f, new List<MenuButtonSpec> { new MenuButtonSpec(150f, false), new MenuButtonSpec(186f, false) }, 40f);
        Assert.Equal(new[] { R(12f, 48f, 150f), R(12f, 92f, 186f) }, narrow.Buttons);
        Assert.Equal(2, narrow.Rows);
        Assert.False(narrow.Fits);

        MenuLayout empty = MenuButtonLayout.Arrange(560f, 700f, new List<MenuButtonSpec>(), 40f);
        Assert.Empty(empty.Buttons);
        Assert.Equal(0, empty.Rows);
        Assert.Equal(50f, empty.Search.Top);
        Assert.True(empty.Fits);

        MenuLayout startsOnEmpty = MenuButtonLayout.Arrange(560f, 700f, new List<MenuButtonSpec> { new MenuButtonSpec(100f, true) }, 40f);
        Assert.Equal(R(12f, 48f, 100f), startsOnEmpty.Buttons[0]);
    }
}
