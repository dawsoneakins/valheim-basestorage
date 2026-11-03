namespace BaseStorage.Core.Tests.Menu;

using System.Collections.Generic;
using BaseStorage.Core.Menu;
using static BaseStorage.Core.Tests.Menu.MenuFixtures;

/// <summary>GW-U-2545: the property over every whole panel width from 380 to 560 (S-QR-002 contract 2.4).</summary>
public sealed class MenuButtonLayoutPropertyTests
{
    /// <summary>GW-U-2545: every width fits, nothing overlaps, every button is below the header, and Restock sits at one of its two places.</summary>
    [Fact]
    public void NoButtonOverlapsAnotherOrTheSearchBoxAtAnyPanelWidth()
    {
        var failures = new List<string>();
        int widthsChecked = 0;
        bool saw380 = false;
        bool saw560 = false;
        for (int width = 380; width <= 560; width++)
        {
            widthsChecked++;
            saw380 |= width == 380;
            saw560 |= width == 560;
            MenuLayout layout = Std(width);
            if (!layout.Fits)
            {
                failures.Add(width + ": does not fit");
            }

            for (int i = 0; i < layout.Buttons.Count; i++)
            {
                if (layout.Buttons[i].Top < 48f)
                {
                    failures.Add(width + ": button " + i + " above the header");
                }

                if (layout.Buttons[i].Overlaps(layout.Search))
                {
                    failures.Add(width + ": button " + i + " overlaps Search");
                }

                for (int j = i + 1; j < layout.Buttons.Count; j++)
                {
                    if (layout.Buttons[i].Overlaps(layout.Buttons[j]))
                    {
                        failures.Add(width + ": buttons " + i + " and " + j + " overlap");
                    }
                }
            }

            MenuRect restock = layout.Buttons[2];
            bool restockAtThird = restock.X == 360f && restock.Top == 48f;
            bool restockWrapped = restock.X == 12f && restock.Top == 92f;
            if (!restockAtThird && !restockWrapped)
            {
                failures.Add(width + ": Restock at " + restock.X + "," + restock.Top);
            }
        }

        Assert.True(saw380 && saw560 && widthsChecked == 181, "The width loop did not run over 380..560 (" + widthsChecked + " widths).");
        Assert.True(failures.Count == 0, string.Join("; ", failures));
    }
}
