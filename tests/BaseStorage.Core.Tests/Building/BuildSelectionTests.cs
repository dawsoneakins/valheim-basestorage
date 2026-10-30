namespace BaseStorage.Core.Tests.Building;

using System.Runtime.CompilerServices;
using BaseStorage.Plugin.Building;

/// <summary>GW-U-1600 (AC-6; contract 3.2): the selection identity of a placement attempt, by reference.</summary>
public sealed class BuildSelectionTests
{
    /// <summary>A piece double whose Equals override answers true for everything.</summary>
    private sealed class AlwaysEqual
    {
        public override bool Equals(object? obj) => true;

        public override int GetHashCode() => 0;
    }

    // GW-U-1600 (3.2 row 1): the same piece object gives equal selections with equal hashes.
    [Fact]
    public void TheSamePieceObjectGivesEqualSelectionsWithEqualHashes()
    {
        object piece = new object();

        Assert.True(new BuildSelection(piece) == new BuildSelection(piece));
        Assert.Equal(new BuildSelection(piece).GetHashCode(), new BuildSelection(piece).GetHashCode());
    }

    // GW-U-1600 (3.2 row 2): distinct piece objects give different selections.
    [Fact]
    public void DistinctPieceObjectsGiveDifferentSelections()
    {
        Assert.False(new BuildSelection(new object()) == new BuildSelection(new object()));
    }

    // GW-U-1600 (3.2 row 3): Equals through object is true for the same piece.
    [Fact]
    public void EqualsThroughObjectIsTrueForTheSamePiece()
    {
        object piece = new object();

        Assert.True(new BuildSelection(piece).Equals((object)new BuildSelection(piece)));
    }

    // GW-U-1600 (3.2 row 4): Equals through object is false when the argument is the piece itself, not a selection.
    [Fact]
    public void EqualsThroughObjectIsFalseForThePieceItself()
    {
        object piece = new object();

        Assert.False(new BuildSelection(piece).Equals((object)piece));
    }

    // GW-U-1600 (3.2 row 5): a null piece throws ArgumentNullException with ParamName "piece".
    [Fact]
    public void ANullPieceThrowsWithTheParameterName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new BuildSelection(null!));

        Assert.Equal("piece", ex.ParamName);
    }

    // GW-U-1600 (3.2): a piece whose Equals override always answers true does not make two selections equal (ReferenceEquals).
    [Fact]
    public void AnOverriddenEqualsOnThePieceDoesNotMakeSelectionsEqual()
    {
        Assert.False(new BuildSelection(new AlwaysEqual()) == new BuildSelection(new AlwaysEqual()));
    }

    // GW-U-1600 (3.2): the hash is RuntimeHelpers.GetHashCode of the piece.
    [Fact]
    public void TheHashIsTheRuntimeHashOfThePiece()
    {
        object piece = new AlwaysEqual();

        Assert.Equal(RuntimeHelpers.GetHashCode(piece), new BuildSelection(piece).GetHashCode());
    }
}
