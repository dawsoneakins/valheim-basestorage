namespace BaseStorage.Core.Networking.Peers;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;

/// <summary>
/// The per-session table of connected peers and their version handshake state (contract version-rpc-contracts.md 3.4). Every
/// decision lives here: the Plugin only tracks a new connection, hands in received payload bytes, ticks the table once per frame
/// and performs the returned actions. Main thread only. Time comes from the clock alone.
/// </summary>
public sealed class PeerVersionTable
{
    /// <summary>How long the host waits for a client's payload before treating the client as having no mod (rule P4).</summary>
    public static readonly TimeSpan MissingPayloadTimeout = TimeSpan.FromSeconds(5);

    /// <summary>How long the host waits after sending the vanilla version error before disconnecting the refused client (rule P5).</summary>
    public static readonly TimeSpan DisconnectGrace = TimeSpan.FromSeconds(1);

    private readonly IClock clock;
    private readonly string localVersion;
    private readonly Dictionary<long, PeerVersionEntry> entries = new Dictionary<long, PeerVersionEntry>();
    private readonly List<long> order = new List<long>();
    private readonly List<PeerVersionOutcome> scratch = new List<PeerVersionOutcome>();
    private long nextId;
    private VersionCheckResult? hostMismatch;

    /// <summary>Creates an empty table.</summary>
    /// <param name="clock">The only time source.</param>
    /// <param name="localVersion">The version of this build, normally <see cref="ModVersion.Current"/>; well formed.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException">The local version is not well formed.</exception>
    public PeerVersionTable(IClock clock, string localVersion)
    {
        if (clock is null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        this.clock = clock;
        this.localVersion = ModVersion.Require(localVersion, nameof(localVersion));
    }

    /// <summary>Gets the version this table compares against.</summary>
    public string LocalVersion => localVersion;

    /// <summary>Gets the number of tracked peers, decided or not.</summary>
    public int Count => entries.Count;

    /// <summary>Gets the tracked peer ids in tracking order.</summary>
    public IReadOnlyList<long> PeerIds => order;

    /// <summary>Gets a value indicating whether a host mismatch awaits display on the connection-error screen.</summary>
    public bool HasHostMismatch => hostMismatch is not null;

    /// <summary>Starts tracking a new connection and returns its id. A client peer gets a payload deadline from the clock.</summary>
    /// <param name="remoteIsServer">True when the remote peer is the server (the local side is a client).</param>
    /// <returns>A fresh positive id, unique within this table.</returns>
    public long Track(bool remoteIsServer)
    {
        long id = ++nextId;
        entries[id] = new PeerVersionEntry(id, remoteIsServer, clock.Elapsed + MissingPayloadTimeout, null, null);
        order.Add(id);
        return id;
    }

    /// <summary>Stops tracking a peer, for example after it disconnected on its own.</summary>
    /// <param name="peerId">The id returned by <see cref="Track"/>.</param>
    /// <returns>True when the peer was tracked.</returns>
    public bool Forget(long peerId)
    {
        if (!entries.Remove(peerId))
        {
            return false;
        }

        order.Remove(peerId);
        return true;
    }

    /// <summary>Forgets every peer; the session ended. A pending host mismatch is kept for the connection-error screen.</summary>
    public void Clear()
    {
        entries.Clear();
        order.Clear();
    }

    /// <summary>Finds the state of a peer.</summary>
    /// <param name="peerId">The id returned by <see cref="Track"/>.</param>
    /// <returns>The entry, or null when the peer is not tracked.</returns>
    public PeerVersionEntry? Find(long peerId) => entries.TryGetValue(peerId, out PeerVersionEntry? entry) ? entry : null;

    /// <summary>
    /// Handles the payload a peer sent. For a client peer the host decides with <see cref="VersionCheck.CheckPayload(string, byte[], bool)"/>;
    /// a refusal schedules the disconnect after <see cref="DisconnectGrace"/>. For the server peer the client compares the host
    /// version with its own: a mismatch is remembered for the connection-error screen and, when <paramref name="requireSameVersion"/>
    /// is on, the client leaves. An unknown or already decided peer, or an unreadable host payload, is ignored. Never throws.
    /// </summary>
    /// <param name="peerId">The id returned by <see cref="Track"/>.</param>
    /// <param name="payload">The received bytes, possibly null or malformed.</param>
    /// <param name="requireSameVersion">The local RequireSameVersion setting.</param>
    /// <returns>The outcome.</returns>
    public PeerVersionOutcome Receive(long peerId, byte[]? payload, bool requireSameVersion)
    {
        if (!entries.TryGetValue(peerId, out PeerVersionEntry? entry) || entry.IsDecided)
        {
            return new PeerVersionOutcome(peerId, entry?.RemoteIsServer ?? false, PeerVersionAction.Ignore, null);
        }

        if (!entry.RemoteIsServer)
        {
            return Decide(entry, VersionCheck.CheckPayload(localVersion, payload, requireSameVersion));
        }

        string? hostVersion = VersionHandshake.DecodePayload(payload);
        if (hostVersion is null)
        {
            return new PeerVersionOutcome(peerId, true, PeerVersionAction.Ignore, null);
        }

        VersionCheckResult display = VersionCheck.Compare(hostVersion, localVersion, true);
        if (display.Status == VersionCheckStatus.RejectedMismatch)
        {
            hostMismatch = display;
        }

        VersionCheckResult decision = VersionCheck.Compare(hostVersion, localVersion, requireSameVersion);
        entries[peerId] = entry.WithResult(decision, null);
        PeerVersionAction action = decision.IsAccepted ? PeerVersionAction.Accept : PeerVersionAction.LeaveMismatch;
        return new PeerVersionOutcome(peerId, true, action, decision);
    }

    /// <summary>
    /// Advances the table by one frame: every undecided client peer past its deadline is decided as having no payload, and every
    /// refused client past its grace yields a disconnect and is forgotten. Server peers never expire. Allocation-free when idle.
    /// </summary>
    /// <param name="requireSameVersion">The local RequireSameVersion setting.</param>
    /// <returns>The outcomes of this frame, in tracking order; empty when nothing changed.</returns>
    public IReadOnlyList<PeerVersionOutcome> Tick(bool requireSameVersion)
    {
        if (entries.Count == 0)
        {
            return Array.Empty<PeerVersionOutcome>();
        }

        TimeSpan now = clock.Elapsed;
        for (int i = 0; i < order.Count; i++)
        {
            PeerVersionEntry entry = entries[order[i]];
            if (entry.RemoteIsServer)
            {
                continue;
            }

            if (!entry.IsDecided)
            {
                if (now >= entry.Deadline)
                {
                    scratch.Add(Decide(entry, VersionCheck.Compare(localVersion, null, requireSameVersion)));
                }
            }
            else if (entry.DisconnectAt.HasValue && now >= entry.DisconnectAt.Value)
            {
                scratch.Add(new PeerVersionOutcome(entry.PeerId, false, PeerVersionAction.Disconnect, entry.Result));
            }
        }

        if (scratch.Count == 0)
        {
            return Array.Empty<PeerVersionOutcome>();
        }

        PeerVersionOutcome[] result = scratch.ToArray();
        scratch.Clear();
        for (int i = 0; i < result.Length; i++)
        {
            if (result[i].Action == PeerVersionAction.Disconnect)
            {
                Forget(result[i].PeerId);
            }
        }

        return result;
    }

    /// <summary>Returns the remembered host mismatch for the connection-error screen and forgets it.</summary>
    /// <returns>The mismatch result, or null when none is pending.</returns>
    public VersionCheckResult? TakeHostMismatch()
    {
        VersionCheckResult? pending = hostMismatch;
        hostMismatch = null;
        return pending;
    }

    private PeerVersionOutcome Decide(PeerVersionEntry entry, VersionCheckResult result)
    {
        PeerVersionAction action;
        TimeSpan? disconnectAt = null;
        switch (result.Status)
        {
            case VersionCheckStatus.Accepted:
                action = PeerVersionAction.Accept;
                break;
            case VersionCheckStatus.RejectedMismatch:
                action = PeerVersionAction.RefuseMismatch;
                disconnectAt = clock.Elapsed + DisconnectGrace;
                break;
            default:
                action = PeerVersionAction.RefuseMissing;
                disconnectAt = clock.Elapsed + DisconnectGrace;
                break;
        }

        entries[entry.PeerId] = entry.WithResult(result, disconnectAt);
        return new PeerVersionOutcome(entry.PeerId, false, action, result);
    }
}
