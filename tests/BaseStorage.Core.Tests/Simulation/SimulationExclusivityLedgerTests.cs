namespace BaseStorage.Core.Tests.Simulation;

using System;
using System.Globalization;
using System.Linq;
using BaseStorage.Core.Snapshots;
using Xunit;
using Xunit.Sdk;
using static BaseStorage.Core.Tests.Simulation.ScheduleGenerator;

/// <summary>
/// GW-U-1464 and GW-U-1465 (S-E005-004, AC-3, ADR-GWIN-114 decisions 4b and 5): the after-schedule ledger catches an overlap of writes on its
/// own, with the per-write check bypassed; and the conservation check is not vacuous, because a dropped or duplicated stack, and a crashed
/// schedule, each fail it with the seed and the schedule index.
/// </summary>
public sealed class SimulationExclusivityLedgerTests
{
    private static readonly ContainerId Chest = new ContainerId(ChestUserId, 42u);
    private static readonly ContainerId OtherChest = new ContainerId(ChestUserId, 43u);

    // GW-U-1464 case 1: peers 0 and 1 both write chest #42 inside overlapping hold intervals.
    [Fact]
    public void AnOverlapOfTwoPeersWritesInsideTheirHoldsFailsNamingTheChestPeersAndTicks()
    {
        var ledger = new ExclusivityLedger();
        ledger.Hold(0, Chest, 1, 10);
        ledger.Hold(1, Chest, 5, 15);
        ledger.Write(0, Chest, 3);
        ledger.Write(1, Chest, 7);

        XunitException failure = Assert.ThrowsAny<XunitException>(() => ledger.AssertExclusive());
        Assert.Contains("#42", failure.Message, StringComparison.Ordinal);
        Assert.Contains(SimulationItems.PeerNames[0], failure.Message, StringComparison.Ordinal);
        Assert.Contains(SimulationItems.PeerNames[1], failure.Message, StringComparison.Ordinal);
        Assert.Contains("tick 3", failure.Message, StringComparison.Ordinal);
        Assert.Contains("tick 7", failure.Message, StringComparison.Ordinal);
    }

    // GW-U-1464 case 2: disjoint holds pass, and a peer that writes inside its own hold (the self-open fast path) passes too.
    [Fact]
    public void DisjointHoldsAndASelfOpenFastPathPassTheLedger()
    {
        var ledger = new ExclusivityLedger();
        ledger.Hold(0, Chest, 1, 4);
        ledger.Write(0, Chest, 2);
        ledger.Hold(1, Chest, 5, 9);
        ledger.Write(1, Chest, 6);
        ledger.Hold(0, OtherChest, 1, 12);
        ledger.Write(0, OtherChest, 2);
        ledger.Write(0, OtherChest, 11);

        ledger.AssertExclusive();
    }

    // GW-U-1464 case 3: a hold that was never released fails the ledger.
    [Fact]
    public void AHoldThatWasNeverReleasedFailsTheLedger()
    {
        var ledger = new ExclusivityLedger();
        ledger.Hold(0, Chest, 1, null);
        ledger.Write(0, Chest, 2);

        XunitException failure = Assert.ThrowsAny<XunitException>(() => ledger.AssertExclusive());
        Assert.Contains("released", failure.Message, StringComparison.Ordinal);
    }

    // GW-U-1465: a runner hook drops one stack (case 0) or duplicates one (case 1) after schedule 0; the run fails with the seed and the index.
    [Theory]
    [InlineData((int)SimulationProbe.Mutation.DropOneStack)]
    [InlineData((int)SimulationProbe.Mutation.DuplicateOneStack)]
    public void ADroppedOrDuplicatedStackFailsConservationWithTheSeedAndTheScheduleIndex(int mutation)
    {
        SimulationConfig config = Generate(0);
        XunitException failure = Assert.ThrowsAny<XunitException>(() => SimulationProbe.RunMutated(config, (SimulationProbe.Mutation)mutation));

        Assert.Contains("Conservation check failed", failure.Message, StringComparison.Ordinal);
        string report = SimulationShrinker.Report(Seed, config, failure, null);
        Assert.Contains("seed 20261028", report, StringComparison.Ordinal);
        Assert.Contains("schedule 0", report, StringComparison.Ordinal);
    }

    // GW-U-1465 case 3: a schedule that ends in a crash is still conservation-checked after a dropped stack.
    [Fact]
    public void ACrashedScheduleIsStillConservationChecked()
    {
        int crashed = Enumerable.Range(0, CorpusSize).First(index => SimulationRunner.Run(Generate(index)).Stats.Crashes > 0);
        SimulationConfig config = Generate(crashed);

        XunitException failure = Assert.ThrowsAny<XunitException>(() => SimulationProbe.RunMutated(config, SimulationProbe.Mutation.DropOneStack));
        Assert.Contains("Conservation check failed", failure.Message, StringComparison.Ordinal);
        Assert.Contains("schedule " + crashed.ToString(CultureInfo.InvariantCulture), SimulationShrinker.Report(Seed, config, failure, null), StringComparison.Ordinal);
    }
}
