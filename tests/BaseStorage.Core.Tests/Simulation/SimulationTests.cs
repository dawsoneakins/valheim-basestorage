namespace BaseStorage.Core.Tests.Simulation;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using Xunit.Abstractions;
using Xunit.Sdk;
using static BaseStorage.Core.Tests.Simulation.ScheduleGenerator;

/// <summary>
/// GW-U-1450 to GW-U-1459 (S-E005-004, AC-5.5, AC-5.2, AC-5.4, AC-3.4, AC-4.8, principle 1): the multi-peer Core simulation. 500
/// random schedules of two or three peers interleaving crafts, Organizes and Deposits against one shared fake world, generated from
/// the fixed <see cref="ScheduleGenerator.Seed"/>; every shard logs the seed and its totals, and a failure names the seed, the
/// schedule index and the smallest failing configuration.
/// </summary>
public sealed class SimulationTests
{
    /// <summary>The shards of the corpus: each theory row runs <see cref="SchedulesPerShard"/> consecutive schedules.</summary>
    internal const int Shards = 4;

    /// <summary>The schedules per shard.</summary>
    internal const int SchedulesPerShard = CorpusSize / Shards;

    private readonly ITestOutputHelper output;

    /// <summary>Creates the tests with xunit's output sink for the seed line.</summary>
    /// <param name="output">The output sink.</param>
    public SimulationTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    // GW-U-1452, GW-U-1453, GW-U-1454, GW-U-1455, GW-U-1456
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RandomSchedulesConserveEveryItemWriteEveryChestExclusivelyAndReleaseEverything(int shard)
    {
        Assert.Equal(CorpusSize, Shards * SchedulesPerShard);
        var total = new SimulationRunner.Stats();
        SimulationProbe.RunShard(
            shard,
            config =>
            {
                SimulationRunner.RunResult run = SimulationRunner.Run(config);
                total.Add(run.Stats);
                return run;
            },
            output.WriteLine);

        // Non-vacuous: every shard exercised every kind of operation, every failure path of the handshake and every modelled event.
        Assert.True(total.CraftsCompleted > 0, "no craft completed");
        Assert.True(total.CraftsFromChests > 0, "no completed craft took from a chest");
        Assert.True(total.CraftsDenied > 0, "no craft was denied a reservation");
        Assert.True(total.CraftsTimedOut > 0, "no craft timed out");
        Assert.True(total.CraftsChanged > 0, "no craft saw a chest change between plan and grant");
        Assert.True(total.OrganizesCompleted > 0, "no Organize completed");
        Assert.True(total.DepositsCompleted > 0, "no Deposit completed");
        Assert.True(total.StoppedDenied > 0, "no executor run was refused a reservation");
        Assert.True(total.StoppedTimedOut > 0, "no executor run timed out");
        Assert.True(total.StoppedChanged > 0, "no executor run saw a chest change");
        Assert.True(total.LockRefusals > 0, "no Organize or Deposit was refused the base lock");
        Assert.True(total.LostHolds > 0, "the server never took a granted hold away");
        Assert.True(total.UiOpens > 0, "no peer opened a chest in the vanilla UI");
        Assert.True(total.SelfOpenGrants > 0, "no peer wrote to a chest it had open");
        Assert.True(total.NoResponses > 0, "every request was answered");
        Assert.True(total.Crashes > 0, "no peer disconnected mid-operation");
        Assert.True(total.Renewals > 0, "no lock was renewed");
        Assert.True(total.ItemsMoved > 0 && total.ItemsConsumed > 0 && total.Writes > 0, "nothing moved");
    }

