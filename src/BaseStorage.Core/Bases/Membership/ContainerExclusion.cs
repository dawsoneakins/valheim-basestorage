namespace BaseStorage.Core.Bases.Membership;

using BaseStorage.Core.Snapshots;

/// <summary>One excluded container and the single reason it was excluded.</summary>
public sealed class ContainerExclusion
{
    internal ContainerExclusion(ContainerSnapshot container, ExclusionReason reason)
    {
        Container = container;
        Reason = reason;
    }

    /// <summary>Gets the excluded container snapshot, the same instance that was passed in.</summary>
    public ContainerSnapshot Container { get; }

    /// <summary>Gets the reason the container was excluded; always a defined <see cref="ExclusionReason"/> member.</summary>
    public ExclusionReason Reason { get; }
}
