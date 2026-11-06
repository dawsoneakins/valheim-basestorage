namespace BaseStorage.Plugin.BaseMove;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.BaseMove;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;

/// <summary>The requester's game side of a move: network messages, the preview popup, holds and result lines. Main thread only.</summary>
internal sealed class GameMoveRequester : IMoveRequesterPort
{
    private readonly Action<MoveMessage> sendToHost;
    private readonly Action<string> show;
    private readonly MoveHolds holds;
    private readonly ILogSink log;
    private IReadOnlyList<ContainerId> chests = Array.Empty<ContainerId>();
    private string destination = string.Empty;
    private ContainerId sourceStation;
    private ContainerId destinationStation;
    private string sourceName = string.Empty;
    private long playerId;

    internal GameMoveRequester(Action<MoveMessage> sendToHost, Action<string> show, MoveHolds holds, ILogSink log)
    {
        this.sendToHost = sendToHost;
        this.show = show;
        this.holds = holds;
        this.log = log;
    }

    /// <summary>Gets or sets the session this port serves.</summary>
    internal MoveRequesterSession? Session { get; set; }

    /// <summary>Gets or sets the time source, in seconds.</summary>
    internal Func<double>? Now { get; set; }

    /// <summary>Gets or sets a value indicating whether the plugin should poll this requester's holds.</summary>
    internal bool WaitingForHolds { get; set; }

    /// <summary>Sets what the next request asks for.</summary>
    /// <param name="station">A station in the source base.</param>
    /// <param name="name">The source base's name.</param>
    /// <param name="destinationAt">A station in the destination base.</param>
    /// <param name="player">The local player's id.</param>
    internal void Prepare(ContainerId station, string name, ContainerId destinationAt, long player)
    {
        sourceStation = station;
        sourceName = name;
        destinationStation = destinationAt;
        playerId = player;
    }

    /// <inheritdoc/>
    public void SendRequest(int requestId, string destinationName)
    {
        destination = destinationName;
        sendToHost(new MoveMessage
        {
            Kind = MoveMessageKind.Request,
            RequestId = requestId,
            SourceStation = sourceStation,
            RequesterPlayerId = playerId,
            DestinationName = destinationName,
            DestinationStation = destinationStation,
        });
    }

    /// <inheritdoc/>
    public void ShowPreview(MoveSummary summary)
    {
        chests = summary.ChangedChests;
        if (summary.MovedStacks == 0)
        {
            show(StringTable.English.Format(StringKeys.MoveNothing, destination));
            Session!.Decline();
            return;
        }

        if (!UnifiedPopup.IsAvailable())
        {
            Session!.Decline();
            return;
        }

        UnifiedPopup.Push(new YesNoPopup(
            SingleLine.Of(StringTable.English.Format(StringKeys.MovePreviewHeader, sourceName, destination)),
            MoveText.Preview(summary, StringTable.English),
            OnYes,
            OnNo,
            false,
            true));
    }

    /// <inheritdoc/>
    public void BeginHolds()
    {
        if (!holds.Begin(chests, null))
        {
            Session!.OnHoldsFinished(null, Now!());
            return;
        }

        WaitingForHolds = true;
    }

    /// <inheritdoc/>
    public void ReleaseHolds()
    {
        WaitingForHolds = false;
        holds.Release();
    }

    /// <inheritdoc/>
    public void SendConfirm(int requestId, IReadOnlyList<MoveChestRevision> held) =>
        sendToHost(new MoveMessage { Kind = MoveMessageKind.Confirm, RequestId = requestId, Revisions = held });

    /// <inheritdoc/>
    public void SendCancel(int requestId) =>
        sendToHost(new MoveMessage { Kind = MoveMessageKind.Cancel, RequestId = requestId });

    /// <inheritdoc/>
    public uint? LocalRevision(ContainerId id) => MoveHolds.RevisionOf(id);

    /// <inheritdoc/>
    public void ShowDone(MoveSummary summary) => show(MoveText.Done(summary, destination, StringTable.English));

    /// <inheritdoc/>
    public void ShowRefused(MoveRefusal refusal, string? playerName) => show(MoveText.Refused(refusal, playerName, StringTable.English));

    /// <inheritdoc/>
    public void ShowStillWaiting()
    {
        log.Log(LogSeverity.Error, "Base move: the host's write has not arrived; chests stay held so nothing can be duplicated.");
        show(StringTable.English.Get(StringKeys.MoveStillWaiting));
    }

    private void OnYes()
    {
        UnifiedPopup.Pop();
        Session!.Confirm(Now!());
    }

    private void OnNo()
    {
        UnifiedPopup.Pop();
        Session!.Decline();
    }
}
