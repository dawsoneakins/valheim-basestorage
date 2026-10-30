namespace BaseStorage.Core.Organize.Run;

/// <summary>What <see cref="OrganizeRunCoordinator.Start"/> did (contract section 2). The numeric values are contracted.</summary>
public enum OrganizeStartOutcome
{
    /// <summary>The run is previewing or running.</summary>
    Started = 0,

    /// <summary>A run of this coordinator is already previewing or running; the in-progress notice names the own holder.</summary>
    AlreadyActive = 1,

    /// <summary>There is no world or no local player; nothing is shown.</summary>
    NoPlayer = 2,

    /// <summary>The start is in no base; the not-in-base notice was shown.</summary>
    NotInBase = 3,

    /// <summary>The plan has no usable chest; the no-chests notice was shown.</summary>
    NoChests = 4,

    /// <summary>Another holder has a live lock on the base; the in-progress notice names that holder.</summary>
    LockRefused = 5,
}
