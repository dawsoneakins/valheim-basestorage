namespace BaseStorage.Core.BaseMove;

using System;
using System.Collections.Generic;

/// <summary>
/// The requester's side of one move (base-move spec section 3, steps 1 and 6 to 9). The rule that matters: from the moment the confirm
/// is sent, the holds are released only when the host cancels, the connection ends, or this game has every revision the host wrote.
/// Releasing earlier could send the old chest contents back and duplicate items, so a timeout here warns and keeps waiting.
/// Main thread only.
/// </summary>
public sealed class MoveRequesterSession
{
    private readonly IMoveRequesterPort port;
    private readonly double timeoutSeconds;
    private int requestId;
    private double deadline;
    private bool warned;
    private MoveSummary? summary;
    private IReadOnlyList<MoveChestRevision> written = Array.Empty<MoveChestRevision>();

    /// <summary>Creates an idle session.</summary>
    /// <param name="port">The game and network. Not null.</param>
    /// <param name="timeoutSeconds">How long to wait for the host at each step.</param>
    /// <exception cref="ArgumentNullException"><paramref name="port"/> is null.</exception>
    public MoveRequesterSession(IMoveRequesterPort port, double timeoutSeconds)
    {
        this.port = port ?? throw new ArgumentNullException(nameof(port));
        this.timeoutSeconds = timeoutSeconds;
    }

    /// <summary>Gets where the move stands.</summary>
    public MoveRequesterState State { get; private set; }

    /// <summary>Starts a move, unless one is already running.</summary>
    /// <param name="newRequestId">The id of this request.</param>
    /// <param name="destinationName">The destination base's name.</param>
    /// <param name="now">The current time in seconds.</param>
    /// <returns>True when the request was sent.</returns>
    public bool Start(int newRequestId, string destinationName, double now)
    {
        if (State != MoveRequesterState.Idle)
        {
            return false;
        }

        requestId = newRequestId;
        warned = false;
        State = MoveRequesterState.AwaitingPreview;
        deadline = now + timeoutSeconds;
        port.SendRequest(requestId, destinationName);
        return true;
    }

    /// <summary>Handles the host's preview or refusal. Anything for another request or at another step is ignored.</summary>
    /// <param name="id">The request id it answers.</param>
    /// <param name="refusal">The refusal, or None.</param>
    /// <param name="playerName">The player a refusal names, or null.</param>
    /// <param name="plan">The plan's summary when not refused.</param>
    public void OnPreview(int id, MoveRefusal refusal, string? playerName, MoveSummary? plan)
    {
        if (id != requestId || State != MoveRequesterState.AwaitingPreview)
        {
            return;
        }

        if (refusal != MoveRefusal.None || plan is null)
        {
            State = MoveRequesterState.Idle;
            port.ShowRefused(refusal == MoveRefusal.None ? MoveRefusal.Failed : refusal, playerName);
            return;
        }

        summary = plan;
        State = MoveRequesterState.Previewing;
        port.ShowPreview(plan);
    }

    /// <summary>The player said yes to the preview. Starts holding the chests.</summary>
    /// <param name="now">The current time in seconds.</param>
    public void Confirm(double now)
    {
        if (State != MoveRequesterState.Previewing)
        {
            return;
        }

        State = MoveRequesterState.Holding;
        deadline = now + timeoutSeconds;
        port.BeginHolds();
    }

    /// <summary>The player said no to the preview. Nothing is held yet.</summary>
    public void Decline()
    {
        if (State == MoveRequesterState.Previewing)
        {
            State = MoveRequesterState.Idle;
            port.SendCancel(requestId);
        }
    }

    /// <summary>The holds finished. Revisions must be read after the last hold, since a hold may raise them.</summary>
    /// <param name="held">The held chests with their current revisions, or null when any hold failed.</param>
    /// <param name="now">The current time in seconds.</param>
    public void OnHoldsFinished(IReadOnlyList<MoveChestRevision>? held, double now)
    {
        if (State != MoveRequesterState.Holding)
        {
            return;
        }

        if (held is null)
        {
            // The confirm was never sent, so the host cannot have written and the holds are safe to drop.
            FinishRefused(MoveRefusal.TimedOut, null);
            port.SendCancel(requestId);
            return;
        }

        State = MoveRequesterState.AwaitingWrite;
        deadline = now + timeoutSeconds;
        port.SendConfirm(requestId, held);
    }

