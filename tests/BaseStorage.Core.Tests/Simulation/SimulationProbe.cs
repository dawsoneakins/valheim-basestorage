namespace BaseStorage.Core.Tests.Simulation;

using System;
using System.Diagnostics;
using System.Globalization;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using Xunit.Sdk;
using static BaseStorage.Core.Tests.Simulation.ScheduleGenerator;

/// <summary>
/// Compile-only seams for the S-E005-004 rows GW-U-1461, GW-U-1462, GW-U-1464, GW-U-1465 and GW-U-1468 (ADR-GWIN-114 decisions 2, 4, 6 and 7;
/// ADR-GWIN-115 H2 and H3). Every member throws <see cref="NotImplementedException"/> until the implement phase builds it, so the tests that
/// call them are red for the reason that the code does not exist yet. None of them changes a result of the existing runner. Not a test row.
/// </summary>
internal static class SimulationProbe
{
    /// <summary>The write the after-schedule hook applies to the world before the conservation check.</summary>
    internal enum Mutation
    {
        /// <summary>One stack is removed from the world after the schedule.</summary>
        DropOneStack = 0,

        /// <summary>One stack is added twice to the world after the schedule.</summary>
        DuplicateOneStack = 1,
    }

    /// <summary>What a disconnect of one peer left behind, read back after it rejoined.</summary>
    /// <param name="OldSessionId">The session id of the peer before the crash.</param>
    /// <param name="NewSessionId">The session id of the peer after it rejoined.</param>
    /// <param name="OldConsumption">The consumption executor before the crash.</param>
    /// <param name="NewConsumption">The consumption executor after the rejoin.</param>
    /// <param name="OldLock">The base lock before the crash.</param>
    /// <param name="NewLock">The base lock after the rejoin.</param>
    /// <param name="OldLayer">The container layer before the crash.</param>
    /// <param name="NewLayer">The container layer after the rejoin.</param>
    /// <param name="NextCraftException">The exception the next craft threw, or null.</param>
    /// <param name="CancelsByDeadPeer">The Cancel calls the dead peer issued.</param>
    /// <param name="HoldsOfOldSessionAfterCrash">The holds still recorded for the dead session once the authority reclaimed them.</param>
    /// <param name="OldLockExpiredByLease">Whether the dead session's lock expired by lease without a release.</param>
    internal sealed record DisconnectReport(
        long OldSessionId,
        long NewSessionId,
        object OldConsumption,
        object NewConsumption,
        object OldLock,
        object NewLock,
        object OldLayer,
        object NewLayer,
        Exception? NextCraftException,
        int CancelsByDeadPeer,
        int HoldsOfOldSessionAfterCrash,
        bool OldLockExpiredByLease);

    /// <summary>Crashes one peer in the middle of an operation of the given kind, lets it rejoin as a new peer, and reads back the result.</summary>
    /// <param name="config">The schedule.</param>
    /// <param name="peer">The peer to crash.</param>
    /// <param name="during">The operation the peer is in when it dies.</param>
    /// <returns>What the disconnect left behind.</returns>
    internal static DisconnectReport CrashAndRejoin(SimulationConfig config, int peer, OperationKind during) =>
        SimulationRunner.CrashAndRejoin(config, peer, during);

    /// <summary>Runs one schedule and applies a mutation to the world after it, before the conservation check.</summary>
    /// <param name="config">The schedule.</param>
    /// <param name="mutation">The mutation to apply.</param>
    /// <returns>The outcome, when the conservation check passes.</returns>
    internal static SimulationRunner.RunResult RunMutated(SimulationConfig config, Mutation mutation) =>
        SimulationRunner.Run(
            config,
            MaxTicksPerSchedule,
            world =>
            {
                if (mutation == Mutation.DropOneStack)
                {
                    world.DropOneStack();
                }
                else
                {
                    world.DuplicateOneStack();
                }
            });

    /// <summary>Drives one shard of the corpus with a caller-supplied schedule runner, as the xunit theory does.</summary>
    /// <param name="shard">The shard number.</param>
    /// <param name="run">Runs one schedule.</param>
    /// <param name="writeLine">Receives the shard's seed line.</param>
    internal static void RunShard(int shard, Func<SimulationConfig, SimulationRunner.RunResult> run, Action<string> writeLine)
    {
        var stopwatch = Stopwatch.StartNew();
        var total = new SimulationRunner.Stats();
        int peers = 0;
        int first = shard * SimulationTests.SchedulesPerShard;
        for (int index = first; index < first + SimulationTests.SchedulesPerShard; index++)
        {
            SimulationConfig config = Generate(index);
            peers += config.Peers;
            try
            {
                total.Add(run(config).Stats);
            }
            catch (Exception failure)
            {
                // Any exception inside a schedule, Core's or the harness's, is reported with the seed, the index and the smallest failing configuration.
                XunitException reported = failure as XunitException ?? new XunitException(failure.GetType().Name + ": " + failure.Message + "\n" + failure.StackTrace);
                SimulationShrinker.Shrunk? shrunk = SimulationShrinker.Shrink(Seed, index, config.Peers, config.OpsPerPeer, c => Replay(run, c));
                throw new XunitException(SimulationShrinker.Report(Seed, config, reported, shrunk));
            }
        }

        stopwatch.Stop();
        writeLine(string.Format(
            CultureInfo.InvariantCulture,
            "simulation seed={0} shard={1} schedules={2}..{3} peers={4} {5} elapsed={6} ms",
            Seed,
            shard,
            first,
            first + SimulationTests.SchedulesPerShard - 1,
            peers,
            total.Describe(),
            stopwatch.ElapsedMilliseconds));
    }

    private static void Replay(Func<SimulationConfig, SimulationRunner.RunResult> run, SimulationConfig config)
    {
        try
        {
            run(config);
        }
        catch (XunitException)
        {
            throw;
        }
        catch (Exception failure)
        {
            throw new XunitException(failure.GetType().Name + ": " + failure.Message);
        }
    }

    /// <summary>Runs one schedule with a frame cap instead of <see cref="ScheduleGenerator.MaxTicksPerSchedule"/>, so that it cannot finish in time.</summary>
    /// <param name="config">The schedule.</param>
    /// <param name="maxTicks">The frame cap.</param>
    /// <returns>The outcome, when the schedule finishes within the cap.</returns>
    internal static SimulationRunner.RunResult RunWithTickCap(SimulationConfig config, int maxTicks) =>
        SimulationRunner.Run(config, maxTicks, null);
}
