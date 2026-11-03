namespace BaseStorage.Core.Tests.Menu;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Menu;

/// <summary>GW-U-2541: validation order and exact exception types of the menu layout (S-QR-002 contract 2.1, R7 and R8).</summary>
public sealed class MenuLayoutArgumentTests
{
    /// <summary>GW-U-2541: each invalid MenuRect and MenuButtonSpec input throws ArgumentOutOfRangeException with the contracted ParamName.</summary>
    /// <param name="kind">The case name, for the failure message.</param>
    [Theory]
    [InlineData("rect x NaN")]
    [InlineData("rect top NaN")]
    [InlineData("rect width NaN")]
    [InlineData("rect width negative")]
    [InlineData("rect height infinite")]
    [InlineData("rect height negative")]
    [InlineData("spec width zero")]
    [InlineData("spec width NaN")]
    public void MenuRectAndSpecValidationThrowsTheContractedParamName(string kind)
    {
        string expected = kind switch
        {
            "rect x NaN" => "x",
            "rect top NaN" => "top",
            "rect width NaN" => "width",
            "rect width negative" => "width",
            "rect height infinite" => "height",
            "rect height negative" => "height",
            _ => "width",
        };

        Action act = () =>
        {
            switch (kind)
            {
                case "rect x NaN": _ = new MenuRect(float.NaN, 0f, 1f, 1f); break;
                case "rect top NaN": _ = new MenuRect(0f, float.NaN, 1f, 1f); break;
                case "rect width NaN": _ = new MenuRect(0f, 0f, float.NaN, 1f); break;
                case "rect width negative": _ = new MenuRect(0f, 0f, -1f, 1f); break;
                case "rect height infinite": _ = new MenuRect(0f, 0f, 1f, float.PositiveInfinity); break;
                case "rect height negative": _ = new MenuRect(0f, 0f, 1f, -1f); break;
                case "spec width zero": _ = new MenuButtonSpec(0f, false); break;
                default: _ = new MenuButtonSpec(float.NaN, false); break;
            }
        };
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(act);
        Assert.Equal(expected, ex.ParamName);
    }

    /// <summary>GW-U-2541: Arrange checks panelWidth, then panelHeight, then buttons (null, then a null element), then searchHeight.</summary>
    [Fact]
    public void LayoutValidationRunsInContractOrder()
    {
        IReadOnlyList<MenuButtonSpec> specs = MenuFixtures.Standard();

        ArgumentOutOfRangeException width = Assert.Throws<ArgumentOutOfRangeException>(() => { _ = MenuButtonLayout.Arrange(float.NaN, 700f, specs, 40f); });
        Assert.Equal("panelWidth", width.ParamName);
        Assert.Equal("panelWidth", Assert.Throws<ArgumentOutOfRangeException>(() => { _ = MenuButtonLayout.Arrange(0f, 700f, specs, 40f); }).ParamName);

        ArgumentOutOfRangeException height = Assert.Throws<ArgumentOutOfRangeException>(() => { _ = MenuButtonLayout.Arrange(560f, 0f, specs, 40f); });
        Assert.Equal("panelHeight", height.ParamName);

        ArgumentNullException nullButtons = Assert.Throws<ArgumentNullException>(() => { _ = MenuButtonLayout.Arrange(560f, 700f, null!, 40f); });
        Assert.Equal("buttons", nullButtons.ParamName);

        var withNull = new MenuButtonSpec?[] { new MenuButtonSpec(100f, false), null };
        ArgumentException nullElement = Assert.Throws<ArgumentException>(() => { _ = MenuButtonLayout.Arrange(560f, 700f, (IReadOnlyList<MenuButtonSpec>)withNull!, 40f); });
        Assert.Equal("buttons", nullElement.ParamName);

        ArgumentOutOfRangeException search = Assert.Throws<ArgumentOutOfRangeException>(() => { _ = MenuButtonLayout.Arrange(560f, 700f, specs, 0f); });
        Assert.Equal("searchHeight", search.ParamName);

        ArgumentOutOfRangeException widthFirst = Assert.Throws<ArgumentOutOfRangeException>(() => { _ = MenuButtonLayout.Arrange(float.NaN, float.NaN, null!, 0f); });
        Assert.Equal("panelWidth", widthFirst.ParamName);

        ArgumentNullException nullBeforeSearch = Assert.Throws<ArgumentNullException>(() => { _ = MenuButtonLayout.Arrange(560f, 700f, null!, 0f); });
        Assert.Equal("buttons", nullBeforeSearch.ParamName);

        ArgumentException elementBeforeSearch = Assert.Throws<ArgumentException>(() => { _ = MenuButtonLayout.Arrange(560f, 700f, (IReadOnlyList<MenuButtonSpec>)withNull!, float.NaN); });
        Assert.Equal("buttons", elementBeforeSearch.ParamName);
    }
}
