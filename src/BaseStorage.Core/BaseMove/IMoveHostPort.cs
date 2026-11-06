namespace BaseStorage.Core.BaseMove;

using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>What <see cref="MoveHostCoordinator"/> needs from the game and the network. Main thread only.</summary>
public interface IMoveHostPort
{
    /// <summary>Starts finding both bases and planning across frames. The result comes back through <see cref="MoveHostCoordinator.OnPlanReady"/>.</summary>
    /// <param name="peer">The requester.</param>
    /// <param name="request">The request.</param>
    void BeginPlan(long peer, MoveRequest request);

    /// <summary>Sends the preview or a refusal.</summary>
    /// <param name="peer">The requester.</param>
    /// <param name="requestId">The request id.</param>
    /// <param name="refusal">The refusal, or None.</param>
    /// <param name="playerName">The player a refusal names, or null.</param>
    /// <param name="summary">The summary when not refused.</param>
    void SendPreview(long peer, int requestId, MoveRefusal refusal, string? playerName, MoveSummary? summary);

    /// <summary>
    /// Starts holding the involved chests the host has loaded, except those the requester already holds: asking for one of those would be
    /// refused as in use and cancel the move. The result comes back through <see cref="MoveHostCoordinator.OnHostHoldsFinished"/>.
    /// </summary>
    /// <param name="heldByRequester">The chests the requester holds.</param>
    void BeginHostHolds(IReadOnlyList<MoveChestRevision> heldByRequester);

    /// <summary>Releases what the host holds and every lock of this move, and forgets the plan.</summary>
    void EndMove();

    /// <summary>Gets the host's revision of a chest.</summary>
    /// <param name="id">The chest id.</param>
    /// <returns>The revision, or null when the chest is gone.</returns>
    uint? LocalRevision(ContainerId id);

    /// <summary>Plans again from the current chest data of the bases found earlier. Writes nothing.</summary>
    /// <returns>The fingerprint of the new plan, or null when planning failed.</returns>
    ulong? Replan();

    /// <summary>Writes the latest plan to every changed chest in this frame, or writes nothing at all.</summary>
    /// <returns>The written revisions, or null when nothing was written.</returns>
    IReadOnlyList<MoveChestRevision>? Apply();

    /// <summary>Tells the requester the write happened.</summary>
    /// <param name="peer">The requester.</param>
    /// <param name="requestId">The request id.</param>
    /// <param name="revisions">The written revisions.</param>
    /// <param name="summary">The summary.</param>
    void SendDone(long peer, int requestId, IReadOnlyList<MoveChestRevision> revisions, MoveSummary summary);

    /// <summary>Tells the requester the move was cancelled and nothing was written.</summary>
    /// <param name="peer">The requester.</param>
    /// <param name="requestId">The request id.</param>
    /// <param name="refusal">The reason.</param>
    /// <param name="playerName">The player the reason names, or null.</param>
    void SendCancelled(long peer, int requestId, MoveRefusal refusal, string? playerName);
}
