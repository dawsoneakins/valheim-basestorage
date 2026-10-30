namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using static BaseStorage.Core.Tests.Bases.ResolutionFixtures;

/// <summary>AC-2 (ADR-GWIN-024, ADR-GWIN-025): the no-base result is a shared singleton that maps to "Not in a base".</summary>
public sealed class BaseResolutionTests
{
    // GW-U-237 (AC-2): the no-base result is one shared instance, with no stations, no containers and the "Not in a base" key.
    [Fact]
    public void NotInBaseIsASharedResultWithTheNotInABaseKey()
    {
        BaseResolution first = BaseResolution.NotInBase;
        BaseResolution second = BaseResolution.NotInBase;

        Assert.Same(first, second);
        Assert.False(first.IsInBase);
        Assert.Empty(first.Stations);
        Assert.Empty(first.Containers);
        Assert.False(first.IsCapped);
        Assert.Equal("message.base.not-in-base", first.MessageKey);
        Assert.Equal("Not in a base", StringTable.English.Get(first.MessageKey!));
    }

    // GW-U-237 (AC-2): an in-base result has no message key, and its station and container lists are read-only.
    [Fact]
    public void InBaseResultHasNoMessageKeyAndReadOnlyLists()
    {
        BaseResolution result = BaseResolver.Resolve(WorldStations(), WorldContainers(), BaseStart.FromStation(StationKey(1)), 64);

        Assert.Null(result.MessageKey);
        Assert.Throws<NotSupportedException>(() =>
        {
            ((IList<StationSnapshot>)result.Stations).Add(Station5());
        });
        Assert.Throws<NotSupportedException>(() =>
        {
            ((IList<ContainerSnapshot>)result.Containers).Add(Container14());
        });
    }
}
