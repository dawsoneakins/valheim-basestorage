namespace BaseStorage.Core.Tests.Snapshots;

using System;

/// <summary>AC-1 and AC-5: StationSnapshot construction, validation and value equality.</summary>
public sealed class StationSnapshotTests
{
    private const string TypeName = "StationSnapshot";

    // GW-U-025
    [Fact]
    public void ConstructorExposesIdPositionAndBuildRange()
    {
        dynamic station = Fixtures.Workbench();
        object id = station.Id;
        object position = station.Position;

        ValueSemantics.AssertEqual(id, Fixtures.StationIdOf(3141592653L, 17u));
        ValueSemantics.AssertEqual(position, Fixtures.PositionOf(12.5f, 31.25f, -840.75f));
        Assert.Equal(20f, (float)station.BuildRange);
    }

    // GW-U-025
    [Fact]
    public void ZeroBuildRangeIsAllowed()
    {
        dynamic station = Fixtures.StationOf(Fixtures.StationIdOf(1L, 1u), Fixtures.PositionOf(0f, 0f, 0f), 0f);

        Assert.Equal(0f, (float)station.BuildRange);
    }

    // GW-U-025
    [Fact]
    public void NegativeZeroBuildRangeIsStoredAsPositiveZero()
    {
        dynamic station = Fixtures.StationOf(Fixtures.StationIdOf(1L, 1u), Fixtures.PositionOf(0f, 0f, 0f), -0f);

        float range = station.BuildRange;
        Assert.Equal(0, BitConverter.SingleToInt32Bits(range));
    }

    // GW-U-025
    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(-1f)]
    public void InvalidBuildRangeIsOutOfRange(float buildRange)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Fixtures.StationOf(Fixtures.StationIdOf(1L, 1u), Fixtures.PositionOf(0f, 0f, 0f), buildRange);
        });

        Assert.Equal("buildRange", ex.ParamName);
    }

    // GW-U-025
    [Fact]
    public void DefaultStationIdIsRejectedAsArgumentException()
    {
        object none = ContractTypes.Default(ContractTypes.Snapshot("StationId"));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
        {
            Fixtures.StationOf(none, Fixtures.PositionOf(0f, 0f, 0f), 20f);
        });

        Assert.Equal("id", ex.ParamName);
    }

    // GW-U-035
    [Fact]
    public void EqualStationsAreEqualWithEqualHashCodes()
    {
        ValueSemantics.AssertEqual(Fixtures.Workbench(), Fixtures.Workbench());
    }

    // GW-U-035
    [Theory]
    [InlineData("id")]
    [InlineData("position")]
    [InlineData("buildRange")]
    public void ChangingOneFieldMakesTheStationUnequal(string field)
    {
        object other = field switch
        {
            "id" => Fixtures.StationOf(Fixtures.StationIdOf(3141592653L, 18u), Fixtures.PositionOf(12.5f, 31.25f, -840.75f), 20f),
            "position" => Fixtures.StationOf(Fixtures.StationIdOf(3141592653L, 17u), Fixtures.PositionOf(12.5f, 31.25f, -841f), 20f),
            "buildRange" => Fixtures.StationOf(Fixtures.StationIdOf(3141592653L, 17u), Fixtures.PositionOf(12.5f, 31.25f, -840.75f), 22.5f),
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };

        ValueSemantics.AssertNotEqual(Fixtures.Workbench(), other);
    }

    // GW-U-035
    [Fact]
    public void NullHandlingFollowsClassEquality()
    {
        ValueSemantics.AssertNullHandling(ContractTypes.Snapshot(TypeName), Fixtures.Workbench());
    }
}
