namespace BaseStorage.Core.Tests.BaseMove;

using System;
using BaseStorage.Core.BaseMove;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;

/// <summary>The move's text messages, far ward checks, and the long reservation a move's holds rely on.</summary>
public sealed class MoveWireAndWardTests
{
    private static readonly WorldPosition Origin = new WorldPosition(0f, 30f, 0f);

    [Fact]
    public void EveryFieldSurvivesTheRoundTrip()
    {
        var message = new MoveMessage
        {
            Kind = MoveMessageKind.Done,
            RequestId = 12,
            Refusal = MoveRefusal.PlayerAtBase,
            PlayerName = "Erik; the:/,Red",
            SourceStation = new ContainerId(-5L, 7u),
            RequesterPlayerId = -99L,
            DestinationName = "Swamp; Keep/1,2:3",
            Summary = new MoveSummary(1, 2, 3, 4, new[] { "CopperOre", "Odd,name" }, new[] { new ContainerId(5L, 6u) }),
            Revisions = new[] { new MoveChestRevision(new ContainerId(-8L, 9u), 4000000000u) },
        };

        var back = MoveMessage.Decode(message.Encode());

        Assert.NotNull(back);
        Assert.Equal(MoveMessageKind.Done, back!.Kind);
        Assert.Equal(12, back.RequestId);
        Assert.Equal(MoveRefusal.PlayerAtBase, back.Refusal);
        Assert.Equal("Erik; the:/,Red", back.PlayerName);
        Assert.Equal(new ContainerId(-5L, 7u), back.SourceStation);
        Assert.Equal(-99L, back.RequesterPlayerId);
        Assert.Equal("Swamp; Keep/1,2:3", back.DestinationName);
        Assert.Equal(1, back.Summary!.MovedStacks);
        Assert.Equal(2, back.Summary.StayedStacks);
        Assert.Equal(3, back.Summary.NoRoomStacks);
        Assert.Equal(4, back.Summary.SkippedChests);
        Assert.Equal(new[] { "CopperOre", "Odd,name" }, back.Summary.StayedPrefabs);
        Assert.Equal(new ContainerId(5L, 6u), Assert.Single(back.Summary.ChangedChests));
        var revision = Assert.Single(back.Revisions);
        Assert.Equal(new ContainerId(-8L, 9u), revision.Id);
        Assert.Equal(4000000000u, revision.Revision);
    }

    [Fact]
    public void AnEmptyMessageSurvivesTheRoundTrip()
    {
        var back = MoveMessage.Decode(new MoveMessage { Kind = MoveMessageKind.Cancel, RequestId = -1 }.Encode());

        Assert.NotNull(back);
        Assert.Equal(MoveMessageKind.Cancel, back!.Kind);
        Assert.Null(back.Summary);
        Assert.True(back.SourceStation.IsNone);
        Assert.Empty(back.Revisions);
        Assert.Empty(back.Bases);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("BSV1;0;1;0;;;0;;;;")]
    [InlineData("BSM1;0;1;0;;;0;;;")]
    [InlineData("BSM1;99;1;0;;;0;;;;")]
    [InlineData("BSM1;0;1;99;;;0;;;;")]
    [InlineData("BSM1;x;1;0;;;0;;;;")]
    [InlineData("BSM1;0;1;0;;nope;0;;;;")]
    [InlineData("BSM1;0;1;0;;;0;;;1/2/3;")]
    [InlineData("BSM1;0;1;0;;;0;;;;1:2")]
    [InlineData("BSM1;0;1;0;;;0;;;;a:b:c")]
    public void AMalformedPayloadDecodesToNull(string? payload) => Assert.Null(MoveMessage.Decode(payload));

    [Fact]
    public void AnEnabledWardDeniesOutsidersInsideItsRadiusOnly()
    {
        var wards = new[] { new MoveWard(Origin, 32f, true, 1L, new[] { 2L }) };

        Assert.True(MoveWard.Denies(wards, new WorldPosition(10f, 500f, 10f), 3L));
        Assert.False(MoveWard.Denies(wards, new WorldPosition(10f, 30f, 10f), 1L));
        Assert.False(MoveWard.Denies(wards, new WorldPosition(10f, 30f, 10f), 2L));
        Assert.False(MoveWard.Denies(wards, new WorldPosition(32f, 30f, 0f), 3L));
        Assert.False(MoveWard.Denies(wards, new WorldPosition(100f, 30f, 0f), 3L));
    }

    [Fact]
    public void ADisabledWardDeniesNobody()
    {
        var wards = new[] { new MoveWard(Origin, 32f, false, 1L, Array.Empty<long>()) };

        Assert.False(MoveWard.Denies(wards, Origin, 3L));
    }

    [Fact]
    public void ALongReservationOutlivesTheUsualTimeoutAndStillEnds()
    {
        var clock = new BaseStorage.Core.Tests.Bases.ManualClock();
        var reservations = new ChestReservations<string>(clock, 1500);
        var id = new ContainerId(1L, 1u);
        var usual = new ContainerId(1L, 2u);
        var longOne = reservations.BeginWithTimeout(id, "move", 60000);
        var shortOne = reservations.Begin(usual, "organize");

        clock.Advance(TimeSpan.FromMilliseconds(1600));
        reservations.CheckTimeouts();

        Assert.False(longOne!.IsTerminal);
        Assert.True(shortOne!.IsTerminal);
        Assert.Null(reservations.BeginWithTimeout(id, "other", 60000));

        clock.Advance(TimeSpan.FromMinutes(2));
        reservations.CheckTimeouts();
        Assert.True(longOne.IsTerminal);
    }
}
