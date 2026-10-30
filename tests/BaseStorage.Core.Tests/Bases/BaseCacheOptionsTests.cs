namespace BaseStorage.Core.Tests.Bases;

using BaseStorage.Core.Bases.Resolution;

/// <summary>AC-3 (ADR-GWIN-024, ADR-GWIN-025): the cache options normalise their values and never throw.</summary>
public sealed class BaseCacheOptionsTests
{
    // GW-U-240 (AC-3): the constructor never throws; a negative window becomes 0, and a station cap below 1 becomes 1.
    [Theory]
    [InlineData(2, 64, 2, 64)]
    [InlineData(-1, 64, 0, 64)]
    [InlineData(0, 0, 0, 1)]
    [InlineData(5, -3, 5, 1)]
    [InlineData(-1, -1, 0, 1)]
    public void ConstructorNormalisesTheSettings(int seconds, int maxStations, int expectedSeconds, int expectedMaxStations)
    {
        var options = new BaseCacheOptions(seconds, maxStations);

        Assert.Equal(expectedSeconds, options.BaseCacheSeconds);
        Assert.Equal(expectedMaxStations, options.MaxBaseStations);
    }

    // GW-U-240 (AC-3): equality compares the normalised values, the hashes match, and different values are not equal.
    [Fact]
    public void EqualityComparesTheNormalisedValues()
    {
        var clamped = new BaseCacheOptions(-1, 0);
        var normal = new BaseCacheOptions(0, 1);

        Assert.True(clamped == normal);
        Assert.Equal(normal.GetHashCode(), clamped.GetHashCode());
        Assert.True(new BaseCacheOptions(2, 64) != new BaseCacheOptions(3, 64));
    }
}
