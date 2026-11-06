namespace BaseStorage.Core.BaseMove;

using System.Collections.Generic;
using BaseStorage.Core.Snapshots;

/// <summary>What <see cref="MoveRequesterSession"/> needs from the game and the network. Main thread only.</summary>
public interface IMoveRequesterPort
{
    /// <summary>Asks the host to plan a move.</summary>
    /// <param name="requestId">The request id.</param>
    /// <param name="destinationName">The destination base's name.</param>
    void SendRequest(int requestId, string destinationName);

    /// <summary>Shows the preview and waits for the player's answer.</summary>
    /// <param name="summary">The plan's summary.</param>
    void ShowPreview(MoveSummary summary);

    /// <summary>Starts holding the source chests this game has loaded. The result comes back through <see cref="MoveRequesterSession.OnHoldsFinished"/>.</summary>
    void BeginHolds();

    /// <summary>Releases every chest this session holds.</summary>
    void ReleaseHolds();

    /// <summary>Tells the host the player confirmed and which chests are held at which revision.</summary>
    /// <param name="requestId">The request id.</param>
    /// <param name="held">The held chests with their revisions.</param>
    void SendConfirm(int requestId, IReadOnlyList<MoveChestRevision> held);

    /// <summary>Tells the host the player backed out or the holds failed.</summary>
    /// <param name="requestId">The request id.</param>
    void SendCancel(int requestId);

    /// <summary>Gets this game's current revision of a chest.</summary>
    /// <param name="id">The chest id.</param>
    /// <returns>The revision, or null when this game no longer knows the chest.</returns>
    uint? LocalRevision(ContainerId id);

    /// <summary>Shows the final line of a finished move.</summary>
    /// <param name="summary">The plan's summary.</param>
    void ShowDone(MoveSummary summary);

    /// <summary>Shows why a move did not happen. Nothing was changed.</summary>
    /// <param name="refusal">The reason.</param>
    /// <param name="playerName">The player the reason names, or null.</param>
    void ShowRefused(MoveRefusal refusal, string? playerName);

    /// <summary>Warns the player and logs at Error that the host's write has not arrived, so the chests stay held.</summary>
    void ShowStillWaiting();
}
