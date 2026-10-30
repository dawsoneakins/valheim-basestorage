namespace BaseStorage.Plugin.World;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// Remembers the snapshot built for each live item in the last completed build (contract 6.6, ADR-GWIN-030), so an
/// unchanged item keeps its snapshot instance between rebuilds (AC-6). Keys use reference identity. Game-free (W1).
/// </summary>
/// <typeparam name="TItem">The item type. Compared by reference identity.</typeparam>
/// <typeparam name="TSnapshot">The snapshot type kept for each item.</typeparam>
internal sealed class SnapshotMemo<TItem, TSnapshot>
    where TItem : class
    where TSnapshot : class
{
    private readonly Dictionary<TItem, TSnapshot> sideA = new Dictionary<TItem, TSnapshot>(ReferenceComparer.Instance);
    private readonly Dictionary<TItem, TSnapshot> sideB = new Dictionary<TItem, TSnapshot>(ReferenceComparer.Instance);
    private bool sideAIsPrevious;
    private bool building;

    /// <summary>Creates an empty memo with no completed build.</summary>
    internal SnapshotMemo()
    {
    }

    /// <summary>Gets the number of entries held after the last completed build.</summary>
    internal int Count => this.PreviousSet().Count;

    /// <summary>Gets a value that tells whether a build is in progress (between Begin and End).</summary>
    internal bool IsBuilding => this.building;

    /// <summary>Starts a build. Calling it while a build is in progress restarts the build and keeps the previous set.</summary>
    internal void Begin()
    {
        this.CurrentSet().Clear();
        this.building = true;
    }

    /// <summary>Gets the snapshot kept for an item by the last completed build.</summary>
    /// <param name="item">The item. Null gives null.</param>
    /// <returns>The kept snapshot, or null when the last completed build did not keep the item.</returns>
    /// <exception cref="InvalidOperationException">No build is in progress.</exception>
    internal TSnapshot? Previous(TItem item)
    {
        this.RequireBuilding();
        if (item is null)
        {
            return null;
        }

        return this.PreviousSet().TryGetValue(item, out TSnapshot? kept) ? kept : null;
    }

    /// <summary>Records a snapshot for an item in the current build. A later call for the same item replaces the earlier one.</summary>
    /// <param name="item">The item.</param>
    /// <param name="snapshot">The snapshot to keep.</param>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> or <paramref name="snapshot"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No build is in progress.</exception>
    internal void Keep(TItem item, TSnapshot snapshot)
    {
        this.RequireBuilding();
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        this.CurrentSet()[item] = snapshot;
    }

    /// <summary>Completes the build. The current set becomes the previous set, and items not kept in this build are dropped.</summary>
    /// <exception cref="InvalidOperationException">No build is in progress.</exception>
    internal void End()
    {
        this.RequireBuilding();
        this.sideAIsPrevious = !this.sideAIsPrevious;
        this.CurrentSet().Clear();
        this.building = false;
    }

    private Dictionary<TItem, TSnapshot> PreviousSet() => this.sideAIsPrevious ? this.sideA : this.sideB;

    private Dictionary<TItem, TSnapshot> CurrentSet() => this.sideAIsPrevious ? this.sideB : this.sideA;

    private void RequireBuilding()
    {
        if (!this.building)
        {
            throw new InvalidOperationException("No build is in progress.");
        }
    }

    private sealed class ReferenceComparer : IEqualityComparer<TItem>
    {
        internal static readonly ReferenceComparer Instance = new ReferenceComparer();

        public bool Equals(TItem? x, TItem? y) => ReferenceEquals(x, y);

        public int GetHashCode(TItem obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
