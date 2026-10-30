namespace BaseStorage.Core.Tests.Simulation;

using System;
using System.Globalization;
using Xunit;
using Xunit.Sdk;
using static BaseStorage.Core.Tests.Simulation.ScheduleGenerator;

/// <summary>
/// GW-U-1466 and GW-U-1468 (S-E005-004, AC-1 and AC-4, ADR-GWIN-114 decisions 7 and 10): over all 500 schedules every rare path of the
/// handshake and the executors is reached at least once, and every schedule ends within the frame guard. A schedule that never finishes fails
/// as a livelock naming its seed and index.
/// </summary>
public sealed class SimulationCorpusCoverageTests
{
    // GW-U-1466: the rare paths, summed over the whole corpus.
    [Fact]
    public void EveryRarePathIsReachedAtLeastOnceAcrossTheCorpus()
    {
        var total = new SimulationRunner.Stats();
        for (int index = 0; index < CorpusSize; index++)
        {
            total.Add(SimulationRunner.Run(Generate(index)).Stats);
        }

        Assert.True(total.CraftsChanged > 0, "no craft saw a chest change between plan and grant");
        Assert.True(total.CraftsDenied > 0, "no craft was denied a reservation");
        Assert.True(total.CraftsTimedOut > 0, "no craft timed out");
        Assert.True(total.StoppedDenied > 0, "no executor run was refused a reservation");
        Assert.True(total.StoppedTimedOut > 0, "no executor run timed out");
        Assert.True(total.StoppedChanged > 0, "no executor run saw a chest change");
        Assert.True(total.Renewals > 0, "no lock was renewed");
        Assert.True(total.LostHolds > 0, "the server never took a granted hold away");
        Assert.True(total.UiOpens > 0, "no peer opened a chest in the vanilla UI");
        Assert.True(total.SelfOpenGrants > 0, "no peer wrote to a chest it had open");
        Assert.True(total.NoResponses > 0, "every request was answered");
        Assert.True(total.Crashes > 0, "no peer disconnected mid-operation");
    }

    // GW-U-1468 case 1: every one of the 500 schedules ends within the frame guard.
    [Fact]
    public void EveryScheduleEndsWithinTheFrameGuard()
    {
        for (int index = 0; index < CorpusSize; index++)
        {
            SimulationRunner.RunResult run = SimulationRunner.Run(Generate(index));
            Assert.True(
                run.Stats.Ticks <= MaxTicksPerSchedule,
                "schedule " + index.ToString(CultureInfo.InvariantCulture) + " took " + run.Stats.Ticks.ToString(CultureInfo.InvariantCulture)
                + " frames, over the guard of " + MaxTicksPerSchedule.ToString(CultureInfo.InvariantCulture));
        }
    }

    // GW-U-1468 case 2: a test-local schedule given a frame cap it cannot meet fails as a livelock, naming the seed and the index.
    [Fact]
    public void AScheduleThatNeverFinishesFailsAsALivelockNamingSeedAndIndex()
    {
        SimulationConfig config = Generate(3);

        XunitException failure = Assert.ThrowsAny<XunitException>(() => SimulationProbe.RunWithTickCap(config, 1));
        Assert.Contains("livelock", failure.Message, StringComparison.Ordinal);
        Assert.Contains("seed 20261028", failure.Message, StringComparison.Ordinal);
        Assert.Contains("schedule 3", failure.Message, StringComparison.Ordinal);
    }
}
