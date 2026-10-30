namespace BaseStorage.Plugin.Reservations;

using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The game port that the reservation driver calls (contract S-E005-002 section 3.1). The driver is game-free and is tested
/// with a recording fake. Port members never throw for a destroyed or invalid chest: they return false or an observation
/// with ViewValid false, and the three commands do nothing.
/// </summary>
/// <typeparam name="TChest">The chest type of the game.</typeparam>
internal interface IReservationGame<TChest>
    where TChest : class
{
    /// <summary>Gets a value that is true when a world exists and a local session is known.</summary>
    bool InWorld { get; }

    /// <summary>Gets the id of a chest.</summary>
    /// <param name="chest">The chest.</param>
    /// <param name="id">The id, when the call returns true.</param>
    /// <returns>False when the view is invalid or has no ZDO.</returns>
    bool TryGetId(TChest chest, out ContainerId id);

    /// <summary>Observes the facts of one chest.</summary>
    /// <param name="chest">The chest.</param>
    /// <returns>The observation.</returns>
    ChestObservation Observe(TChest chest);

    /// <summary>Tells whether the view of a chest is valid.</summary>
    /// <param name="chest">The chest.</param>
    /// <returns>True when the view is valid and has a ZDO.</returns>
    bool IsViewValid(TChest chest);

    /// <summary>Tells whether the local peer owns a chest.</summary>
    /// <param name="chest">The chest.</param>
    /// <returns>True when the local peer owns the chest.</returns>
    bool IsOwner(TChest chest);

    /// <summary>Tells whether the local vanilla UI has a chest open.</summary>
    /// <param name="chest">The chest.</param>
    /// <returns>True when the local UI has this chest open.</returns>
    bool IsOpenLocally(TChest chest);

    /// <summary>Sends the open request to the owner of a chest.</summary>
    /// <param name="chest">The chest.</param>
    void SendOpenRequest(TChest chest);

    /// <summary>Claims the ownership of a chest for the local peer.</summary>
    /// <param name="chest">The chest.</param>
    void ClaimOwnership(TChest chest);

    /// <summary>Sets the in-use marker of a chest.</summary>
    /// <param name="chest">The chest.</param>
    /// <param name="inUse">The new value of the marker.</param>
    void SetInUse(TChest chest, bool inUse);
}
