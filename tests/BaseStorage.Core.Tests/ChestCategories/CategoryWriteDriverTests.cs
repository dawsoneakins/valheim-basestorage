namespace BaseStorage.Core.Tests.ChestCategories;

using BaseStorage.Core.ChestCategories;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.ChestAccess;
using BaseStorage.Plugin.ChestCategories;
using static BaseStorage.Core.Tests.ChestCategories.ChestCategoryFixtures;

/// <summary>
/// GW-U-1220..1229 (AC-3, AC-4): the write driver over the S-E005-002 reservation driver and a recording storage (contract 3.2, 6.2-6.6):
/// the local-owner fast path, Auto as the empty string, unchanged values, the network grant, deny, in use, timeout, loss, and the idle tick.
/// </summary>
public sealed class CategoryWriteDriverTests
{
    private static readonly ContainerId Chest42 = ChestAccessFixtures.Chest42;
    private static readonly CategoryAssignment Wood = Cat("Wood");

    // GW-U-1220: a chest the local peer owns is written inside the call, reserved and released around the write.
    [Fact]
    public void LocalOwnerWritesInsideTheCall()
    {
        Setup s = Fresh();
        s.Chest.Owner = ChestAccessFixtures.Me;

        CategoryWriteStatus status = s.Driver.TrySet(s.Chest, Wood);

        Assert.Equal(CategoryWriteStatus.Applied, status);
        Assert.Equal(new[] { "3141592653:42=c:Wood" }, s.Storage.Writes);
        Assert.Equal("c:Wood", s.Storage.Stored(Chest42));
        Assert.Equal(new[] { new CategoryWriteResult(Chest42, Wood, CategoryWriteOutcome.Applied, null) }, s.Results);
        Assert.Equal(ReservationState.Completed, s.Reservations.Find(Chest42)!.State);
        Assert.Equal(new[] { "SetInUse 3141592653:42 true", "SetInUse 3141592653:42 false" }, s.Game.Calls);
        Assert.Equal(0, s.Driver.PendingCount);
        Assert.False(s.Driver.IsPending(Chest42));
        Assert.Equal(Wood, s.Driver.Read(s.Chest));
    }

    // GW-U-1220: the single-player and host paths also complete inside the call, and Keep is stored as k.
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public void HostAndSinglePlayerPathsWriteInsideTheCall(bool isServer, bool ownerless)
    {
        Setup s = Fresh(isServer);
        s.Chest.Owner = ownerless ? 0L : ChestAccessFixtures.Me;

        Assert.Equal(CategoryWriteStatus.Applied, s.Driver.TrySet(s.Chest, CategoryAssignment.Keep));

        Assert.Equal("k", s.Storage.Stored(Chest42));
        Assert.Equal(ChestAccessFixtures.Me, s.Chest.Owner);
        Assert.DoesNotContain(s.Game.Calls, call => call.StartsWith("SendOpenRequest", StringComparison.Ordinal));
    }

    // GW-U-1221: Auto is written as the empty string and reads back as Auto.
    [Fact]
    public void AutoIsWrittenAsTheEmptyString()
    {
        Setup s = Fresh();
        s.Chest.Owner = ChestAccessFixtures.Me;
        s.Storage.Seed(Chest42, "c:Wood");

        Assert.Equal(CategoryWriteStatus.Applied, s.Driver.TrySet(s.Chest, null));

        Assert.Equal(string.Empty, s.Storage.Stored(Chest42));
        Assert.Equal(new[] { "3141592653:42=" }, s.Storage.Writes);
        Assert.Null(s.Driver.Read(s.Chest));
        Assert.Equal(new CategoryWriteResult(Chest42, null, CategoryWriteOutcome.Applied, null), s.Results.Single());
    }

    // GW-U-1221: an unknown stored name is read as stored and is not touched until the player writes a new value.
    [Fact]
    public void AnUnknownNameIsPreservedUntilThePlayerWrites()
    {
        Setup s = Fresh();
        s.Chest.Owner = ChestAccessFixtures.Me;
        s.Storage.Seed(Chest42, "c:Gold");

        Assert.Equal(Cat("Gold"), s.Driver.Read(s.Chest));
        s.Driver.Tick();
        Assert.Empty(s.Storage.Writes);
        Assert.Equal("c:Gold", s.Storage.Stored(Chest42));

        Assert.Equal(CategoryWriteStatus.Applied, s.Driver.TrySet(s.Chest, null));
        Assert.Equal(string.Empty, s.Storage.Stored(Chest42));
    }

