namespace BaseStorage.Core.Tests.Feeding;

using System;
using BaseStorage.Core.Feeding;
using static BaseStorage.Core.Tests.Feeding.FeedFixtures;

/// <summary>The four factories of <c>FeedStationState</c> (contract 2.2) and their validation (3.4, 7.6).</summary>
public sealed class FeedStationShapeTests
{
    // GW-U-2640
    [Theory]
    [InlineData("SmelterClass")]
    [InlineData("FuelOnly")]
    [InlineData("SingleInput")]
    [InlineData("SeveralInput")]
    public void FM1_EachFactoryFixesTheShapeAndStoresValuesAsGiven(string factory)
    {
        FeedStationState state = factory switch
        {
            "SmelterClass" => FeedStationState.SmelterClass(3, 10, 1.5f, 20f, "Coal", Ores),
            "FuelOnly" => FeedStationState.FuelOnly(2.5f, 10.5f, "Wood"),
            "SingleInput" => FeedStationState.SingleInput(true, Mead),
            "SeveralInput" => FeedStationState.SeveralInput(4, 1, Food),
            _ => throw new ArgumentException("Unknown factory: " + factory, nameof(factory)),
        };

        switch (factory)
        {
            case "SmelterClass":
                Assert.Equal(FeedStationShape.SmelterClass, state.Shape);
                Assert.Equal(3, state.Queued);
                Assert.Equal(10, state.MaxQueued);
                Assert.Equal(1.5f, state.Fuel);
                Assert.Equal(20f, state.MaxFuel);
                Assert.Equal("Coal", state.FuelPrefab);
                Assert.Same(Ores, state.Inputs);
                break;
            case "FuelOnly":
                Assert.Equal(FeedStationShape.FuelOnly, state.Shape);
                Assert.Equal(0, state.Queued);
                Assert.Equal(0, state.MaxQueued);
                Assert.Equal(2.5f, state.Fuel);
                Assert.Equal(10.5f, state.MaxFuel);
                Assert.Equal("Wood", state.FuelPrefab);
                Assert.Empty(state.Inputs);
                break;
            case "SingleInput":
                Assert.Equal(FeedStationShape.SingleInput, state.Shape);
                Assert.Equal(1, state.Queued);
                Assert.Equal(1, state.MaxQueued);
                Assert.Equal(0f, state.Fuel);
                Assert.Equal(0f, state.MaxFuel);
                Assert.Null(state.FuelPrefab);
                Assert.Same(Mead, state.Inputs);
                break;
            default:
                Assert.Equal(FeedStationShape.SeveralInput, state.Shape);
                Assert.Equal(1, state.Queued);
                Assert.Equal(4, state.MaxQueued);
                Assert.Equal(0f, state.Fuel);
                Assert.Equal(0f, state.MaxFuel);
                Assert.Null(state.FuelPrefab);
                Assert.Same(Food, state.Inputs);
                break;
        }

        // Out-of-range values are stored as given, never clamped (2.2, R7 exception).
        FeedStationState several = FeedStationState.SeveralInput(-1, 7, Food);
        Assert.Equal(-1, several.MaxQueued);
        Assert.Equal(7, several.Queued);
        FeedStationState burnt = FeedStationState.FuelOnly(float.NaN, 10f, "Wood");
        Assert.True(float.IsNaN(burnt.Fuel));
    }

    // GW-U-2645
    [Theory]
    [InlineData("SmelterClass")]
    [InlineData("SingleInput")]
    [InlineData("SeveralInput")]
    [InlineData("FuelOnly")]
    public void FactoriesRejectOnlyNullInputs(string factory)
    {
        if (factory == "FuelOnly")
        {
            // FuelOnly has no inputs parameter, and a null fuel prefab is allowed (2.2).
            Assert.Equal(FeedStationShape.FuelOnly, FeedStationState.FuelOnly(0f, 10f, null).Shape);
            return;
        }

        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => Build(factory));
        Assert.Equal("inputs", error.ParamName);
    }

    private static FeedStationState Build(string factory) => factory switch
    {
        "SmelterClass" => FeedStationState.SmelterClass(0, 1, 0f, 1f, null, null!),
        "SingleInput" => FeedStationState.SingleInput(false, null!),
        "SeveralInput" => FeedStationState.SeveralInput(1, 0, null!),
        _ => throw new ArgumentException("Unknown factory: " + factory, nameof(factory)),
    };
}
