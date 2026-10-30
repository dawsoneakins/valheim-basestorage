namespace BaseStorage.Core.ChestAccess;

using System;

/// <summary>
/// The facts the Plugin reads for one chest at one moment (contract S-E005-002 section 2.1). The constructor never throws:
/// every combination of raw facts is representable, and the rules of <see cref="ChestAccessRules"/> decide what it means.
/// Equality compares all ten members, with <see cref="OwnerName"/> compared ordinally.
/// </summary>
public sealed class ChestObservation : IEquatable<ChestObservation>
{
    private readonly bool viewValid;
    private readonly bool accessAllowed;
    private readonly bool openLocally;
    private readonly bool inUseFlag;
    private readonly long localSessionId;
    private readonly long ownerSessionId;
    private readonly bool ownerListed;
    private readonly bool ownerConnected;
    private readonly string? ownerName;
    private readonly bool isServer;

    /// <summary>Creates an observation from the raw facts of one chest.</summary>
    /// <param name="viewValid">True when the view is valid and has a network object.</param>
    /// <param name="accessAllowed">True when vanilla access allows the local player (privacy and ward).</param>
    /// <param name="openLocally">True when the local vanilla UI has this chest open.</param>
    /// <param name="inUseFlag">True when the in-use flag of the network object is set.</param>
    /// <param name="localSessionId">The local session id; 0 when there is no world.</param>
    /// <param name="ownerSessionId">The owner session id; 0 when there is no owner.</param>
    /// <param name="ownerListed">True when a listed player has the owner session id.</param>
    /// <param name="ownerConnected">True when the owner is connected, as seen from this peer.</param>
    /// <param name="ownerName">The listed name of the owner, or null when not listed.</param>
    /// <param name="isServer">True when this peer is the host or a single-player world.</param>
    public ChestObservation(
        bool viewValid,
        bool accessAllowed,
        bool openLocally,
        bool inUseFlag,
        long localSessionId,
        long ownerSessionId,
        bool ownerListed,
        bool ownerConnected,
        string? ownerName,
        bool isServer)
    {
        this.viewValid = viewValid;
        this.accessAllowed = accessAllowed;
        this.openLocally = openLocally;
        this.inUseFlag = inUseFlag;
        this.localSessionId = localSessionId;
        this.ownerSessionId = ownerSessionId;
        this.ownerListed = ownerListed;
        this.ownerConnected = ownerConnected;
        this.ownerName = ownerName;
        this.isServer = isServer;
    }

    /// <summary>Gets a value that is true when the view is valid and has a network object.</summary>
    public bool ViewValid => viewValid;

    /// <summary>Gets a value that is true when vanilla access allows the local player.</summary>
    public bool AccessAllowed => accessAllowed;

    /// <summary>Gets a value that is true when the local vanilla UI has this chest open.</summary>
    public bool OpenLocally => openLocally;

    /// <summary>Gets a value that is true when the in-use flag of the network object is set.</summary>
    public bool InUseFlag => inUseFlag;

    /// <summary>Gets the local session id; 0 when there is no world.</summary>
    public long LocalSessionId => localSessionId;

    /// <summary>Gets the owner session id; 0 means no owner.</summary>
    public long OwnerSessionId => ownerSessionId;

    /// <summary>Gets a value that is true when a listed player has the owner session id.</summary>
    public bool OwnerListed => ownerListed;

    /// <summary>Gets a value that is true when the owner is connected, as seen from this peer.</summary>
    public bool OwnerConnected => ownerConnected;

    /// <summary>Gets the listed name of the owner, or null when the owner is not listed.</summary>
    public string? OwnerName => ownerName;

    /// <summary>Gets a value that is true when this peer is the host or a single-player world.</summary>
    public bool IsServer => isServer;

    /// <summary>Compares every member with another observation.</summary>
    /// <param name="other">The observation to compare with, or null.</param>
    /// <returns>True when all ten members are equal.</returns>
    public bool Equals(ChestObservation? other) =>
        other is not null
        && viewValid == other.viewValid
        && accessAllowed == other.accessAllowed
        && openLocally == other.openLocally
        && inUseFlag == other.inUseFlag
        && localSessionId == other.localSessionId
        && ownerSessionId == other.ownerSessionId
        && ownerListed == other.ownerListed
        && ownerConnected == other.ownerConnected
        && string.Equals(ownerName, other.ownerName, StringComparison.Ordinal)
        && isServer == other.isServer;

    /// <summary>Compares this observation with an object.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when the object is an equal observation.</returns>
    public override bool Equals(object? obj) => Equals(obj as ChestObservation);

    /// <summary>Gets a hash code that agrees with <see cref="Equals(ChestObservation?)"/>.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = viewValid ? 1 : 0;
            hash = (hash * 31) + (accessAllowed ? 1 : 0);
            hash = (hash * 31) + (openLocally ? 1 : 0);
            hash = (hash * 31) + (inUseFlag ? 1 : 0);
            hash = (hash * 31) + localSessionId.GetHashCode();
            hash = (hash * 31) + ownerSessionId.GetHashCode();
            hash = (hash * 31) + (ownerListed ? 1 : 0);
            hash = (hash * 31) + (ownerConnected ? 1 : 0);
            hash = (hash * 31) + (ownerName is null ? 0 : StringComparer.Ordinal.GetHashCode(ownerName));
            hash = (hash * 31) + (isServer ? 1 : 0);
            return hash;
        }
    }

    /// <summary>Compares two observations; two nulls are equal.</summary>
    /// <param name="left">The left observation, or null.</param>
    /// <param name="right">The right observation, or null.</param>
    /// <returns>True when the observations are equal.</returns>
    public static bool operator ==(ChestObservation? left, ChestObservation? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Compares two observations for inequality.</summary>
    /// <param name="left">The left observation, or null.</param>
    /// <param name="right">The right observation, or null.</param>
    /// <returns>True when the observations are not equal.</returns>
    public static bool operator !=(ChestObservation? left, ChestObservation? right) => !(left == right);
}