    // GW-U-1450
    [Fact]
    public void TheSameSeedAndIndexGenerateTheSameScheduleWorldAndEventLogAndTheCapsKeepTheWorld()
    {
        foreach (int index in new[] { 0, 1, 77, 250, 499 })
        {
            SimulationConfig first = Generate(index);
            SimulationConfig second = Generate(index);
            Assert.Equal(first.Seed, second.Seed);
            Assert.Equal(first.Chests, second.Chests);
            Assert.Equal(first.Inventories, second.Inventories);
            Assert.Equal(first.Stations, second.Stations);
            Assert.Equal(Sorted(first.Assignments), Sorted(second.Assignments));
            Assert.Equal(first with { Chests = second.Chests, Inventories = second.Inventories, Stations = second.Stations, Assignments = second.Assignments }, second);

            SimulationRunner.RunResult runA = SimulationRunner.Run(first);
            SimulationRunner.RunResult runB = SimulationRunner.Run(second);
            Assert.Equal(runA.Log, runB.Log);
            Assert.Equal(runA.After, runB.After);
            Assert.Equal(runA.Consumed, runB.Consumed);

            SimulationConfig capped = Generate(index, 2, 1);
            Assert.Equal(first.Chests, capped.Chests);
            Assert.Equal(first.Stations, capped.Stations);
            Assert.Equal(first.Inventories.Take(2), capped.Inventories);
            Assert.Equal(2, capped.Peers);
            Assert.Equal(1, capped.OpsPerPeer);
            Assert.Equal(first.FullPeers, capped.FullPeers);
            Assert.Equal(first.FullOps, capped.FullOps);
        }

        Assert.NotEqual(Generate(3).Chests, Generate(4).Chests);
        Assert.NotEqual(Generate(Seed + 1, 3, MaxPeers, int.MaxValue).Chests, Generate(3).Chests);
    }

    // GW-U-1451
    [Fact]
    public void TheCorpusVariesPeersOperationsLatencyRatesChestsAssignmentsAndInventories()
    {
        var peers = new HashSet<int>();
        var ops = new HashSet<int>();
        var movesPerFrame = new HashSet<int>();
        var chestCounts = new HashSet<int>();
        var sizes = new HashSet<(int, int)>();
        var stationCounts = new HashSet<int>();
        int slowLatency = 0, fastLatency = 0, noDeny = 0, deny = 0, disconnecting = 0, keep = 0, category = 0, unassigned = 0, tombstones = 0, vehicles = 0, empty = 0, full = 0, gear = 0, emptyInventories = 0;
        for (int index = 0; index < CorpusSize; index++)
        {
            SimulationConfig config = Generate(index);
            peers.Add(config.Peers);
            ops.Add(config.OpsPerPeer);
            movesPerFrame.Add(config.MovesPerFrame);
            chestCounts.Add(config.Chests.Length);
            stationCounts.Add(config.Stations.Length);
            slowLatency += config.MaxLatencyTicks > 30 ? 1 : 0;
            fastLatency += config.MaxLatencyTicks <= 30 ? 1 : 0;
            noDeny += config.DenyPct == 0 ? 1 : 0;
            deny += config.DenyPct > 0 ? 1 : 0;
            disconnecting += config.DisconnectPerMille > 0 ? 1 : 0;
            Assert.Equal(config.Peers, config.Inventories.Length);
            Assert.Equal(config.Chests.Select(c => c.Id).Distinct().Count(), config.Chests.Length);
            foreach (ContainerSnapshot chest in config.Chests)
            {
                sizes.Add((chest.Width, chest.Height));
                tombstones += (chest.Flags & ContainerFlags.Tombstone) != 0 ? 1 : 0;
                vehicles += (chest.Flags & ContainerFlags.Vehicle) != 0 ? 1 : 0;
                empty += chest.Contents.Count == 0 ? 1 : 0;
                full += chest.Contents.Count == chest.SlotCount ? 1 : 0;
                gear += chest.Contents.Count(s => s.MaxStack == 1);
                Assert.True(chest.Contents.Count <= chest.SlotCount);
                Assert.All(chest.Contents, s => Assert.True(s.Count <= s.MaxStack && s.MaxStack == SimulationItems.SpecOf(s.PrefabName).MaxStack));
                if (config.Assignments.TryGetValue(chest.Id, out CategoryAssignment? assignment))
                {
                    keep += assignment!.IsKeep ? 1 : 0;
                    category += assignment.IsKeep ? 0 : 1;
                }
                else
                {
                    unassigned++;
                }
            }

            foreach (ContainerSnapshot inventory in config.Inventories)
            {
                emptyInventories += inventory.Contents.Count == 0 ? 1 : 0;
                Assert.True(inventory.Contents.Count <= inventory.SlotCount);
                Assert.All(config.Chests, chest => Assert.NotEqual(inventory.Id, chest.Id));
            }
        }

        Assert.Equal(new[] { 2, 3 }, peers.OrderBy(p => p));
        Assert.Equal(new[] { 2, 3, 4, 5 }, ops.OrderBy(o => o));
        Assert.Equal(new[] { 1, 2, 3, 4 }, movesPerFrame.OrderBy(m => m));
        Assert.Equal(Enumerable.Range(4, MaxChests - 3), chestCounts.OrderBy(c => c));
        Assert.Equal(Sizes.Length, sizes.Count);
        Assert.Equal(new[] { 1, 2 }, stationCounts.OrderBy(s => s));
        Assert.All(new[] { slowLatency, fastLatency, noDeny, deny, disconnecting, keep, category, unassigned, tombstones, vehicles, empty, full, gear, emptyInventories }, n => Assert.True(n > 0));
        Assert.True(disconnecting < CorpusSize, "every schedule disconnects");
    }

