namespace BaseStorage.Core.Tests.Simulation;

using System;
using System.Collections.Generic;
using System.Reflection;
using BaseStorage.Core.Snapshots;
using Xunit;
using Xunit.Sdk;
using static BaseStorage.Core.Tests.Simulation.ScheduleGenerator;

/// <summary>
/// GW-U-1462 and GW-U-1463 (S-E005-004, AC-2, ADR-GWIN-114 decision 6, ADR-GWIN-115 H3): any exception inside a schedule names the seed and
/// the schedule index, and the corpus seed and size are pinned. Schedules are a pure function of the seed and the index.
/// </summary>
public sealed class SimulationFailureReportTests
{
    // GW-U-1462: a test-local operation throws InvalidOperationException inside schedule 5 of shard 0.
    [Fact]
    public void AnExceptionInsideScheduleINamesTheSeedAndTheIndexInsteadOfABareException()
    {
        const int failing = 5;
        Func<SimulationConfig, SimulationRunner.RunResult> run = config =>
        {
            if (config.Index == failing)
            {
                throw new InvalidOperationException("test-local operation failed");
            }

            return Empty();
        };

        XunitException failure = Assert.ThrowsAny<XunitException>(() => SimulationProbe.RunShard(0, run, _ => { }));
        Assert.Contains("seed 20261028", failure.Message, StringComparison.Ordinal);
        Assert.Contains("schedule 5", failure.Message, StringComparison.Ordinal);
        Assert.Contains("test-local operation failed", failure.Message, StringComparison.Ordinal);
    }

    // GW-U-1463: the seed is a const 20261028, the corpus has at least 500 schedules, and each schedule depends only on (seed, index).
    [Fact]
    public void TheSeedAndCorpusSizeArePinnedAndEachScheduleDependsOnlyOnItsSeedAndIndex()
    {
        FieldInfo? seedField = typeof(ScheduleGenerator).GetField(nameof(ScheduleGenerator.Seed), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(seedField);
        Assert.True(seedField!.IsLiteral, "Seed is not a const");
        Assert.Equal(20261028, Seed);
        Assert.True(CorpusSize >= 500, "the corpus has fewer than 500 schedules");

        int[] order = { 0, 1, 77, 250, 499 };
        var forward = new Dictionary<int, SimulationConfig>();
        foreach (int index in order)
        {
            forward[index] = Generate(index);
        }

        for (int k = order.Length - 1; k >= 0; k--)
        {
            AssertSameSchedule(forward[order[k]], Generate(order[k]));
        }

        AssertSameSchedule(forward[77], Generate(Seed, 77, MaxPeers, int.MaxValue));
    }

    private static SimulationRunner.RunResult Empty() => new SimulationRunner.RunResult(
        new SimulationRunner.Stats { Schedules = 1 },
        new List<string>(),
        new List<ContainerSnapshot>(),
        new List<ContainerSnapshot>(),
        new List<ItemStackSnapshot>());

    private static void AssertSameSchedule(SimulationConfig expected, SimulationConfig actual)
    {
        Assert.Equal(expected.Index, actual.Index);
        Assert.Equal(expected.Seed, actual.Seed);
        Assert.Equal(expected.Peers, actual.Peers);
        Assert.Equal(expected.OpsPerPeer, actual.OpsPerPeer);
        Assert.Equal(expected.Chests, actual.Chests);
        Assert.Equal(expected.Inventories, actual.Inventories);
        Assert.Equal(expected.Stations, actual.Stations);
        Assert.Equal(expected.Assignments.Count, actual.Assignments.Count);
        Assert.Equal(Describe(expected), Describe(actual));
    }
}
