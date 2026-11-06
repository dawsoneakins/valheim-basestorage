namespace BaseStorage.Core.BaseMove;

using System;
using BaseStorage.Core.Snapshots;

/// <summary>A requester's ask: move the base around this station to the base with this name.</summary>
public sealed class MoveRequest
{
    /// <summary>Creates the request.</summary>
    /// <param name="requestId">The request id.</param>
    /// <param name="sourceStation">The id of a station in the source base.</param>
    /// <param name="requesterPlayerId">The requester's player id, for privacy and ward checks.</param>
    /// <param name="destinationName">The destination base's name, for messages. Not null.</param>
    /// <param name="destinationStation">The id of a station in the destination base.</param>
    /// <exception cref="ArgumentNullException"><paramref name="destinationName"/> is null.</exception>
    public MoveRequest(int requestId, ContainerId sourceStation, long requesterPlayerId, string destinationName, ContainerId destinationStation = default)
    {
        RequestId = requestId;
        SourceStation = sourceStation;
        RequesterPlayerId = requesterPlayerId;
        DestinationName = destinationName ?? throw new ArgumentNullException(nameof(destinationName));
        DestinationStation = destinationStation;
    }

    /// <summary>Gets the request id.</summary>
    public int RequestId { get; }

    /// <summary>Gets the id of a station in the source base.</summary>
    public ContainerId SourceStation { get; }

    /// <summary>Gets the requester's player id.</summary>
    public long RequesterPlayerId { get; }

    /// <summary>Gets the destination base's name.</summary>
    public string DestinationName { get; }

    /// <summary>Gets the id of a station in the destination base.</summary>
    public ContainerId DestinationStation { get; }
}
