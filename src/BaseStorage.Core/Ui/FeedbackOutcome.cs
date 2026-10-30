namespace BaseStorage.Core.Ui;

/// <summary>Every user-action outcome at this base (contract feedback-audit 2.1); the numeric values are contracted.</summary>
public enum FeedbackOutcome
{
    /// <summary>The craft was made from the base chests.</summary>
    CraftCreated = 0,

    /// <summary>Vanilla declined the craft at commit and the materials went back.</summary>
    CraftReturned = 1,

    /// <summary>The materials could not be taken.</summary>
    CraftFailed = 2,

    /// <summary>The pre-flight check failed at the start of the craft.</summary>
    CraftPreFlightFailed = 3,

    /// <summary>There was no plan at craft time.</summary>
    CraftUnavailable = 4,

    /// <summary>The panel was closed while the craft waited.</summary>
    CraftAbandoned = 5,

    /// <summary>The piece was placed with materials from the base chests.</summary>
    BuildPlaced = 10,

    /// <summary>Vanilla declined the placement after the take.</summary>
    BuildReturned = 11,

    /// <summary>The materials could not be taken.</summary>
    BuildFailed = 12,

    /// <summary>The pre-flight check failed while waiting.</summary>
    BuildPreFlightFailed = 13,

    /// <summary>There was no plan at place time.</summary>
    BuildUnavailable = 14,

    /// <summary>Place mode was left while waiting.</summary>
    BuildAbandoned = 15,

    /// <summary>The game refused the placement.</summary>
    BuildPlacementRefused = 16,

    /// <summary>The chest category was written.</summary>
    CategoryApplied = 20,

    /// <summary>The chest denied access.</summary>
    CategoryDenied = 21,

    /// <summary>The chest is in use by another player.</summary>
    CategoryInUse = 22,

    /// <summary>The chest owner did not answer.</summary>
    CategoryTimeout = 23,

    /// <summary>The chest is not available.</summary>
    CategoryUnavailable = 24,

    /// <summary>The chest already held the requested value.</summary>
    CategoryUnchanged = 25,

    /// <summary>The same write is already pending.</summary>
    CategoryAlreadyPending = 26,

    /// <summary>The chest is gone.</summary>
    CategoryChestGone = 27,

    /// <summary>The run completed.</summary>
    OrganizeCompleted = 30,

    /// <summary>The run stopped early.</summary>
    OrganizeStopped = 31,

    /// <summary>The preview was cancelled.</summary>
    OrganizeCancelled = 32,

    /// <summary>The player is not in a base.</summary>
    OrganizeNotInBase = 33,

    /// <summary>The base has no chests to use.</summary>
    OrganizeNoChests = 34,

    /// <summary>A run is already active.</summary>
    OrganizeAlreadyActive = 35,

    /// <summary>The base lock was refused.</summary>
    OrganizeLockRefused = 36,

    /// <summary>The lock was refused at confirm.</summary>
    OrganizeConfirmRefused = 37,

    /// <summary>There is no local player.</summary>
    OrganizeNoPlayer = 38,

    /// <summary>The world is gone.</summary>
    OrganizeWorldGone = 39,

    /// <summary>The run completed.</summary>
    DepositCompleted = 50,

    /// <summary>The run stopped early.</summary>
    DepositStopped = 51,

    /// <summary>The preview was cancelled.</summary>
    DepositCancelled = 52,

    /// <summary>The player is not in a base.</summary>
    DepositNotInBase = 53,

    /// <summary>The base has no chests to use.</summary>
    DepositNoChests = 54,

    /// <summary>A run is already active.</summary>
    DepositAlreadyActive = 55,

    /// <summary>The base lock was refused.</summary>
    DepositLockRefused = 56,

    /// <summary>The lock was refused at confirm.</summary>
    DepositConfirmRefused = 57,

    /// <summary>There is no local player.</summary>
    DepositNoPlayer = 58,

    /// <summary>The world is gone.</summary>
    DepositWorldGone = 59,

    /// <summary>A slot was marked as favourite.</summary>
    FavouriteAdded = 70,

    /// <summary>A slot was unmarked as favourite.</summary>
    FavouriteRemoved = 71,
}