    // GW-U-1459
    [Fact]
    public void TheShrinkerFindsTheSmallestFailingConfigurationAndTheReportNamesSeedAndSchedule()
    {
        int target = Enumerable.Range(0, CorpusSize).First(index => Generate(index).FullPeers == 3 && Generate(index).FullOps >= 3);
        SimulationConfig full = Generate(target);
        string targetText = target.ToString(CultureInfo.InvariantCulture);

        static void FailsFromThreePeersOrTwoOps(SimulationConfig config) =>
            Assert.True(config.Peers < 3 && config.OpsPerPeer < 2, "three peers or two ops");

        SimulationShrinker.Shrunk? shrunk = SimulationShrinker.Shrink(Seed, target, full.Peers, full.OpsPerPeer, FailsFromThreePeersOrTwoOps);
        Assert.NotNull(shrunk);
        Assert.Equal(3, shrunk!.Config.Peers);
        Assert.Equal(1, shrunk.Config.OpsPerPeer);
        Assert.Equal(full.Chests, shrunk.Config.Chests);
        Assert.Contains("three peers or two ops", shrunk.Failure.Message, StringComparison.Ordinal);

        XunitException failure = Assert.Throws<TrueException>(() => FailsFromThreePeersOrTwoOps(full));
        string report = SimulationShrinker.Report(Seed, full, failure, shrunk);
        Assert.Contains("seed " + Seed.ToString(CultureInfo.InvariantCulture), report, StringComparison.Ordinal);
        Assert.Contains("schedule " + targetText + " (3 peers", report, StringComparison.Ordinal);
        Assert.Contains("Smallest failing configuration: 3 peers, 1 ops/peer", report, StringComparison.Ordinal);
        Assert.Contains("#" + full.Chests[0].Id.Id.ToString(CultureInfo.InvariantCulture) + " ", report, StringComparison.Ordinal);
        Assert.Contains("Erik", report, StringComparison.Ordinal);

        Assert.Null(SimulationShrinker.Shrink(Seed, target, full.Peers, full.OpsPerPeer, config => Assert.True(config.Peers < full.Peers || config.OpsPerPeer < full.OpsPerPeer)));
        Assert.Contains("No smaller configuration fails", SimulationShrinker.Report(Seed, full, failure, null), StringComparison.Ordinal);

        // A real failure inside a run carries the event log tail, so the report is enough to replay by hand.
        XunitException wrapped = Assert.ThrowsAny<XunitException>(() => SimulationRunner.AssertConserved(full.Inventories, Array.Empty<ContainerSnapshot>(), Array.Empty<ItemStackSnapshot>()));
        Assert.Contains("Conservation check failed", wrapped.Message, StringComparison.Ordinal);
    }

    private static (uint Id, string Value)[] Sorted(Dictionary<ContainerId, CategoryAssignment> assignments) =>
        assignments.Select(p => (p.Key.Id, p.Value.IsKeep ? "Keep" : p.Value.CategoryName!)).OrderBy(p => p.Item1).ToArray();
}
