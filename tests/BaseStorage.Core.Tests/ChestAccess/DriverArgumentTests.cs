namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Reservations;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>GW-U-756 (AC-1): argument rules and the constructor rules of the driver (contract 3.3).</summary>
public sealed class DriverArgumentTests
{
    // GW-U-756
    [Fact]
    public void BeginRejectsANullChestByName()
    {
        var (_, _, _, _, driver) = Fresh();

        var ex = Assert.Throws<ArgumentNullException>(() => driver.Begin(null!, "organize"));

        Assert.Equal("chest", ex.ParamName);
    }

    // GW-U-756
    [Fact]
    public void BeginRejectsANullOperationByName()
    {
        var (_, chest, _, _, driver) = Fresh();

        var ex = Assert.Throws<ArgumentNullException>(() => driver.Begin(chest, null!));

        Assert.Equal("operation", ex.ParamName);
    }

    // GW-U-756
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void BeginRejectsAnEmptyOperationByName(string operation)
    {
        var (_, chest, _, _, driver) = Fresh();

        var ex = Assert.Throws<ArgumentException>(() => driver.Begin(chest, operation));

        Assert.Equal("operation", ex.ParamName);
    }

    // GW-U-756
    [Theory]
    [InlineData("game")]
    [InlineData("clock")]
    [InlineData("strings")]
    public void ConstructorRejectsANullArgumentByName(string name)
    {
        var (game, _, clock, sink, _) = Fresh();
        FakeReservationGame? nullGame = name == "game" ? null : game;
        ManualClock? nullClock = name == "clock" ? null : clock;
        StringTable? nullStrings = name == "strings" ? null : StringTable.English;

        var ex = Assert.Throws<ArgumentNullException>(() => new ReservationDriver<FakeChest>(nullGame!, nullClock!, 1500, nullStrings!, sink));

        Assert.Equal(name, ex.ParamName);
    }

    // GW-U-756
    [Fact]
    public void ConstructorAcceptsANullSinkAndReadsNeitherTheClockNorTheGame()
    {
        var (game, _, clock, _, _) = Fresh();

        var driver = new ReservationDriver<FakeChest>(game, clock, 1500, StringTable.English, sink: null);

        Assert.Equal(0, clock.Reads);
        Assert.Equal(0, game.Accesses);
        Assert.Equal(0, driver.ActiveCount);
    }

    // GW-U-756
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ConstructorNormalisesANonPositiveTimeoutToOne(int timeoutMs)
    {
        var (game, _, clock, sink, _) = Fresh();

        var driver = new ReservationDriver<FakeChest>(game, clock, timeoutMs, StringTable.English, sink);

        Assert.Equal(1, driver.ReserveTimeoutMs);
    }

    // GW-U-756
    [Fact]
    public void QueryInUseRejectsANullChestByName()
    {
        var (_, _, _, _, driver) = Fresh();

        var ex = Assert.Throws<ArgumentNullException>(() => driver.QueryInUse(null!));

        Assert.Equal("chest", ex.ParamName);
    }
}
