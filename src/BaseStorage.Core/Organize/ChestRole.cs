namespace BaseStorage.Core.Organize;

/// <summary>The role of a usable chest in an Organize plan (contract 2.2).</summary>
public enum ChestRole
{
    /// <summary>A category chest (auto-assign outcome AlreadyAssigned or Assigned); compacted.</summary>
    Category = 0,

    /// <summary>An empty overflow chest claimed for one category in this plan; compacted.</summary>
    ClaimedOverflow = 1,

    /// <summary>An unassigned chest that holds items (auto-assign outcome SourceOnly); never a destination.</summary>
    Source = 2,

    /// <summary>An empty overflow chest that this plan did not claim; stays empty.</summary>
    UnusedOverflow = 3,
}