    // GW-U-1222: a value the chest already holds is not reserved or written; Auto matches no key and the empty string.
    [Fact]
    public void AnUnchangedValueReservesNothing()
    {
        Setup s = Fresh();
        s.Chest.Owner = ChestAccessFixtures.Me;
        s.Storage.Seed(Chest42, "c:Wood");

        Assert.Equal(CategoryWriteStatus.Unchanged, s.Driver.TrySet(s.Chest, Wood));
        Assert.Null(s.Reservations.Find(Chest42));
        Assert.Empty(s.Storage.Writes);
        Assert.Empty(s.Results);
        Assert.Empty(s.Game.Calls);

        FakeChest fresh = s.Game.AddChest(new ContainerId(3141592653L, 43u));
        fresh.Owner = ChestAccessFixtures.Me;
        Assert.Equal(CategoryWriteStatus.Unchanged, s.Driver.TrySet(fresh, null));
        s.Storage.Seed(fresh.Id, string.Empty);
        Assert.Equal(CategoryWriteStatus.Unchanged, s.Driver.TrySet(fresh, null));
        Assert.Empty(s.Results);
    }

    // GW-U-1223: a chest owned by another peer is requested, stays pending, and is written on the frame after the grant arrives.
    [Fact]
    public void NetworkGrantCompletesOnALaterFrame()
    {
        Setup s = Fresh();

        Assert.Equal(CategoryWriteStatus.Pending, s.Driver.TrySet(s.Chest, Wood));
        Assert.True(s.Driver.IsPending(Chest42));
        Assert.Equal(1, s.Driver.PendingCount);
        Assert.Equal(new[] { "SendOpenRequest 3141592653:42" }, s.Game.Calls);
        Assert.Empty(s.Storage.Writes);
        Assert.Empty(s.Results);

        s.Clock.Advance(TimeSpan.FromMilliseconds(100));
        s.Reservations.Tick();
        s.Driver.Tick();
        Assert.True(s.Driver.IsPending(Chest42));
        Assert.Empty(s.Storage.Writes);

        Assert.False(s.Reservations.OnOpenResponse(s.Chest, true));
        s.Chest.Owner = ChestAccessFixtures.Me;
        s.Clock.Advance(TimeSpan.FromMilliseconds(100));
        s.Reservations.Tick();
        Assert.Equal(ReservationState.Granted, s.Reservations.Find(Chest42)!.State);
        s.Driver.Tick();

        Assert.Equal(new[] { "3141592653:42=c:Wood" }, s.Storage.Writes);
        Assert.Equal(new CategoryWriteResult(Chest42, Wood, CategoryWriteOutcome.Applied, null), s.Results.Single());
        Assert.Equal(ReservationState.Completed, s.Reservations.Find(Chest42)!.State);
        Assert.Equal(new[] { "SendOpenRequest 3141592653:42", "SetInUse 3141592653:42 true", "SetInUse 3141592653:42 false" }, s.Game.Calls);
        Assert.Equal(0, s.Driver.PendingCount);
    }

    // GW-U-1223: a second request for a pending chest does not send again.
    [Fact]
    public void ASecondRequestWhilePendingSendsNothing()
    {
        Setup s = Fresh();
        Assert.Equal(CategoryWriteStatus.Pending, s.Driver.TrySet(s.Chest, Wood));

        Assert.Equal(CategoryWriteStatus.Pending, s.Driver.TrySet(s.Chest, CategoryAssignment.Keep));

        Assert.Single(s.Game.Calls);
        Assert.Equal(1, s.Driver.PendingCount);
        Assert.Empty(s.Results);
    }

    // GW-U-1224: a refusal by the owner leaves the value unchanged and reports Denied.
    [Fact]
    public void OwnerDenyLeavesTheValueAndReportsDenied()
    {
        Setup s = Fresh();
        s.Storage.Seed(Chest42, "k");
        Assert.Equal(CategoryWriteStatus.Pending, s.Driver.TrySet(s.Chest, Wood));

        Assert.False(s.Reservations.OnOpenResponse(s.Chest, false));
        s.Driver.Tick();

        Assert.Equal("k", s.Storage.Stored(Chest42));
        Assert.Empty(s.Storage.Writes);
        Assert.Equal(new CategoryWriteResult(Chest42, Wood, CategoryWriteOutcome.Denied, null), s.Results.Single());
        Assert.Equal(0, s.Driver.PendingCount);
    }

