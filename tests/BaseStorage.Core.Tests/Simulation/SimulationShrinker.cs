namespace BaseStorage.Core.Tests.Simulation;

using System;
using System.Globalization;
using Xunit.Sdk;
using static BaseStorage.Core.Tests.Simulation.ScheduleGenerator;

/// <summary>
/// The shrinking aid of the S-E005-004 simulation. When a schedule fails, it replays the same seed and world with fewer operations per
/// peer and fewer peers (the generator's caps keep the world identical) and reports the smallest configuration that still fails,
/// with the world and the event log, so the failure can be turned into a literal fixture. Not a test row.
/// </summary>
internal static class SimulationShrinker
{
    /// <summary>The smallest failing configuration of a schedule.</summary>
    /// <param name="Config">The shrunk schedule.</param>
    /// <param name="Failure">The assertion it fails.</param>
    internal sealed record Shrunk(SimulationConfig Config, XunitException Failure);

    /// <summary>Finds the smallest (operations per peer, then peers) configuration of a schedule that fails <paramref name="check"/>.</summary>
    /// <param name="seed">The corpus seed.</param>
    /// <param name="index">The schedule index.</param>
    /// <param name="peers">The peer count of the failing schedule.</param>
    /// <param name="opsPerPeer">The operations per peer of the failing schedule.</param>
    /// <param name="check">The check that failed on the full schedule; it throws an xunit assertion exception on failure.</param>
    /// <returns>The smallest failing configuration, or null when only the full schedule fails.</returns>
    internal static Shrunk? Shrink(int seed, int index, int peers, int opsPerPeer, Action<SimulationConfig> check)
    {
        for (int ops = 1; ops <= opsPerPeer; ops++)
        {
            for (int count = Math.Min(2, peers); count <= peers; count++)
            {
                if (ops == opsPerPeer && count == peers)
                {
                    continue;
                }

                SimulationConfig config = Generate(seed, index, count, ops);
                try
                {
                    check(config);
                }
                catch (XunitException failure)
                {
                    return new Shrunk(config, failure);
                }
            }
        }

        return null;
    }

    /// <summary>Formats the failure of one schedule with the seed, the index, the smallest failing configuration and its world.</summary>
    /// <param name="seed">The corpus seed.</param>
    /// <param name="full">The failing schedule.</param>
    /// <param name="failure">The assertion the full schedule failed; its message carries the event log tail.</param>
    /// <param name="shrunk">The smallest failing configuration, or null.</param>
    internal static string Report(int seed, SimulationConfig full, XunitException failure, Shrunk? shrunk)
    {
        string header = string.Format(
            CultureInfo.InvariantCulture,
            "Simulation seed {0}, schedule {1} ({2} peers, {3} ops/peer, {4} chests) failed: {5}\n",
            seed,
            full.Index,
            full.Peers,
            full.OpsPerPeer,
            full.Chests.Length,
            failure.Message);
        if (shrunk is null)
        {
            return header + "No smaller configuration fails. Full schedule:\n" + Describe(full);
        }

        return header
            + string.Format(
                CultureInfo.InvariantCulture,
                "Smallest failing configuration: {0} peers, {1} ops/peer, failing with: {2}\n",
                shrunk.Config.Peers,
                shrunk.Config.OpsPerPeer,
                shrunk.Failure.Message)
            + Describe(shrunk.Config);
    }
}
