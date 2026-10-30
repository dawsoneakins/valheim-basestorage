namespace BaseStorage.Core.Tests.ChestAccess;

using System.Globalization;
using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.Reservations;

/// <summary>GW-U-758 (AC-1): the Debug line formats M1-M6 of contract 5, verbatim.</summary>
public sealed class ReservationMessagesTests
{
    private static readonly ContainerId Chest42 = new ContainerId(3141592653L, 42u);

    // GW-U-758
    [Theory]
    [InlineData(1, "Reserve 3141592653:42 [probe]: route SendRequest")]
    [InlineData(2, "Reserve 3141592653:42 [probe]: Requesting -> Granted")]
    [InlineData(3, "Reserve 3141592653:42 [organize]: Requesting -> Failed (Timeout)")]
    [InlineData(4, "Reserve 3141592653:42 [probe]: Writing -> Failed (OwnerLost)")]
    [InlineData(5, "Reserve 3141592653:42 [probe]: in use by Erik")]
    [InlineData(6, "Reserve 3141592653:42 [probe]: open response granted consumed, chest UI not shown")]
    [InlineData(7, "Reserve 3141592653:42 [probe]: open response refused consumed, chest UI not shown")]
    [InlineData(8, "Reserve 2718281828:7 [late]: open response granted consumed, chest UI not shown")]
    [InlineData(9, "Reserve 3141592653:42 [probe]: already reserved")]
    [InlineData(10, "Reserve 3141592653:42 [probe]: route 42")]
    public void ExampleRowsRenderVerbatim(int row, string expected)
    {
        Assert.Equal(expected, Render(row));
    }

    // GW-U-758
    [Fact]
    public void NullOperationThrowsByParamName()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => ReservationMessages.AlreadyReserved(Chest42, null!));

        Assert.Equal("operation", ex.ParamName);
    }

    // GW-U-758
    [Fact]
    public void NullHolderLabelThrowsByParamName()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => ReservationMessages.InUse(Chest42, "probe", null!));

        Assert.Equal("holderLabel", ex.ParamName);
    }

    // GW-U-758: the id and the enum names are rendered invariantly, under every culture.
    [Theory]
    [InlineData("ar-SA")]
    [InlineData("tr-TR")]
    public void InvariantRenderingHoldsUnderTheCulture(string culture)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            for (int row = 1; row <= 10; row++)
            {
                Assert.Equal(Render(row, invariant: true), Render(row));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    private static string Render(int row, bool invariant = false) => row switch
    {
        1 => ReservationMessages.Route(Chest42, "probe", ReservationRoute.SendRequest),
        2 => ReservationMessages.Transition(Chest42, "probe", ReservationState.Requesting, ReservationState.Granted, ReservationFailure.None),
        3 => ReservationMessages.Transition(Chest42, "organize", ReservationState.Requesting, ReservationState.Failed, ReservationFailure.Timeout),
        4 => ReservationMessages.Transition(Chest42, "probe", ReservationState.Writing, ReservationState.Failed, ReservationFailure.OwnerLost),
        5 => ReservationMessages.InUse(Chest42, "probe", "Erik"),
        6 => ReservationMessages.ResponseConsumed(Chest42, "probe", true),
        7 => ReservationMessages.ResponseConsumed(Chest42, "probe", false),
        8 => ReservationMessages.LateResponseConsumed(new ContainerId(2718281828L, 7u), true),
        9 => ReservationMessages.AlreadyReserved(Chest42, "probe"),
        10 => ReservationMessages.Route(Chest42, "probe", (ReservationRoute)42),
        _ => throw new ArgumentOutOfRangeException(nameof(row), invariant ? "invariant" : "row"),
    };
}
