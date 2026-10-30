namespace BaseStorage.Core.Organize;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The immutable move plan of one Organize (contract 4.2). It lists the moves in execution order, the no-room entries, the
/// skipped chests, one layout per usable chest and the claimed overflow chests. It is safe to share across threads.
/// </summary>
public sealed class OrganizePlan
{
    private readonly Dictionary<ContainerId, ChestLayout> layoutsById;
    private readonly Dictionary<ContainerId, SkippedChest> skippedById;

    /// <summary>Creates a plan. Only the planner creates plans.</summary>
    /// <param name="autoAssign">The auto-assign result over the usable chests.</param>
    /// <param name="moves">The planned moves in execution order.</param>
    /// <param name="noRoom">The no-room entries in source-stack processing order.</param>
    /// <param name="skipped">The skipped chests in snapshot order.</param>
    /// <param name="layouts">One layout per usable chest in snapshot order.</param>
    /// <param name="claimedOverflow">The claimed overflow layouts in claim order; the same instances as in <paramref name="layouts"/>.</param>
    internal OrganizePlan(
        AutoAssignResult autoAssign,
        PlannedMove[] moves,
        NoRoomEntry[] noRoom,
        SkippedChest[] skipped,
        ChestLayout[] layouts,
        ChestLayout[] claimedOverflow)
    {
        AutoAssign = autoAssign;
        Moves = new ReadOnlyCollection<PlannedMove>(moves);
        NoRoom = new ReadOnlyCollection<NoRoomEntry>(noRoom);
        Skipped = new ReadOnlyCollection<SkippedChest>(skipped);
        Layouts = new ReadOnlyCollection<ChestLayout>(layouts);
        ClaimedOverflow = new ReadOnlyCollection<ChestLayout>(claimedOverflow);

        layoutsById = new Dictionary<ContainerId, ChestLayout>(layouts.Length);
        for (var i = 0; i < layouts.Length; i++)
        {
            layoutsById[layouts[i].Id] = layouts[i];
        }

        skippedById = new Dictionary<ContainerId, SkippedChest>(skipped.Length);
        for (var i = 0; i < skipped.Length; i++)
        {
            skippedById[skipped[i].Id] = skipped[i];
        }
    }

    /// <summary>Gets the auto-assign result over the usable chests.</summary>
    public AutoAssignResult AutoAssign { get; }

    /// <summary>Gets the planned moves in execution order, read-only.</summary>
    public IReadOnlyList<PlannedMove> Moves { get; }

    /// <summary>Gets the misplaced stack remainders that found no room, in source-stack processing order, read-only.</summary>
    public IReadOnlyList<NoRoomEntry> NoRoom { get; }

    /// <summary>Gets the skipped chests, in snapshot order with each id at most once, read-only.</summary>
    public IReadOnlyList<SkippedChest> Skipped { get; }

    /// <summary>Gets one final layout per usable chest, in snapshot order, read-only.</summary>
    public IReadOnlyList<ChestLayout> Layouts { get; }

    /// <summary>Gets the claimed overflow layouts in claim order; the same instances as in <see cref="Layouts"/>, read-only.</summary>
    public IReadOnlyList<ChestLayout> ClaimedOverflow { get; }

    /// <summary>Returns the layout of a usable chest.</summary>
    /// <param name="id">The container id.</param>
    /// <returns>The layout of the usable chest, or null for a skipped or unknown id.</returns>
    public ChestLayout? FindLayout(ContainerId id) => layoutsById.TryGetValue(id, out var layout) ? layout : null;

    /// <summary>Returns the skipped entry of a chest.</summary>
    /// <param name="id">The container id.</param>
    /// <returns>The skipped entry, or null for a usable or unknown id.</returns>
    public SkippedChest? FindSkipped(ContainerId id) => skippedById.TryGetValue(id, out var skipped) ? skipped : null;
}
