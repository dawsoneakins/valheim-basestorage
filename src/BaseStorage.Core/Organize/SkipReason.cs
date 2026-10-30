namespace BaseStorage.Core.Organize;

/// <summary>Why an Organize plan skips a chest (contract 2.1).</summary>
public enum SkipReason
{
    /// <summary>The chest is persisted as Keep (AC-7).</summary>
    Keep = 0,

    /// <summary>Another player has the chest open (AC-8).</summary>
    InUse = 1,

    /// <summary>The chest is not eligible for an Organize: tombstone, vehicle, not player-built or access denied (AC-9).</summary>
    Ineligible = 2,

    /// <summary>The chest has unclear contents: more stacks than slots, or a stack above its max stack (NFR safety).</summary>
    InvalidContents = 3,
}
