namespace BaseStorage.Core.Restock;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaseStorage.Core.Snapshots;

/// <summary>The result of <see cref="RestockPlanner.Plan"/> (S-QR-001, contract 4.2). Result object with no public constructor.</summary>
public sealed class RestockPlan
{
    private readonly ReadOnlyCollection<RestockMove> moves;
    private readonly ReadOnlyCollection<ItemStackSnapshot> carriedAfter;
    private readonly int movedItems;
    private readonly int toppedUpStacks;
    private readonly int remainingRoom;
    private readonly int leftInUse;
    private readonly int leftDifferentIdentity;

    internal RestockPlan(
        RestockMove[] moves,
        ItemStackSnapshot[] carriedAfter,
        int movedItems,
        int toppedUpStacks,
        int remainingRoom,
        int leftInUse,
        int leftDifferentIdentity)
    {
        this.moves = new ReadOnlyCollection<RestockMove>(moves);
        this.carriedAfter = new ReadOnlyCollection<ItemStackSnapshot>(carriedAfter);
        this.movedItems = movedItems;
        this.toppedUpStacks = toppedUpStacks;
        this.remainingRoom = remainingRoom;
        this.leftInUse = leftInUse;
        this.leftDifferentIdentity = leftDifferentIdentity;
    }

    /// <summary>Gets the moves in execution order. Read-only.</summary>
    public IReadOnlyList<RestockMove> Moves => moves;

    /// <summary>Gets the carried stacks after the moves, in the same length and order as the input carried list. Read-only.</summary>
    public IReadOnlyList<ItemStackSnapshot> CarriedAfter => carriedAfter;

    /// <summary>Gets the sum of the moved item counts.</summary>
    public int MovedItems => movedItems;

    /// <summary>Gets the number of distinct target indexes in the moves.</summary>
    public int ToppedUpStacks => toppedUpStacks;

    /// <summary>Gets the sum over the targets of the max stack minus the count after the moves.</summary>
    public int RemainingRoom => remainingRoom;

    /// <summary>Gets the wanted items left in in-use chests.</summary>
    public int LeftInUse => leftInUse;

    /// <summary>Gets the wanted items left because only a different identity was available.</summary>
    public int LeftDifferentIdentity => leftDifferentIdentity;

    /// <summary>Gets the sum of <see cref="LeftInUse"/> and <see cref="LeftDifferentIdentity"/>.</summary>
    public int LeftBehind => (int)Math.Min((long)leftInUse + leftDifferentIdentity, int.MaxValue);
}