    // GW-U-1224: vanilla access denied is refused inside the call with Denied.
    [Fact]
    public void AccessDeniedIsRefusedInsideTheCall()
    {
        Setup s = Fresh();
        s.Chest.AccessAllowed = false;

        Assert.Equal(CategoryWriteStatus.Refused, s.Driver.TrySet(s.Chest, Wood));

        Assert.Equal(CategoryWriteOutcome.Denied, s.Results.Single().Outcome);
        Assert.Empty(s.Storage.Writes);
        Assert.Empty(s.Game.Calls);
        Assert.Equal(ReservationState.Failed, s.Reservations.Find(Chest42)!.State);
    }

    // GW-U-1225: a chest another player has open is refused with that player's label; an unusable name gives the fallback label.
    [Theory]
    [InlineData("Erik", "Erik")]
    [InlineData("", "another player")]
    [InlineData(null, "another player")]
    public void InUseIsRefusedWithTheHolderLabel(string? ownerName, string expectedLabel)
    {
        Setup s = Fresh();
        s.Chest.InUseFlag = true;
        s.Chest.OwnerName = ownerName;

        Assert.Equal(CategoryWriteStatus.Refused, s.Driver.TrySet(s.Chest, Wood));

        Assert.Equal(new CategoryWriteResult(Chest42, Wood, CategoryWriteOutcome.InUse, expectedLabel), s.Results.Single());
        Assert.Empty(s.Storage.Writes);
        Assert.Empty(s.Game.Calls);
    }

    // GW-U-1226: no answer within the reservation timeout reports Timeout and writes nothing.
    [Fact]
    public void ReservationTimeoutReportsTimeout()
    {
        Setup s = Fresh();
        Assert.Equal(CategoryWriteStatus.Pending, s.Driver.TrySet(s.Chest, Wood));

        s.Clock.Advance(TimeSpan.FromMilliseconds(1499));
        s.Reservations.Tick();
        s.Driver.Tick();
        Assert.True(s.Driver.IsPending(Chest42));

        s.Clock.Advance(TimeSpan.FromMilliseconds(1));
        s.Reservations.Tick();
        s.Driver.Tick();

        Assert.Equal(new CategoryWriteResult(Chest42, Wood, CategoryWriteOutcome.Timeout, null), s.Results.Single());
        Assert.Empty(s.Storage.Writes);
        Assert.Equal(0, s.Driver.PendingCount);
    }

    // GW-U-1226: the driver's own deadline (twice the timeout) cancels a request the reservation driver never advanced.
    [Fact]
    public void TheDeadlineCancelsAStuckRequest()
    {
        Setup s = Fresh();
        Assert.Equal(CategoryWriteStatus.Pending, s.Driver.TrySet(s.Chest, Wood));

        s.Clock.Advance(TimeSpan.FromMilliseconds(2999));
        s.Driver.Tick();
        Assert.True(s.Driver.IsPending(Chest42));

        s.Clock.Advance(TimeSpan.FromMilliseconds(1));
        s.Driver.Tick();

        Assert.Equal(CategoryWriteOutcome.Timeout, s.Results.Single().Outcome);
        Assert.Equal(ReservationFailure.Cancelled, s.Reservations.Find(Chest42)!.Failure);
        Assert.Empty(s.Storage.Writes);
    }

    // GW-U-1227: a chest that goes away while requesting reports Unavailable; a chest with no view is refused without a result.
    [Fact]
    public void LostChestReportsUnavailable()
    {
        Setup s = Fresh();
        Assert.Equal(CategoryWriteStatus.Pending, s.Driver.TrySet(s.Chest, Wood));

        s.Chest.ViewValid = false;
        s.Reservations.Tick();
        s.Driver.Tick();

        Assert.Equal(CategoryWriteOutcome.Unavailable, s.Results.Single().Outcome);
        Assert.Empty(s.Storage.Writes);

        Assert.Equal(CategoryWriteStatus.Refused, s.Driver.TrySet(s.Chest, Wood));
        Assert.Single(s.Results);
    }

    // GW-U-1227: a chest reserved by another operation is refused with Unavailable and that reservation is untouched.
    [Fact]
    public void AChestReservedByAnotherOperationIsRefused()
    {
        Setup s = Fresh();
        s.Chest.Owner = ChestAccessFixtures.Me;
        s.Reservations.Begin(s.Chest, "organize");

        Assert.Equal(CategoryWriteStatus.Refused, s.Driver.TrySet(s.Chest, Wood));

        Assert.Equal(CategoryWriteOutcome.Unavailable, s.Results.Single().Outcome);
        Assert.Empty(s.Storage.Writes);
        Assert.Equal(ReservationState.Granted, s.Reservations.Find(Chest42)!.State);
        Assert.Equal("organize", s.Reservations.Find(Chest42)!.Operation);
    }

