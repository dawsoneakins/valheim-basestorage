namespace BaseStorage.Core.Tests.ChestAccess;

using BaseStorage.Core.ChestAccess;
using static BaseStorage.Core.Tests.ChestAccess.ChestAccessFixtures;

/// <summary>GW-U-757 (AC-1): value semantics of ChestObservation (contract 2.1).</summary>
public sealed class ChestObservationTests
{
    // GW-U-757
    [Fact]
    public void TheConstructorNeverThrowsForEveryDefault()
    {
        var none = new ChestObservation(false, false, false, false, 0L, 0L, false, false, null, false);
        var max = new ChestObservation(true, true, true, true, long.MaxValue, long.MinValue, true, true, string.Empty, true);

        Assert.NotNull(none);
        Assert.NotNull(max);
    }

    // GW-U-757
    [Fact]
    public void AllTenMembersRoundTrip()
    {
        ChestObservation o = new ChestObservation(false, true, true, false, Me, Erik, false, true, "Ragnar", true);

        Assert.False(o.ViewValid);
        Assert.True(o.AccessAllowed);
        Assert.True(o.OpenLocally);
        Assert.False(o.InUseFlag);
        Assert.Equal(Me, o.LocalSessionId);
        Assert.Equal(Erik, o.OwnerSessionId);
        Assert.False(o.OwnerListed);
        Assert.True(o.OwnerConnected);
        Assert.Equal("Ragnar", o.OwnerName);
        Assert.True(o.IsServer);
    }

    // GW-U-757
    [Fact]
    public void EqualObservationsAreEqualWithEqualHashCodes()
    {
        ChestObservation a = Obs(inUseFlag: true);
        ChestObservation b = Obs(inUseFlag: true);

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.True(a == b);
        Assert.False(a != b);
    }

    // GW-U-757: every one of the ten members takes part in equality.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public void EachMemberTakesPartInEquality(int member)
    {
        ChestObservation baseline = Obs();
        ChestObservation changed = ChangeMember(member);

        Assert.False(baseline.Equals(changed));
        Assert.False(baseline == changed);
        Assert.True(baseline != changed);
    }

    // GW-U-757
    [Fact]
    public void OwnerNameComparesOrdinallyAndNullEqualsNull()
    {
        Assert.False(Obs(ownerName: "Erik") == Obs(ownerName: "erik"));
        Assert.True(Obs(ownerName: null) == Obs(ownerName: null));
        Assert.True(Obs(ownerName: null) != Obs(ownerName: "Erik"));
    }

    // GW-U-757
    [Fact]
    public void NullOperandsCompareWithTheNullRules()
    {
        ChestObservation a = Obs();
        ChestObservation? none = null;

        Assert.False(a == none);
        Assert.True(a != none);
        Assert.True(none == null);
        Assert.False(a.Equals(null));
    }

    private static ChestObservation ChangeMember(int member) => member switch
    {
        0 => Obs(viewValid: false),
        1 => Obs(accessAllowed: false),
        2 => Obs(openLocally: true),
        3 => Obs(inUseFlag: true),
        4 => Obs(localSessionId: Erik),
        5 => Obs(ownerSessionId: 0),
        6 => Obs(ownerListed: false),
        7 => Obs(ownerConnected: false),
        8 => Obs(ownerName: "Ragnar"),
        9 => Obs(isServer: true),
        _ => throw new ArgumentOutOfRangeException(nameof(member)),
    };
}
