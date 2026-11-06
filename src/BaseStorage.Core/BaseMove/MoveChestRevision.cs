namespace BaseStorage.Core.BaseMove;

using BaseStorage.Core.Snapshots;

/// <summary>A chest's data revision at one moment of a move.</summary>
public readonly struct MoveChestRevision
{
    /// <summary>Creates the value.</summary>
    /// <param name="id">The chest id.</param>
    /// <param name="revision">The chest's data revision.</param>
    public MoveChestRevision(ContainerId id, uint revision)
    {
        Id = id;
        Revision = revision;
    }

    /// <summary>Gets the chest id.</summary>
    public ContainerId Id { get; }

    /// <summary>Gets the chest's data revision.</summary>
    public uint Revision { get; }
}
