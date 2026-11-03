namespace BaseStorage.Core.Restock;

using BaseStorage.Core.Snapshots;

/// <summary>One move of a restock plan: items from one usable source stack into one target (S-QR-001, contract 4.3). Result object with no public constructor.</summary>
public sealed class RestockMove
{
    private readonly ContainerId source;
    private readonly int sourceStackIndex;
    private readonly ItemStackSnapshot sourceStack;
    private readonly int targetIndex;
    private readonly ItemStackSnapshot targetStack;
    private readonly int count;

    internal RestockMove(
        ContainerId source,
        int sourceStackIndex,
        ItemStackSnapshot sourceStack,
        int targetIndex,
        ItemStackSnapshot targetStack,
        int count)
    {
        this.source = source;
        this.sourceStackIndex = sourceStackIndex;
        this.sourceStack = sourceStack;
        this.targetIndex = targetIndex;
        this.targetStack = targetStack;
        this.count = count;
    }

    /// <summary>Gets the source chest id, a usable source.</summary>
    public ContainerId Source => source;

    /// <summary>Gets the index of the source stack in the source chest's input contents.</summary>
    public int SourceStackIndex => sourceStackIndex;

    /// <summary>Gets the source stack as planned: the same instance as that chest's input contents at <see cref="SourceStackIndex"/>.</summary>
    public ItemStackSnapshot SourceStack => sourceStack;

    /// <summary>Gets the index of the target in the carried list.</summary>
    public int TargetIndex => targetIndex;

    /// <summary>Gets the target as planned: the same instance as the carried stack at <see cref="TargetIndex"/>.</summary>
    public ItemStackSnapshot TargetStack => targetStack;

    /// <summary>Gets the number of items moved; at least 1.</summary>
    public int Count => count;
}
