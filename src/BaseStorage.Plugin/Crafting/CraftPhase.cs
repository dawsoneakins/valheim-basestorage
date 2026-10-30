namespace BaseStorage.Plugin.Crafting;

/// <summary>The phase of the craft session (contract craft-from-base 3.1).</summary>
internal enum CraftPhase
{
    /// <summary>No craft is pending.</summary>
    Idle = 0,

    /// <summary>The executor runs; vanilla crafting was skipped.</summary>
    Executing = 1,

    /// <summary>The executor succeeded; the commit crafting call is running.</summary>
    Committing = 2,
}
