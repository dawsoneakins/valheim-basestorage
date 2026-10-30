namespace BaseStorage.Core.Tests.Simulation;

using System;
using System.Collections.Generic;
using System.Globalization;
using BaseStorage.Core.Snapshots;
using Xunit.Sdk;

/// <summary>
/// The per-base hold and write ledger of the S-E005-004 simulation (ADR-GWIN-114 decision 4b): per chest, the hold intervals of each peer
/// and the ticks at which each peer wrote. <see cref="AssertExclusive"/> fails when two peers write inside overlapping holds, when a write
/// has no enclosing hold of its writer, or when a hold was never released. Not a test row.
/// </summary>
internal sealed class ExclusivityLedger
{
    private readonly List<HoldInterval> holds = new List<HoldInterval>();
    private readonly List<WriteEvent> writes = new List<WriteEvent>();

    /// <summary>Records a hold interval of a peer on a chest.</summary>
    /// <param name="peer">The peer index.</param>
    /// <param name="chest">The chest.</param>
    /// <param name="grantTick">The tick the hold was granted.</param>
    /// <param name="releaseTick">The tick the hold was released, or null when it never was.</param>
    internal void Hold(int peer, ContainerId chest, int grantTick, int? releaseTick) =>
        holds.Add(new HoldInterval(peer, chest, grantTick, releaseTick));

    /// <summary>Records a write of a peer to a chest at a tick.</summary>
    /// <param name="peer">The peer index.</param>
    /// <param name="chest">The chest.</param>
    /// <param name="tick">The tick of the write.</param>
    internal void Write(int peer, ContainerId chest, int tick) =>
        writes.Add(new WriteEvent(peer, chest, tick));

    /// <summary>Asserts that no chest has writes from two peers inside overlapping holds, and that every hold was released.</summary>
    internal void AssertExclusive()
    {
        foreach (HoldInterval hold in holds)
        {
            Assert.True(
                hold.ReleaseTick.HasValue,
                Name(hold.Chest) + ": the hold of " + PeerName(hold.Peer) + " granted at tick " + Number(hold.GrantTick) + " was never released");
        }

        for (int i = 0; i < writes.Count; i++)
        {
            WriteEvent first = writes[i];
            HoldInterval? firstHold = Enclosing(first);
            Assert.True(
                firstHold is not null,
                Name(first.Chest) + ": " + PeerName(first.Peer) + " wrote at tick " + Number(first.Tick) + " outside any hold of its own");
            for (int j = i + 1; j < writes.Count; j++)
            {
                WriteEvent second = writes[j];
                if (second.Peer == first.Peer || second.Chest != first.Chest)
                {
                    continue;
                }

                HoldInterval? secondHold = Enclosing(second);
                if (secondHold is null)
                {
                    continue;
                }

                bool overlap = firstHold!.GrantTick <= End(secondHold) && secondHold.GrantTick <= End(firstHold);
                Assert.False(
                    overlap,
                    Name(first.Chest) + ": " + PeerName(first.Peer) + " wrote at tick " + Number(first.Tick) + " inside its hold "
                    + Interval(firstHold) + " and " + PeerName(second.Peer) + " wrote at tick " + Number(second.Tick) + " inside its hold " + Interval(secondHold));
            }
        }
    }

    private static int End(HoldInterval hold) => hold.ReleaseTick ?? int.MaxValue;

    private static string Name(ContainerId chest) => "chest #" + chest.Id.ToString(CultureInfo.InvariantCulture);

    private static string PeerName(int peer) => SimulationItems.PeerNames[peer];

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Interval(HoldInterval hold) =>
        "[tick " + Number(hold.GrantTick) + ".." + (hold.ReleaseTick.HasValue ? "tick " + Number(hold.ReleaseTick.Value) : "never") + "]";

    private HoldInterval? Enclosing(WriteEvent write)
    {
        foreach (HoldInterval hold in holds)
        {
            if (hold.Peer == write.Peer && hold.Chest == write.Chest && hold.GrantTick <= write.Tick && write.Tick <= End(hold))
            {
                return hold;
            }
        }

        return null;
    }

    private sealed record HoldInterval(int Peer, ContainerId Chest, int GrantTick, int? ReleaseTick);

    private sealed record WriteEvent(int Peer, ContainerId Chest, int Tick);
}
