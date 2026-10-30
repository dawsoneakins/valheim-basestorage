namespace BaseStorage.Core.Organize;

using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Snapshots;

/// <summary>
/// A chest that an Organize plan does not use (contract 4.5). Its contents are in no move, no no-room entry and no layout.
/// </summary>
public sealed class SkippedChest
{
    /// <summary>Creates a skipped entry. Only the planner creates entries.</summary>
    /// <param name="container">The input snapshot.</param>
    /// <param name="kind">The reason kind.</param>
    /// <param name="reason">The fixed English diagnostic reason.</param>
    /// <param name="playerName">The player's name for an in-use skip with a known name; otherwise null.</param>
    /// <param name="exclusion">The eligibility verdict for a Keep or Ineligible skip; otherwise null.</param>
    internal SkippedChest(ContainerSnapshot container, SkipReason kind, string reason, string? playerName, ExclusionReason? exclusion)
    {
        Container = container;
        Id = container.Id;
        Kind = kind;
        Reason = reason;
        PlayerName = playerName;
        Exclusion = exclusion;
    }

    /// <summary>Gets the input snapshot, the same instance as passed in the containers argument.</summary>
    public ContainerSnapshot Container { get; }

    /// <summary>Gets the container id, the same as <see cref="Container"/> id.</summary>
    public ContainerId Id { get; }

    /// <summary>Gets the reason kind of the skip.</summary>
    public SkipReason Kind { get; }

    /// <summary>Gets the non-empty, fixed English diagnostic reason. It is not a localized UI string.</summary>
    public string Reason { get; }

    /// <summary>Gets the player's name for an in-use skip with a known name; otherwise null.</summary>
    public string? PlayerName { get; }

    /// <summary>Gets the eligibility verdict for a Keep or Ineligible skip; otherwise null.</summary>
    public ExclusionReason? Exclusion { get; }
}
