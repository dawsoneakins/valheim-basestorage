namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;

/// <summary>GW-U-751 (AC-1): the route-to-event and route-to-command flags of contract 2.2 and 2.4.</summary>
public sealed class ChestAccessEventTests
{
    // GW-U-751
    [Theory]
    [InlineData(ReservationRoute.SendRequest, null, true, false, true)]
    [InlineData(ReservationRoute.LocalOwner, ReservationEvent.Grant, false, false, true)]
    [InlineData(ReservationRoute.SelfOpen, ReservationEvent.Grant, false, false, false)]
    [InlineData(ReservationRoute.HostClaim, ReservationEvent.Grant, false, true, true)]
    [InlineData(ReservationRoute.SkipInvalid, ReservationEvent.OwnerLost, false, false, false)]
    [InlineData(ReservationRoute.SkipAccessDenied, ReservationEvent.Deny, false, false, false)]
    [InlineData(ReservationRoute.SkipInUse, ReservationEvent.InUseByOther, false, false, false)]
    [InlineData(ReservationRoute.SkipNoOwner, ReservationEvent.OwnerLost, false, false, false)]
    [InlineData((ReservationRoute)42, ReservationEvent.OwnerLost, false, false, false)]
    [InlineData((ReservationRoute)(-1), ReservationEvent.OwnerLost, false, false, false)]
    public void RouteFlagsAndEventMatchTheContract(ReservationRoute route, ReservationEvent? expectedEvent, bool sends, bool claims, bool setsInUse)
    {
        Assert.Equal(expectedEvent, ChestAccessRules.EventForRoute(route));
        Assert.Equal(sends, ChestAccessRules.SendsRequest(route));
        Assert.Equal(claims, ChestAccessRules.ClaimsOwnership(route));
        Assert.Equal(setsInUse, ChestAccessRules.SetsInUse(route));
    }

    // GW-U-751
    [Fact]
    public void ReservationRouteHasTheContractedNamesAndAnIntUnderlyingType()
    {
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(ReservationRoute)));
        Assert.Equal(
            new[]
            {
                "SendRequest", "LocalOwner", "SelfOpen", "HostClaim",
                "SkipInvalid", "SkipAccessDenied", "SkipInUse", "SkipNoOwner",
            },
            Enum.GetNames(typeof(ReservationRoute)));
        for (int value = 0; value < 8; value++)
        {
            Assert.Equal(value, (int)(ReservationRoute)value);
        }
    }
}
