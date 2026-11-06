namespace BaseStorage.Core.BaseMove;

using System;
using System.Collections.Generic;

/// <summary>
/// The host's side of a move (base-move spec section 3). It runs one move at a time across the world. The write happens only after the
/// requester confirmed, both sides hold their chests, the host's copy has caught up with the requester's revisions, and a fresh plan
/// matches the previewed one. Until the write, any failure ends the move with nothing changed. Main thread only.
/// </summary>
public sealed class MoveHostCoordinator
{
    private readonly IMoveHostPort port;
    private readonly double previewSeconds;
    private readonly double holdSeconds;
    private long peer;
    private int requestId;
    private ulong fingerprint;
    private double deadline;
    private MoveSummary? summary;
    private IReadOnlyList<MoveChestRevision> heldByRequester = Array.Empty<MoveChestRevision>();

    /// <summary>Creates an idle coordinator.</summary>
    /// <param name="port">The game and network. Not null.</param>
    /// <param name="previewSeconds">How long the requester may keep a preview open.</param>
    /// <param name="holdSeconds">How long the holds and the catch-up may take.</param>
    /// <exception cref="ArgumentNullException"><paramref name="port"/> is null.</exception>
    public MoveHostCoordinator(IMoveHostPort port, double previewSeconds, double holdSeconds)
    {
        this.port = port ?? throw new ArgumentNullException(nameof(port));
        this.previewSeconds = previewSeconds;
        this.holdSeconds = holdSeconds;
    }

    /// <summary>Gets where the current move stands.</summary>
    public MoveHostState State { get; private set; }

    /// <summary>Handles a new request; a second one while a move runs is refused as busy.</summary>
    /// <param name="sender">The requester.</param>
    /// <param name="request">The request. Not null.</param>
    /// <param name="now">The current time in seconds.</param>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    public void OnRequest(long sender, MoveRequest request, double now)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (State != MoveHostState.Idle)
        {
            port.SendPreview(sender, request.RequestId, MoveRefusal.Busy, null, null);
            return;
        }

        peer = sender;
        requestId = request.RequestId;
        State = MoveHostState.Planning;
        deadline = now + previewSeconds;
        port.BeginPlan(sender, request);
    }

    /// <summary>The first plan is ready, or the bases cannot move.</summary>
    /// <param name="refusal">The refusal, or None.</param>
    /// <param name="playerName">The player a refusal names, or null.</param>
    /// <param name="planFingerprint">The plan's fingerprint when not refused.</param>
    /// <param name="plan">The plan's summary when not refused.</param>
    /// <param name="now">The current time in seconds.</param>
    public void OnPlanReady(MoveRefusal refusal, string? playerName, ulong planFingerprint, MoveSummary? plan, double now)
    {
        if (State != MoveHostState.Planning)
        {
            return;
        }

        if (refusal != MoveRefusal.None || plan is null)
        {
            var reason = refusal == MoveRefusal.None ? MoveRefusal.Failed : refusal;
            State = MoveHostState.Idle;
            port.EndMove();
            port.SendPreview(peer, requestId, reason, playerName, null);
            return;
        }

        fingerprint = planFingerprint;
        summary = plan;
        State = MoveHostState.AwaitingConfirm;
        deadline = now + previewSeconds;
        port.SendPreview(peer, requestId, MoveRefusal.None, null, plan);
    }

    /// <summary>The requester confirmed and holds its chests.</summary>
    /// <param name="sender">The sender.</param>
    /// <param name="id">The request id.</param>
    /// <param name="held">The requester's held chests with their revisions. Not null.</param>
    /// <param name="now">The current time in seconds.</param>
    /// <exception cref="ArgumentNullException"><paramref name="held"/> is null.</exception>
    public void OnConfirm(long sender, int id, IReadOnlyList<MoveChestRevision> held, double now)
    {
        if (held is null)
        {
            throw new ArgumentNullException(nameof(held));
        }

        if (sender != peer || id != requestId || State != MoveHostState.AwaitingConfirm)
        {
            return;
        }

        heldByRequester = held;
        State = MoveHostState.HoldingOwn;
        deadline = now + holdSeconds;
        port.BeginHostHolds(held);
    }

    /// <summary>The host's own holds finished.</summary>
    /// <param name="succeeded">False when any hold failed.</param>
    /// <param name="now">The current time in seconds.</param>
    public void OnHostHoldsFinished(bool succeeded, double now)
    {
        if (State != MoveHostState.HoldingOwn)
        {
            return;
        }

        if (!succeeded)
        {
            Cancel(MoveRefusal.TimedOut);
            return;
        }

        State = MoveHostState.AwaitingRevisions;
        deadline = now + holdSeconds;
        Tick(now);
    }

    /// <summary>The requester backed out before confirming, or its holds failed.</summary>
    /// <param name="sender">The sender.</param>
    /// <param name="id">The request id.</param>
    public void OnCancel(long sender, int id)
    {
        if (sender != peer || id != requestId || State == MoveHostState.Idle)
        {
            return;
        }

        State = MoveHostState.Idle;
        port.EndMove();
    }

    /// <summary>A peer left. A move it started ends with nothing written.</summary>
    /// <param name="left">The peer that left.</param>
    public void OnPeerLeft(long left)
    {
        if (left == peer && State != MoveHostState.Idle)
        {
            State = MoveHostState.Idle;
            port.EndMove();
        }
    }

    /// <summary>Advances timeouts and, once everything is ready, writes. Call it regularly while a move runs.</summary>
    /// <param name="now">The current time in seconds.</param>
    public void Tick(double now)
    {
        if (State == MoveHostState.Idle)
        {
            return;
        }

        if (State == MoveHostState.AwaitingRevisions && CaughtUp())
        {
            Write();
            return;
        }

        if (now >= deadline)
        {
            Cancel(MoveRefusal.TimedOut);
        }
    }

    private bool CaughtUp()
    {
        for (var i = 0; i < heldByRequester.Count; i++)
        {
            var local = port.LocalRevision(heldByRequester[i].Id);
            if (!local.HasValue || local.Value < heldByRequester[i].Revision)
            {
                return false;
            }
        }

        return true;
    }

    private void Write()
    {
        var fresh = port.Replan();
        if (!fresh.HasValue || fresh.Value != fingerprint)
        {
            Cancel(fresh.HasValue ? MoveRefusal.Changed : MoveRefusal.Failed);
            return;
        }

        var written = port.Apply();
        if (written is null)
        {
            Cancel(MoveRefusal.Failed);
            return;
        }

        State = MoveHostState.Idle;
        port.EndMove();
        port.SendDone(peer, requestId, written, summary!);
    }

    private void Cancel(MoveRefusal refusal)
    {
        State = MoveHostState.Idle;
        port.EndMove();
        port.SendCancelled(peer, requestId, refusal, null);
    }
}
