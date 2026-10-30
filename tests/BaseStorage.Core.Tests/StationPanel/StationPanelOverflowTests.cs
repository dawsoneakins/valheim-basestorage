namespace BaseStorage.Core.Tests.StationPanel;

using System;
using System.Collections.Generic;
using BaseStorage.Core.StationPanel;

/// <summary>AC-1 (SP8, ADR-GWIN-040): derived candidate values that overflow float never make Compute throw.</summary>
public sealed class StationPanelOverflowTests
{
    [Fact]
    public void OverflowingDerivedValuesNeverThrowAndHoldSp5()
    {
        float h = float.MaxValue / 2;
        float m = float.MaxValue;
        var crafts = new List<PanelRect>
        {
            new PanelRect(h, 0f, h, 10f),
            new PanelRect(-m, -m, m, m),
            new PanelRect(m, m, m, m),
            new PanelRect(m, 0f, m, 0f),
            new PanelRect(-m, 0f, 0f, 0f),
            new PanelRect(0f, -m, 0f, m),
            new PanelRect(0f, m, 0f, m),
            new PanelRect(0f, 0f, m, m),
            new PanelRect(h, h, h, h),
        };

        foreach (PanelRect craft in crafts)
        {
            foreach (PanelRect other in crafts)
            {
                StationPanelInput input = StationPanelFixtures.In(1920, 1080, 1f, other, other, craft);
                StationPanelState? state = null;
                Exception? thrown = Record.Exception(() => state = StationPanelLayout.Compute(input));
                Assert.Null(thrown);
                Sp5.AssertHolds(input, state!);
            }
        }
    }
}
