namespace BaseStorage.Core.Tests.Reservations;

using System;
using BaseStorage.Core.Reservations;

/// <summary>AC-1 (R8): argument violations throw the exact contracted exception with its ParamName; game-state conditions do not.</summary>
public sealed class ReservationValidationTests
{
    // GW-U-156 (AC-1, 8.9): the reservation constructor rejects each bad argument with its exact exception and ParamName.
    [Fact]
    public void ReservationConstructorRejectsBadArgumentsWithTheContractedParamName()
    {
        var clock = ReservationFixtures.NewClock();

        ArgumentException chest = Assert.Throws<ArgumentException>(() =>
            new Reservation<OrganizeRun>(default, ReservationFixtures.Organize(), clock, ReservationFixtures.TimeoutMs));
        Assert.Equal("chest", chest.ParamName);

        ArgumentNullException operation = Assert.Throws<ArgumentNullException>(() =>
            new Reservation<OrganizeRun>(ReservationFixtures.ChestA, null!, clock, ReservationFixtures.TimeoutMs));
        Assert.Equal("operation", operation.ParamName);

        ArgumentNullException nullClock = Assert.Throws<ArgumentNullException>(() =>
            new Reservation<OrganizeRun>(ReservationFixtures.ChestA, ReservationFixtures.Organize(), null!, ReservationFixtures.TimeoutMs));
        Assert.Equal("clock", nullClock.ParamName);

        ArgumentOutOfRangeException timeout = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Reservation<OrganizeRun>(ReservationFixtures.ChestA, ReservationFixtures.Organize(), clock, 0));
        Assert.Equal("reserveTimeoutMs", timeout.ParamName);
    }

    // GW-U-156 (AC-1, order): the chest is reported first when every argument is bad.
    [Fact]
    public void ReservationConstructorReportsChestFirst()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new Reservation<OrganizeRun>(default, null!, null!, 0));

        Assert.Equal("chest", ex.ParamName);
    }

    // GW-U-156 (AC-1, 8.9): the registry constructor rejects a null clock and a timeout below 1, and accepts 1.
    [Fact]
    public void RegistryConstructorValidatesClockAndTimeout()
    {
        var clock = ReservationFixtures.NewClock();

        ArgumentNullException nullClock = Assert.Throws<ArgumentNullException>(() =>
            new ChestReservations<OrganizeRun>(null!, ReservationFixtures.TimeoutMs));
        Assert.Equal("clock", nullClock.ParamName);

        ArgumentOutOfRangeException negative = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ChestReservations<OrganizeRun>(clock, -1));
        Assert.Equal("reserveTimeoutMs", negative.ParamName);

        ArgumentOutOfRangeException zero = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ChestReservations<OrganizeRun>(clock, 0));
        Assert.Equal("reserveTimeoutMs", zero.ParamName);

        ArgumentOutOfRangeException minimum = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ChestReservations<OrganizeRun>(clock, int.MinValue));
        Assert.Equal("reserveTimeoutMs", minimum.ParamName);

        var accepted = new ChestReservations<OrganizeRun>(clock, 1);
        Assert.NotNull(accepted);
    }

    // GW-U-156 (AC-1, 8.9): Begin rejects a none chest and a null operation with the contracted ParamName.
    [Fact]
    public void BeginRejectsBadArgumentsWithTheContractedParamName()
    {
        var book = new ChestReservations<OrganizeRun>(ReservationFixtures.NewClock(), ReservationFixtures.TimeoutMs);

        ArgumentException chest = Assert.Throws<ArgumentException>(() => book.Begin(default, ReservationFixtures.Organize()));
        Assert.Equal("chest", chest.ParamName);

        ArgumentNullException operation = Assert.Throws<ArgumentNullException>(() => book.Begin(ReservationFixtures.ChestA, null!));
        Assert.Equal("operation", operation.ParamName);
    }

    // GW-U-156 (AC-1): a failed constructor or Begin reads the clock 0 times and stores nothing.
    [Fact]
    public void FailedConstructionAndBeginReadNoClockAndStoreNothing()
    {
        var clock = ReservationFixtures.NewClock();
        var book = new ChestReservations<OrganizeRun>(clock, ReservationFixtures.TimeoutMs);
        Assert.Equal(0, clock.Reads);

        Assert.Throws<ArgumentException>(() => new Reservation<OrganizeRun>(default, ReservationFixtures.Organize(), clock, ReservationFixtures.TimeoutMs));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Reservation<OrganizeRun>(ReservationFixtures.ChestA, ReservationFixtures.Organize(), clock, 0));
        Assert.Equal(0, clock.Reads);

        Assert.Throws<ArgumentException>(() => book.Begin(default, ReservationFixtures.Organize()));
        Assert.Throws<ArgumentNullException>(() => book.Begin(ReservationFixtures.ChestA, null!));

        Assert.Equal(0, clock.Reads);
        Assert.Null(book.Find(ReservationFixtures.ChestA));
        Assert.Empty(book.Active);
    }

    // GW-U-156 (AC-1, R8): game-state conditions never throw. Find and Apply on the none chest return null and false.
    [Fact]
    public void NoneChestFindsNothingAndAppliesNothingWithoutThrowing()
    {
        var book = new ChestReservations<OrganizeRun>(ReservationFixtures.NewClock(), ReservationFixtures.TimeoutMs);

        Assert.Null(book.Find(default));
        Assert.False(book.Apply(default, ReservationEvent.Grant));
    }
}
