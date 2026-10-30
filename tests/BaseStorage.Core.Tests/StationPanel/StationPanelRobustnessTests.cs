namespace BaseStorage.Core.Tests.StationPanel;

using System;
using System.Collections.Generic;
using BaseStorage.Core.StationPanel;

/// <summary>AC-1 (SP6, SP8, ADR-GWIN-040 finite guard, ADR-GWIN-041 corpus): Compute never throws for a valid input, and SP5 holds.</summary>
public sealed class StationPanelRobustnessTests
{
    // GW-U-529 (AC-1): a seeded corpus of 2,000 inputs from new Random(20261026). For each: no exception, SP5 (or the hidden shape) holds,
    // and a second Compute returns an equal state.
    [Fact]
    public void SeededCorpusNeverThrowsAndHoldsSp5()
    {
        var random = new Random(20261026);
        for (int i = 0; i < 2000; i++)
        {
            StationPanelInput input = RandomInput(random);
            StationPanelState? first = null;
            Exception? thrown = Record.Exception(() => first = StationPanelLayout.Compute(input));
            Assert.Null(thrown);
            Sp5.AssertHolds(input, first!);
            Assert.True(first == StationPanelLayout.Compute(input), $"Corpus case {i} is not deterministic.");
        }
    }

    // GW-U-529 (AC-1, contract 5.5): fixed extremes (float.MaxValue rects, uiScale float.Epsilon, 1e-30f and 1e30f, 1x1 and int.MaxValue
    // screens, default vanilla rects), plus the out-of-range 1024x768@1.5, which gives canvas 1280 x 960 and A-right.
    [Fact]
    public void FixedExtremesNeverThrowAndHoldSp5()
    {
        var inputs = new List<StationPanelInput>
        {
            StationPanelFixtures.In(1920, 1080, 1f, new PanelRect(float.MaxValue, float.MaxValue, 0f, 0f), new PanelRect(-float.MaxValue, -float.MaxValue, float.MaxValue, float.MaxValue), new PanelRect(0f, 0f, float.MaxValue, 0f)),
            StationPanelFixtures.In(1920, 1080, 1f, new PanelRect(-float.MaxValue, -float.MaxValue, 0f, 0f), new PanelRect(float.MaxValue, float.MaxValue, 0f, 0f), new PanelRect(-float.MaxValue, -float.MaxValue, 0f, 0f)),
            StationPanelFixtures.In(1920, 1080, 1f, default, default, default),
            StationPanelFixtures.AIn(1920, 1080, float.Epsilon),
            StationPanelFixtures.AIn(1920, 1080, 1e-30f),
            StationPanelFixtures.AIn(1920, 1080, 1e30f),
            StationPanelFixtures.AIn(1, 1, 1f),
            StationPanelFixtures.AIn(int.MaxValue, 1, 1f),
            StationPanelFixtures.AIn(1, int.MaxValue, 1f),
            StationPanelFixtures.In(1920, 1080, 1f, default, default, default, 6, 41, 0, "Erik", true),
        };

        var failures = new List<string>();
        foreach (StationPanelInput input in inputs)
        {
            StationPanelState? first = null;
            Exception? thrown = Record.Exception(() => first = StationPanelLayout.Compute(input));
            if (thrown is not null)
            {
                failures.Add("threw " + thrown.GetType().Name + ": " + thrown.Message);
                continue;
            }

            Exception? sp5 = Record.Exception(() => Sp5.AssertHolds(input, first!));
            if (sp5 is not null)
            {
                failures.Add("SP5: " + sp5.Message);
            }
        }

        Assert.Empty(failures);

        StationPanelInput outOfRange = StationPanelFixtures.AIn(1024, 768, 1.5f);
        StationPanelState state = StationPanelLayout.Compute(outOfRange);
        StationPanelFixtures.AssertCanvas(state, 1280f, 960f);
        StationPanelFixtures.AssertPlaced(
            state,
            StationPanelPlacement.RightOfCraftButton,
            StationPanelFixtures.ARightOrganize,
            StationPanelFixtures.ARightDeposit,
            StationPanelFixtures.ARightStatus);
        Sp5.AssertHolds(outOfRange, state);
    }

    private static StationPanelInput RandomInput(Random random)
    {
        return new StationPanelInput(
            random.Next(1, 8001),
            random.Next(1, 8001),
            (float)(3.0 * (1.0 - random.NextDouble())),
            RandomRect(random),
            RandomRect(random),
            RandomRect(random),
            random.Next(0, 50),
            random.Next(0, 50),
            0,
            random.Next(2) == 0 ? null : "Erik",
            random.Next(4) != 0);
    }

    private static PanelRect RandomRect(Random random)
    {
        float x = (float)(random.NextDouble() * 5500.0 - 500.0);
        float y = (float)(random.NextDouble() * 5500.0 - 500.0);
        float width = (float)(random.NextDouble() * 3000.0);
        float height = (float)(random.NextDouble() * 3000.0);
        return new PanelRect(x, y, width, height);
    }
}
