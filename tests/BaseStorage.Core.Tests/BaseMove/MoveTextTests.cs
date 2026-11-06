namespace BaseStorage.Core.Tests.BaseMove;

using System;
using BaseStorage.Core.BaseMove;
using BaseStorage.Core.Ui;

/// <summary>The lines a player reads: the preview, the final line and each refusal.</summary>
public sealed class MoveTextTests
{
    private static readonly StringTable English = StringTable.English;

    [Fact]
    public void AMoveWithNothingLeftBehindIsOneShortLine()
    {
        var summary = new MoveSummary(214, 0, 0, 0, Array.Empty<string>());

        Assert.Equal("Stacks to move: 214.", MoveText.Preview(summary, English));
        Assert.Equal("Moved 214 stacks to Swamp Keep", MoveText.Done(summary, "Swamp Keep", English));
    }

    [Fact]
    public void WhatStaysIsNamedInThePreviewAndTheFinalLine()
    {
        var summary = new MoveSummary(214, 12, 3, 1, new[] { "CopperOre", "TinOre" });

        Assert.Equal(
            "Stacks to move: 214. Stacks staying because of portal rules: 12 (CopperOre, TinOre). Stacks with no room: 3. Chests skipped: 1.",
            MoveText.Preview(summary, English));
        Assert.Equal(
            "Moved 214 stacks to Swamp Keep; 12 stayed (portal rules): CopperOre, TinOre; 3 had no room; 1 chests skipped",
            MoveText.Done(summary, "Swamp Keep", English));
    }

    [Fact]
    public void ALongListOfItemsIsCutShort()
    {
        var summary = new MoveSummary(1, 4, 0, 0, new[] { "A", "B", "C", "D" });

        Assert.Contains("(A, B, C, ...)", MoveText.Preview(summary, English), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(MoveRefusal.Busy, "Move not started: another move is running")]
    [InlineData(MoveRefusal.NoSuchBase, "Move not started: base not found")]
    [InlineData(MoveRefusal.PlayerAtBase, "Move not started: Erik is at one of the bases")]
    [InlineData(MoveRefusal.BasesTooNear, "Move not started: that is this base")]
    [InlineData(MoveRefusal.Changed, "Move cancelled: chests changed, try again")]
    [InlineData(MoveRefusal.TimedOut, "Move cancelled: a chest is in use, try again")]
    [InlineData(MoveRefusal.Failed, "Move failed: nothing was moved")]
    [InlineData(MoveRefusal.None, "Move failed: nothing was moved")]
    public void EachRefusalHasItsOwnLine(MoveRefusal refusal, string expected) =>
        Assert.Equal(expected, MoveText.Refused(refusal, "Erik", English));
}
