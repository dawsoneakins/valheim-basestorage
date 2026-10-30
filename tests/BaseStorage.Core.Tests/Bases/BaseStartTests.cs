namespace BaseStorage.Core.Tests.Bases;

using System;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Bases.ResolutionFixtures;

/// <summary>AC-1 (ADR-GWIN-024, ADR-GWIN-025): the two start kinds, their factories, value equality, and the fixed enum values.</summary>
public sealed class BaseStartTests
{
    // GW-U-239 (AC-1): a station start carries the station id and the default position.
    [Fact]
    public void FromStationCarriesTheStationAndNoPosition()
    {
        BaseStart start = BaseStart.FromStation(StationKey(1));

        Assert.Equal(BaseStartKind.Station, start.Kind);
        Assert.Equal(StationKey(1), start.Station);
        Assert.Equal(default(WorldPosition), start.Position);
    }

    // GW-U-239 (AC-1): a position start carries the position and no station.
    [Fact]
    public void FromPositionCarriesThePositionAndNoStation()
    {
        var point = new WorldPosition(4f, 2f, -3f);

        BaseStart start = BaseStart.FromPosition(point);

        Assert.Equal(BaseStartKind.Position, start.Kind);
        Assert.True(start.Station.IsNone);
        Assert.Equal(point, start.Position);
    }

    // GW-U-239 (AC-1): a none station id is rejected for "station".
    [Fact]
    public void FromStationRejectsNoneForStation()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
        {
            BaseStart.FromStation(default(StationId));
        });

        Assert.Equal("station", ex.ParamName);
    }

    // GW-U-239 (AC-1): equality is by value, the enum values are fixed, and default(BaseStart) has no defined kind.
    [Fact]
    public void StartsCompareByValueAndTheKindValuesAreFixed()
    {
        Assert.True(BaseStart.FromStation(StationKey(1)) == BaseStart.FromStation(StationKey(1)));
        Assert.True(BaseStart.FromStation(StationKey(1)) != BaseStart.FromStation(StationKey(2)));
        Assert.True(BaseStart.FromPosition(new WorldPosition(1f, 0f, 0f)).Equals(BaseStart.FromPosition(new WorldPosition(1f, 0f, 0f))));
        Assert.Equal(BaseStart.FromStation(StationKey(1)).GetHashCode(), BaseStart.FromStation(StationKey(1)).GetHashCode());

        Assert.Equal(1, (int)BaseStartKind.Station);
        Assert.Equal(2, (int)BaseStartKind.Position);
        Assert.Equal(2, Enum.GetValues<BaseStartKind>().Length);
        Assert.False(Enum.IsDefined(typeof(BaseStartKind), default(BaseStart).Kind));
    }
}
