namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.ChestAccess;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>GW-U-750 (AC-1): the route table of contract 2.5, rows 1-18, verbatim.</summary>
public sealed class ChestAccessRouteTests
{
    // GW-U-750
    [Theory]
    [InlineData(1, ReservationRoute.SendRequest, false)]
    [InlineData(2, ReservationRoute.LocalOwner, false)]
    [InlineData(3, ReservationRoute.LocalOwner, false)]
    [InlineData(4, ReservationRoute.SelfOpen, false)]
    [InlineData(5, ReservationRoute.SkipInUse, true)]
    [InlineData(6, ReservationRoute.SkipNoOwner, false)]
    [InlineData(7, ReservationRoute.HostClaim, false)]
    [InlineData(8, ReservationRoute.SkipNoOwner, false)]
    [InlineData(9, ReservationRoute.HostClaim, false)]
    [InlineData(10, ReservationRoute.SendRequest, false)]
    [InlineData(11, ReservationRoute.SkipInUse, true)]
    [InlineData(12, ReservationRoute.LocalOwner, false)]
    [InlineData(13, ReservationRoute.SkipAccessDenied, false)]
    [InlineData(14, ReservationRoute.SkipAccessDenied, false)]
    [InlineData(15, ReservationRoute.SkipAccessDenied, true)]
    [InlineData(16, ReservationRoute.SkipInvalid, false)]
    [InlineData(17, ReservationRoute.SkipInvalid, false)]
    [InlineData(18, ReservationRoute.HostClaim, false)]
    public void RouteAndInUseMatchTheContractRow(int row, ReservationRoute route, bool inUseByOther)
    {
        ChestObservation observation = RowObservation(row);

        Assert.Equal(route, ChestAccessRules.Route(observation));
        Assert.Equal(inUseByOther, ChestAccessRules.IsInUseByOther(observation));
    }

    // GW-U-750
    [Fact]
    public void RouteRejectsANullObservationByName()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => ChestAccessRules.Route(null!));

        Assert.Equal("observation", ex.ParamName);
    }

    // GW-U-750
    [Fact]
    public void IsInUseByOtherRejectsANullObservationByName()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => ChestAccessRules.IsInUseByOther(null!));

        Assert.Equal("observation", ex.ParamName);
    }

    private static ChestObservation RowObservation(int row) => row switch
    {
        1 => Obs(),
        2 => Obs(ownerSessionId: Me),
        3 => Obs(ownerSessionId: Me, inUseFlag: true),
        4 => Obs(openLocally: true, ownerSessionId: Me, inUseFlag: true),
        5 => Obs(inUseFlag: true),
        6 => Obs(inUseFlag: true, ownerListed: false, ownerConnected: false, ownerName: null),
        7 => Obs(inUseFlag: true, ownerListed: false, ownerConnected: false, ownerName: null, isServer: true),
        8 => Obs(ownerSessionId: 0, ownerListed: false, ownerConnected: false, ownerName: null),
        9 => Obs(ownerSessionId: 0, ownerListed: false, ownerConnected: false, ownerName: null, isServer: true),
        10 => Obs(isServer: true),
        11 => Obs(isServer: true, inUseFlag: true),
        12 => Obs(isServer: true, ownerSessionId: Me),
        13 => Obs(accessAllowed: false),
        14 => Obs(accessAllowed: false, openLocally: true),
        15 => Obs(accessAllowed: false, inUseFlag: true),
        16 => Obs(viewValid: false, inUseFlag: true),
        17 => Obs(localSessionId: 0),
        18 => Obs(ownerListed: true, ownerConnected: false, isServer: true),
        _ => throw new ArgumentOutOfRangeException(nameof(row)),
    };
}
