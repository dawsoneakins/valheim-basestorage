namespace BaseStorage.Core.Tests.StationPanel;

using System;
using System.Collections.Generic;
using BaseStorage.Core.StationPanel;

/// <summary>AC-4 (NFR, SP1, U2, contract 5.4 determinism example): equal inputs give equal states under any culture and on any thread.</summary>
public sealed class StationPanelDeterminismTests
{
    private static IReadOnlyList<Func<StationPanelInput>> Makers()
    {
        return new Func<StationPanelInput>[]
        {
            () => StationPanelFixtures.AIn(1920, 1080, 1f),
            () => StationPanelFixtures.AIn(1920, 1080, 1f, holder: "Erik"),
            () => StationPanelFixtures.AIn(1920, 1080, 1f, stations: 1, chests: 0),
            () => StationPanelFixtures.AIn(1920, 1080, 1f, stations: 6, chests: 0, holder: "Erik"),
            () => StationPanelFixtures.AIn(1920, 1080, 1f, holder: "Erik\nthe Red"),
            () => StationPanelFixtures.AIn(1920, 1080, 1f, on: false),
            () => StationPanelFixtures.AIn(1920, 1080, 1f, stations: 1234567, chests: 41),
        };
    }

    // GW-U-544 (AC-4, SP1, U2): under each culture, two separately constructed equal inputs give states that are ==, Equals and hash equally,
    // and the texts equal the invariant literals.
    [Theory]
    [InlineData("")]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    [InlineData("ar-SA")]
    public void EqualInputsGiveEqualStatesUnderEveryCulture(string cultureName)
    {
        StationPanelFixtures.WithCulture(
            cultureName,
            () =>
            {
                IReadOnlyList<Func<StationPanelInput>> makers = Makers();
                foreach (Func<StationPanelInput> make in makers)
                {
                    StationPanelState first = StationPanelLayout.Compute(make());
                    StationPanelState second = StationPanelLayout.Compute(make());
                    Assert.True(first == second, "Equal inputs must give equal states under " + cultureName + ".");
                    Assert.True(first.Equals(second));
                    Assert.Equal(first.GetHashCode(), second.GetHashCode());
                }

                Assert.Equal(
                    "Base: <color=orange>1234567</color> stations, <color=orange>41</color> chests, <color=orange>0%</color> full",
                    StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, stations: 1234567, chests: 41)).StatusLineText);
                Assert.Equal(
                    "Organize in progress by Erik the Red",
                    StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f, holder: "Erik\nthe Red")).TooltipText);
                Assert.Equal(
                    StationPanelFixtures.Status6Stations41Chests,
                    StationPanelLayout.Compute(StationPanelFixtures.AIn(1920, 1080, 1f)).StatusLineText);
            });
    }

    // GW-U-544 (AC-4, SP1): 64 parallel computations all equal the single-threaded baseline.
    [Fact]
    public void ParallelComputationsEqualTheSingleThreadedBaseline()
    {
        StationPanelInput input = StationPanelFixtures.AIn(1280, 720, 1.2f, holder: "Erik");
        StationPanelState baseline = StationPanelLayout.Compute(input);

        var results = new StationPanelState?[64];
        System.Threading.Tasks.Parallel.For(0, results.Length, index => results[index] = StationPanelLayout.Compute(input));

        for (int index = 0; index < results.Length; index++)
        {
            Assert.True(results[index] == baseline, "Parallel result " + index + " differs from the baseline.");
        }
    }

    // GW-U-544 (AC-4, SP1): 1,000 repeated calls on one thread give equal states.
    [Fact]
    public void RepeatedCallsGiveEqualStates()
    {
        StationPanelInput input = StationPanelFixtures.AIn(3840, 2160, 0.8f, stations: 6, chests: 0, holder: "Erik");
        StationPanelState baseline = StationPanelLayout.Compute(input);
        for (int i = 0; i < 1000; i++)
        {
            Assert.True(StationPanelLayout.Compute(input) == baseline, "Repeated call " + i + " differs.");
        }
    }
}
