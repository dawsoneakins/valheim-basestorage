namespace BaseStorage.Core.BaseMove;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Snapshots;

/// <summary>The outcome of planning one move between bases. Never applies anything.</summary>
public sealed class MovePlan
{
    private const ulong FnvOffset = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;

    /// <summary>Creates the plan.</summary>
    /// <param name="moves">The moves.</param>
    /// <param name="noRoom">The source stacks, or parts of them, the destination has no room for.</param>
    /// <param name="stayed">The source stacks the portal rule keeps in place.</param>
    /// <param name="skippedSources">The source chests that give up nothing.</param>
    /// <param name="skippedDestinations">The destination chests that receive nothing.</param>
    /// <param name="rulesToSave">The destination chest categories this plan relies on.</param>
    internal MovePlan(
        PlannedMove[] moves,
        NoRoomEntry[] noRoom,
        StayedStack[] stayed,
        SkippedChest[] skippedSources,
        SkippedChest[] skippedDestinations,
        IReadOnlyList<ChestAutoAssignment> rulesToSave)
    {
        Moves = new ReadOnlyCollection<PlannedMove>(moves);
        NoRoom = new ReadOnlyCollection<NoRoomEntry>(noRoom);
        Stayed = new ReadOnlyCollection<StayedStack>(stayed);
        SkippedSources = new ReadOnlyCollection<SkippedChest>(skippedSources);
        SkippedDestinations = new ReadOnlyCollection<SkippedChest>(skippedDestinations);
        RulesToSave = rulesToSave;
        Fingerprint = ComputeFingerprint();
    }

    /// <summary>Gets the moves, in the order they apply.</summary>
    public IReadOnlyList<PlannedMove> Moves { get; }

    /// <summary>Gets the source stacks, or parts of them, the destination has no room for. They stay where they are.</summary>
    public IReadOnlyList<NoRoomEntry> NoRoom { get; }

    /// <summary>Gets the source stacks the portal rule keeps in place.</summary>
    public IReadOnlyList<StayedStack> Stayed { get; }

    /// <summary>Gets the source chests that give up nothing.</summary>
    public IReadOnlyList<SkippedChest> SkippedSources { get; }

    /// <summary>Gets the destination chests that receive nothing.</summary>
    public IReadOnlyList<SkippedChest> SkippedDestinations { get; }

    /// <summary>Gets the destination chest categories this plan relies on; they are saved with the move.</summary>
    public IReadOnlyList<ChestAutoAssignment> RulesToSave { get; }

    /// <summary>Gets a value that is the same for the same plan on any machine; a re-plan with another value means the world changed.</summary>
    public ulong Fingerprint { get; }

    /// <summary>Gets the number of source stacks that give up at least one item.</summary>
    public int MovedStacks
    {
        get
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Moves.Count; i++)
            {
                seen.Add(Text(Moves[i].Source) + "#" + Moves[i].SourceStackIndex.ToString(CultureInfo.InvariantCulture));
            }

            return seen.Count;
        }
    }

    /// <summary>Lists every chest the plan writes: both ends of each move and each chest with a category to save, sorted by id.</summary>
    /// <returns>The chest ids, without duplicates.</returns>
    public IReadOnlyList<ContainerId> ChangedChests()
    {
        var ids = new SortedSet<ContainerId>();
        for (var i = 0; i < Moves.Count; i++)
        {
            ids.Add(Moves[i].Source);
            ids.Add(Moves[i].Destination);
        }

        for (var i = 0; i < RulesToSave.Count; i++)
        {
            ids.Add(RulesToSave[i].Id);
        }

        return new List<ContainerId>(ids).AsReadOnly();
    }

    /// <summary>Lists the distinct prefab names the portal rule keeps in place, sorted ordinally.</summary>
    /// <returns>The prefab names.</returns>
    public IReadOnlyList<string> StayedPrefabs()
    {
        var names = new SortedSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < Stayed.Count; i++)
        {
            names.Add(Stayed[i].Stack.PrefabName);
        }

        return new List<string>(names).AsReadOnly();
    }

    private static string Text(ContainerId id) =>
        id.UserId.ToString(CultureInfo.InvariantCulture) + ":" + id.Id.ToString(CultureInfo.InvariantCulture);

    private static ulong Mix(ulong hash, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        for (var i = 0; i < bytes.Length; i++)
        {
            hash = unchecked((hash ^ bytes[i]) * FnvPrime);
        }

        return hash;
    }

    private ulong ComputeFingerprint()
    {
        var text = new StringBuilder();
        for (var i = 0; i < Moves.Count; i++)
        {
            var m = Moves[i];
            text.Append("M;").Append(Text(m.Source)).Append(';').Append(m.SourceStackIndex).Append(';').Append(Text(m.Destination))
                .Append(';').Append(m.PrefabName).Append(';').Append(m.Quality).Append(';').Append(m.Count).Append('\n');
        }

        for (var i = 0; i < NoRoom.Count; i++)
        {
            var n = NoRoom[i];
            text.Append("N;").Append(Text(n.Source)).Append(';').Append(n.SourceStackIndex).Append(';').Append(n.PrefabName)
                .Append(';').Append(n.Quality).Append(';').Append(n.Count).Append('\n');
        }

        for (var i = 0; i < Stayed.Count; i++)
        {
            var s = Stayed[i];
            text.Append("S;").Append(Text(s.Source)).Append(';').Append(s.StackIndex).Append(';').Append(s.Stack.PrefabName)
                .Append(';').Append(s.Stack.Quality).Append(';').Append(s.Stack.Count).Append('\n');
        }

        return Mix(FnvOffset, text.ToString());
    }
}