    /// <summary>Handles the host's cancel. The host wrote nothing, so the holds are safe to drop.</summary>
    /// <param name="id">The request id it answers.</param>
    /// <param name="refusal">The reason.</param>
    /// <param name="playerName">The player the reason names, or null.</param>
    public void OnCancelled(int id, MoveRefusal refusal, string? playerName)
    {
        if (id != requestId || State == MoveRequesterState.Idle || State == MoveRequesterState.Settling)
        {
            return;
        }

        FinishRefused(refusal, playerName);
    }

    /// <summary>Handles the host's report that it wrote. The holds stay until <see cref="Tick"/> sees every revision here.</summary>
    /// <param name="id">The request id it answers.</param>
    /// <param name="revisions">The revisions the host wrote, for the chests this game holds or knows. Not null.</param>
    /// <param name="done">The summary of what was written. Not null.</param>
    /// <param name="now">The current time in seconds.</param>
    /// <exception cref="ArgumentNullException"><paramref name="revisions"/> or <paramref name="done"/> is null.</exception>
    public void OnDone(int id, IReadOnlyList<MoveChestRevision> revisions, MoveSummary done, double now)
    {
        if (revisions is null)
        {
            throw new ArgumentNullException(nameof(revisions));
        }

        if (done is null)
        {
            throw new ArgumentNullException(nameof(done));
        }

        if (id != requestId || State != MoveRequesterState.AwaitingWrite)
        {
            return;
        }

        written = revisions;
        summary = done;
        State = MoveRequesterState.Settling;
        deadline = now + timeoutSeconds;
        warned = false;
        Tick(now);
    }

    /// <summary>The connection to the host ended. This game's copy of the world goes with it, so the holds are safe to drop.</summary>
    public void OnDisconnected()
    {
        if (State == MoveRequesterState.Idle)
        {
            return;
        }

        if (State != MoveRequesterState.AwaitingPreview && State != MoveRequesterState.Previewing)
        {
            port.ReleaseHolds();
        }

        State = MoveRequesterState.Idle;
    }

    /// <summary>Advances timeouts and the settle step. Call it regularly while a move runs.</summary>
    /// <param name="now">The current time in seconds.</param>
    public void Tick(double now)
    {
        switch (State)
        {
            case MoveRequesterState.AwaitingPreview:
                if (now >= deadline)
                {
                    State = MoveRequesterState.Idle;
                    port.ShowRefused(MoveRefusal.TimedOut, null);
                }

                break;
            case MoveRequesterState.Holding:
                if (now >= deadline)
                {
                    FinishRefused(MoveRefusal.TimedOut, null);
                    port.SendCancel(requestId);
                }

                break;
            case MoveRequesterState.AwaitingWrite:
                WarnAfterDeadline(now);
                break;
            case MoveRequesterState.Settling:
                if (AllWrittenRevisionsArrived())
                {
                    State = MoveRequesterState.Idle;
                    port.ReleaseHolds();
                    port.ShowDone(summary!);
                }
                else
                {
                    WarnAfterDeadline(now);
                }

                break;
        }
    }

    private void FinishRefused(MoveRefusal refusal, string? playerName)
    {
        var held = State == MoveRequesterState.Holding || State == MoveRequesterState.AwaitingWrite;
        State = MoveRequesterState.Idle;
        if (held)
        {
            port.ReleaseHolds();
        }

        port.ShowRefused(refusal, playerName);
    }

    private void WarnAfterDeadline(double now)
    {
        if (!warned && now >= deadline)
        {
            warned = true;
            port.ShowStillWaiting();
        }
    }

    private bool AllWrittenRevisionsArrived()
    {
        for (var i = 0; i < written.Count; i++)
        {
            var local = port.LocalRevision(written[i].Id);
            if (local.HasValue && local.Value < written[i].Revision)
            {
                return false;
            }
        }

        return true;
    }
}
