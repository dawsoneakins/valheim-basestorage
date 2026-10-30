namespace BaseStorage.Core.ChestCategories;

/// <summary>What the write driver does with a pending write on one frame, given its reservation state (contract S-E004-003 section 2.4).</summary>
public enum PendingWriteAction
{
    /// <summary>The reservation is still requesting: keep waiting, unless the deadline has passed.</summary>
    Wait = 0,

    /// <summary>The reservation is granted: begin writing, write the value when the gate allows it, then release.</summary>
    Write = 1,

    /// <summary>The reservation ended without a write: release it and tell the player why.</summary>
    Fail = 2,
}
