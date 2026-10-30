namespace BaseStorage.Core.Tests.Snapshots;

using System;

/// <summary>AC-4 and AC-5: WorldPosition construction, float normalisation and value equality.</summary>
public sealed class WorldPositionTests
{
    private const string TypeName = "WorldPosition";

    // GW-U-029
    [Fact]
    public void AxesRoundTripThroughTheConstructor()
    {
        dynamic position = Fixtures.PositionOf(12.5f, 31.25f, -840.75f);

        Assert.Equal(12.5f, (float)position.X);
        Assert.Equal(31.25f, (float)position.Y);
        Assert.Equal(-840.75f, (float)position.Z);
    }

    // GW-U-029
    [Fact]
    public void DefaultValueIsTheOrigin()
    {
        dynamic origin = ContractTypes.Default(ContractTypes.Snapshot(TypeName));

        Assert.Equal(0f, (float)origin.X);
        Assert.Equal(0f, (float)origin.Y);
        Assert.Equal(0f, (float)origin.Z);
    }

    // GW-U-029
    [Theory]
    [InlineData(float.NaN, 0f, 0f, "x")]
    [InlineData(float.PositiveInfinity, 0f, 0f, "x")]
    [InlineData(float.NegativeInfinity, 0f, 0f, "x")]
    [InlineData(0f, float.NaN, 0f, "y")]
    [InlineData(0f, float.PositiveInfinity, 0f, "y")]
    [InlineData(0f, float.NegativeInfinity, 0f, "y")]
    [InlineData(0f, 0f, float.NaN, "z")]
    [InlineData(0f, 0f, float.PositiveInfinity, "z")]
    [InlineData(0f, 0f, float.NegativeInfinity, "z")]
    [InlineData(float.NaN, float.PositiveInfinity, 0f, "x")]
    [InlineData(0f, float.NaN, float.PositiveInfinity, "y")]
    public void NonFiniteAxisIsRejectedInXYZOrder(float x, float y, float z, string paramName)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            ContractTypes.Make(ContractTypes.Snapshot(TypeName), x, y, z);
        });

        Assert.Equal(paramName, ex.ParamName);
    }

    // GW-U-029
    [Fact]
    public void NegativeZeroIsStoredAsPositiveZeroOnEveryAxis()
    {
        dynamic position = Fixtures.PositionOf(-0f, -0f, -0f);

        float x = position.X;
        float y = position.Y;
        float z = position.Z;
        Assert.Equal(0, BitConverter.SingleToInt32Bits(x));
        Assert.Equal(0, BitConverter.SingleToInt32Bits(y));
        Assert.Equal(0, BitConverter.SingleToInt32Bits(z));
    }

    // GW-U-032
    [Fact]
    public void EqualValuesAreEqualWithEqualHashCodes()
    {
        ValueSemantics.AssertEqual(
            Fixtures.PositionOf(12.5f, 31.25f, -840.75f),
            Fixtures.PositionOf(12.5f, 31.25f, -840.75f));
    }

    // GW-U-032
    [Theory]
    [InlineData(12.5f, 31.5f, -840.75f)]
    [InlineData(12.75f, 31.25f, -840.75f)]
    [InlineData(12.5f, 31.25f, -840.5f)]
    public void AnyDifferingAxisIsUnequal(float x, float y, float z)
    {
        ValueSemantics.AssertNotEqual(Fixtures.PositionOf(12.5f, 31.25f, -840.75f), Fixtures.PositionOf(x, y, z));
    }

    // GW-U-032
    [Fact]
    public void NegativeZeroEqualsTheDefaultOrigin()
    {
        ValueSemantics.AssertEqual(
            Fixtures.PositionOf(-0f, 0f, 0f),
            ContractTypes.Default(ContractTypes.Snapshot(TypeName)));
    }

    // GW-U-032
    [Fact]
    public void ComparingWithAStringIsFalse()
    {
        object position = Fixtures.PositionOf(12.5f, 31.25f, -840.75f);

        Assert.False(position.Equals((object)"x"));
    }
}
