namespace BaseStorage.Core.Tests.Reservations;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BaseStorage.Core.Reservations;

/// <summary>AC-1: the 19-row table (contract 3.1 and 3.4) and the 6 x 9 matrix (3.2), checked against literal copies of the contract.</summary>
public sealed class ReservationTransitionTableTests
{
    // GW-U-151 (AC-1): each row of All equals the contract row at the same index.
    [Theory]
    [MemberData(nameof(RowIndices))]
    public void AllRowMatchesTheContractRowAtTheSameIndex(int index)
    {
        ReservationTransition actual = ReservationTransitions.All[index];
        TableRow expected = ReservationFixtures.TableRows[index];

        Assert.Equal(Describe(index, expected), Describe(index, actual));
    }

    // GW-U-151 (AC-1, 3.4): All has 19 rows, is the same instance on every read, and is a ReadOnlyCollection.
    [Fact]
    public void AllHasNineteenRowsAndIsOneReadOnlyCollection()
    {
        IReadOnlyList<ReservationTransition> first = ReservationTransitions.All;
        IReadOnlyList<ReservationTransition> second = ReservationTransitions.All;

        Assert.Equal(19, first.Count);
        Assert.Same(first, second);
        Assert.IsType<ReadOnlyCollection<ReservationTransition>>(first);
    }

    // GW-U-151 (AC-1, R4): the list cannot be changed through the IList cast.
    [Fact]
    public void AllCannotBeChanged()
    {
        var list = (IList<ReservationTransition>)ReservationTransitions.All;

        Assert.Throws<NotSupportedException>(() => list.Add(default));
        Assert.Throws<NotSupportedException>(() => list.Clear());
        Assert.Throws<NotSupportedException>(() => list[0] = default);
    }

    // GW-U-152 (AC-1, 3.2): TryGet agrees with the contract matrix for every one of the 54 defined pairs.
    [Theory]
    [MemberData(nameof(AllPairs))]
    public void TryGetAgreesWithTheContractMatrixCellByCell(ReservationState from, ReservationEvent trigger)
    {
        bool legal = ReservationFixtures.TryRow(from, trigger, out TableRow row);

        bool found = ReservationTransitions.TryGet(from, trigger, out ReservationTransition actual);

        Assert.Equal(legal, found);
        if (legal)
        {
            Assert.Equal(Describe(row), Describe(actual));
        }
        else
        {
            Assert.Equal(default(ReservationTransition), actual);
        }
    }

    // GW-U-152 (AC-1, 3.2): exactly 19 of the 54 defined pairs are legal and 35 are illegal.
    [Fact]
    public void ExactlyNineteenOfTheFiftyFourPairsAreLegal()
    {
        int legal = 0;
        int illegal = 0;
        foreach (object[] pair in ReservationFixtures.AllPairs())
        {
            if (ReservationTransitions.TryGet((ReservationState)pair[0], (ReservationEvent)pair[1], out _))
            {
                legal++;
            }
            else
            {
                illegal++;
            }
        }

        Assert.Equal(19, legal);
        Assert.Equal(35, illegal);
    }

    // GW-U-152 (AC-1): undefined inputs give false with default output and no throw.
    [Fact]
    public void UndefinedInputsGiveFalseWithoutThrowing()
    {
        Assert.False(ReservationTransitions.TryGet((ReservationState)9, ReservationEvent.Grant, out ReservationTransition nine));
        Assert.Equal(default(ReservationTransition), nine);

        Assert.False(ReservationTransitions.TryGet((ReservationState)(-1), ReservationEvent.Grant, out ReservationTransition minusOne));
        Assert.Equal(default(ReservationTransition), minusOne);

        Assert.False(ReservationTransitions.TryGet(ReservationState.Requesting, (ReservationEvent)42, out ReservationTransition bogus));
        Assert.Equal(default(ReservationTransition), bogus);
    }

    // GW-U-152 (AC-1, 3.4): IsTerminal is true only for Completed and Failed, and false for undefined values.
    [Fact]
    public void IsTerminalIsTrueOnlyForCompletedAndFailed()
    {
        foreach (ReservationState state in ReservationFixtures.DefinedStates())
        {
            bool expected = state is ReservationState.Completed or ReservationState.Failed;
            Assert.Equal(expected, ReservationTransitions.IsTerminal(state));
        }

        Assert.False(ReservationTransitions.IsTerminal((ReservationState)9));
    }

    // GW-U-152 (AC-1, 3.4): HoldsChest is true only for Granted, Writing and Releasing, and false for undefined values.
    [Fact]
    public void HoldsChestIsTrueOnlyForGrantedWritingAndReleasing()
    {
        foreach (ReservationState state in ReservationFixtures.DefinedStates())
        {
            bool expected = state is ReservationState.Granted or ReservationState.Writing or ReservationState.Releasing;
            Assert.Equal(expected, ReservationTransitions.HoldsChest(state));
        }

        Assert.False(ReservationTransitions.HoldsChest((ReservationState)9));
    }

    /// <summary>Row indices 0 to 18 as theory data.</summary>
    public static IEnumerable<object[]> RowIndices() => Enumerable.Range(0, 19).Select(i => new object[] { i });

    /// <summary>The 54 defined (state, event) pairs as theory data.</summary>
    public static IEnumerable<object[]> AllPairs() => ReservationFixtures.AllPairs();

    private static string Describe(int index, TableRow row) => $"row {index + 1}: {Describe(row)}";

    private static string Describe(TableRow row) => $"{row.From} {row.Trigger} -> {row.To} ({row.Failure})";

    private static string Describe(ReservationTransition row) => $"{row.From} {row.Trigger} -> {row.To} ({row.Failure})";

    private static string Describe(int index, ReservationTransition row) => $"row {index + 1}: {Describe(row)}";
}
