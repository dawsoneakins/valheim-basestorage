namespace BaseStorage.Core.Tests.Bases;

using System;
using BaseStorage.Core.Bases.Inspection;

/// <summary>AC-4 (ADR-GWIN-032): null arguments are rejected with an ArgumentNullException that names the parameter.</summary>
public sealed class HoverArgumentTests
{
    // GW-U-548 (AC-4): the constructor rejects a null cache.
    [Fact]
    public void TheConstructorRejectsANullCacheByName()
    {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => new HoverInspector(null!));

        Assert.Equal("cache", error.ParamName);
    }

    // GW-U-548 (AC-4): FormatChest rejects a null resolution.
    [Fact]
    public void FormatChestRejectsANullResolutionByName()
    {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => HoverInspector.FormatChest(null!, null));

        Assert.Equal("resolution", error.ParamName);
    }

    // GW-U-548 (AC-4): FormatStation rejects a null resolution.
    [Fact]
    public void FormatStationRejectsANullResolutionByName()
    {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => HoverInspector.FormatStation(null!));

        Assert.Equal("resolution", error.ParamName);
    }

    // GW-U-548 (AC-4): CountChests rejects a null resolution.
    [Fact]
    public void CountChestsRejectsANullResolutionByName()
    {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => HoverInspector.CountChests(null!));

        Assert.Equal("resolution", error.ParamName);
    }

    // GW-U-548 (AC-4): Append rejects a null line.
    [Fact]
    public void AppendRejectsANullLineByName()
    {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => HoverInspector.Append("x", null!));

        Assert.Equal("line", error.ParamName);
    }
}