    // GW-U-1228: a write the storage refuses (view gone between the gate and the write) reports Unavailable and still releases.
    [Fact]
    public void AFailedStorageWriteReportsUnavailableAndReleases()
    {
        Setup s = Fresh();
        s.Chest.Owner = ChestAccessFixtures.Me;
        s.Storage.FailWrites = true;

        Assert.Equal(CategoryWriteStatus.Refused, s.Driver.TrySet(s.Chest, Wood));

        Assert.Equal(CategoryWriteOutcome.Unavailable, s.Results.Single().Outcome);
        Assert.Null(s.Storage.Stored(Chest42));
        Assert.Equal(ReservationState.Completed, s.Reservations.Find(Chest42)!.State);
        Assert.Equal(new[] { "SetInUse 3141592653:42 true", "SetInUse 3141592653:42 false" }, s.Game.Calls);
    }

    // GW-U-1228: the local UI opening the chest while granted fails the write without writing.
    [Fact]
    public void TheLocalUiTakingTheChestFailsTheWrite()
    {
        Setup s = Fresh();
        Assert.Equal(CategoryWriteStatus.Pending, s.Driver.TrySet(s.Chest, Wood));
        Assert.False(s.Reservations.OnOpenResponse(s.Chest, true));
        s.Chest.Owner = ChestAccessFixtures.Me;
        s.Reservations.Tick();
        Assert.Equal(ReservationState.Granted, s.Reservations.Find(Chest42)!.State);

        s.Chest.OpenLocally = true;
        s.Driver.Tick();

        Assert.Equal(CategoryWriteOutcome.Unavailable, s.Results.Single().Outcome);
        Assert.Empty(s.Storage.Writes);
        Assert.True(s.Reservations.Find(Chest42)!.IsTerminal);
    }

    // GW-U-1229: an idle tick reads nothing and touches nothing; null arguments throw by parameter name.
    [Fact]
    public void IdleTickIsFreeAndNullArgumentsThrow()
    {
        Setup s = Fresh();
        int reads = s.Clock.Reads;
        int accesses = s.Game.Accesses;

        s.Driver.Tick();
        s.Driver.Tick();

        Assert.Equal(reads, s.Clock.Reads);
        Assert.Equal(accesses, s.Game.Accesses);
        Assert.Equal(0, s.Storage.Reads);

        Assert.Equal("chest", Assert.Throws<ArgumentNullException>(() => s.Driver.TrySet(null!, Wood)).ParamName);
        Assert.Equal("chest", Assert.Throws<ArgumentNullException>(() => s.Driver.Read(null!)).ParamName);
        Assert.Equal("reservations", Assert.Throws<ArgumentNullException>(() => new ChestCategoryWriteDriver<FakeChest>(null!, s.Storage, s.Clock, 1500, null)).ParamName);
        Assert.Equal("storage", Assert.Throws<ArgumentNullException>(() => new ChestCategoryWriteDriver<FakeChest>(s.Reservations, null!, s.Clock, 1500, null)).ParamName);
        Assert.Equal("clock", Assert.Throws<ArgumentNullException>(() => new ChestCategoryWriteDriver<FakeChest>(s.Reservations, s.Storage, null!, 1500, null)).ParamName);
        Assert.Equal("category", ChestCategoryWriteDriver<FakeChest>.Operation);
    }

    // GW-U-1229: without a callback the driver still writes, and two chests pend and complete independently.
    [Fact]
    public void TwoChestsPendIndependentlyAndTheCallbackIsOptional()
    {
        Setup s = Fresh();
        var driver = new ChestCategoryWriteDriver<FakeChest>(s.Reservations, s.Storage, s.Clock, 1500, null);
        FakeChest other = s.Game.AddChest(new ContainerId(2718281828L, 7u));

        Assert.Equal(CategoryWriteStatus.Pending, driver.TrySet(s.Chest, Wood));
        Assert.Equal(CategoryWriteStatus.Pending, driver.TrySet(other, CategoryAssignment.Keep));
        Assert.Equal(2, driver.PendingCount);

        Assert.False(s.Reservations.OnOpenResponse(other, true));
        other.Owner = ChestAccessFixtures.Me;
        s.Reservations.Tick();
        driver.Tick();

        Assert.Equal(1, driver.PendingCount);
        Assert.True(driver.IsPending(Chest42));
        Assert.Equal("k", s.Storage.Stored(other.Id));
        Assert.Null(s.Storage.Stored(Chest42));
    }
}
